using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Desktopirates
{
    public static class CompactSaveCodec
    {
        private const byte Version = 7;
        private const int Version4RoleCount = 4;
        private const int Version4PerkCount = 14;
        private const int Version5RoleCount = 5;
        private const int Version5PerkCount = 20;
        private const float PositionScale = 16f;

        public static byte[] Serialize(GameState state)
        {
            using var raw = new MemoryStream();
            using (var writer = new BinaryWriter(raw, Encoding.UTF8, true))
            {
                writer.Write(new byte[] { (byte)'D', (byte)'P', (byte)'R', (byte)'S' });
                writer.Write(Version);
                writer.Write(state.WorldSeed);
                WriteSigned(writer, Mathf.RoundToInt(state.PlayerPosition.x * PositionScale));
                WriteSigned(writer, Mathf.RoundToInt(state.PlayerPosition.y * PositionScale));
                WriteUnsigned(writer, (ulong)Mathf.RoundToInt(Mathf.Repeat(state.HeadingDegrees, 360f) / 360f * 65535f));
                WriteUnsigned(writer, (ulong)Mathf.Max(0, state.Gold));
                WriteUnsigned(writer, (ulong)Mathf.Max(0, state.Hull));
                WriteUnsigned(writer, (ulong)Mathf.Max(1, state.MaxHull));
                WriteUnsigned(writer, (ulong)Mathf.Max(0, state.Supplies));
                writer.Write((byte)Mathf.Clamp(state.EngineLevel, 0, 255));
                writer.Write((byte)Mathf.Clamp(state.CannonLevel, 0, 255));
                for (int i = 0; i < SalvageInventory.PartKindCount; i++)
                    WriteUnsigned(writer, (ulong)state.GetPartCount((SalvagePartKind)i));
                WriteUnsigned(writer, (ulong)Mathf.Max(0, state.Crew));
                writer.Write((byte)Mathf.Clamp(state.CapacityLevel, 0, 255));
                writer.Write((byte)Mathf.Clamp(state.ArmorLevel, 0, 255));
                writer.Write((byte)Mathf.Clamp(state.TurningLevel, 0, 255));
                WriteUnsigned(writer, (ulong)(state.CannonMountMask & ((1 << ShipCustomizationModel.CannonSlotCount) - 1)));
                WriteUnsigned(writer, (ulong)Mathf.Max(0, state.SpareCannons));
                writer.Write((byte)Mathf.Clamp(state.ShipLevel, 0, ShipProgressionModel.TierCount - 1));
                WriteUnsigned(writer, (ulong)Mathf.Max(0, state.Food));
                WriteUnsigned(writer, (ulong)Mathf.Max(0, state.Water));
                WriteUnsigned(writer, (ulong)Mathf.Max(0, Mathf.RoundToInt(state.ProvisionClock * 10f)));
                for (int i = 0; i < CrewManagementModel.RoleCount; i++)
                    WriteUnsigned(writer, (ulong)Mathf.Max(0, state.GetRoleCrew((CrewRole)i)));
                for (int i = 1; i < CrewManagementModel.PerkCount; i++)
                for (int rank = 0; rank < PerkRankModel.RankCount; rank++)
                    WriteUnsigned(writer, (ulong)Mathf.Max(0, state.GetPerkCount((CrewPerk)i, (PerkRank)rank)));
                for (int role = 0; role < CrewManagementModel.RoleCount; role++)
                for (int perk = 1; perk < CrewManagementModel.PerkCount; perk++)
                for (int rank = 0; rank < PerkRankModel.RankCount; rank++)
                    WriteUnsigned(writer, (ulong)Mathf.Max(0, state.GetEquippedPerkCount((CrewRole)role, (CrewPerk)perk, (PerkRank)rank)));
                WriteExploration(writer, state.ExploredChunks);
                WriteResolved(writer, state.ResolvedEvents);
                WriteCaptainRecord(writer, state.Captain);
            }

            raw.Position = 0;
            using var compressed = new MemoryStream();
            using (var deflate = new DeflateStream(compressed, System.IO.Compression.CompressionLevel.Optimal, true)) raw.CopyTo(deflate);
            return compressed.ToArray();
        }

        public static GameState Deserialize(byte[] bytes)
        {
            using var compressed = new MemoryStream(bytes);
            using var deflate = new DeflateStream(compressed, CompressionMode.Decompress);
            using var reader = new BinaryReader(deflate, Encoding.UTF8);
            if (reader.ReadByte() != 'D' || reader.ReadByte() != 'P' || reader.ReadByte() != 'R' || reader.ReadByte() != 'S')
                throw new InvalidDataException("Not a desktopirates save.");
            byte version = reader.ReadByte();
            if (version < 1 || version > Version) throw new InvalidDataException("Unsupported desktopirates save version.");

            var state = new GameState
            {
                WorldSeed = reader.ReadInt32(),
                PlayerPosition = new Vector2(ReadSigned(reader) / PositionScale, ReadSigned(reader) / PositionScale),
                HeadingDegrees = (float)(ReadUnsigned(reader) * 360.0 / 65535.0),
                Gold = (int)ReadUnsigned(reader),
                Hull = (int)ReadUnsigned(reader),
                MaxHull = (int)ReadUnsigned(reader),
                Supplies = (int)ReadUnsigned(reader),
                EngineLevel = reader.ReadByte(),
                CannonLevel = reader.ReadByte()
            };
            int serializedMaxHull = state.MaxHull;
            if (version >= 2)
            {
                for (int i = 0; i < SalvageInventory.PartKindCount; i++)
                    state.SetPartCount((SalvagePartKind)i, checked((int)ReadUnsigned(reader)));
            }
            if (version >= 3)
            {
                state.Crew = checked((int)ReadUnsigned(reader));
                state.CapacityLevel = reader.ReadByte();
                state.ArmorLevel = reader.ReadByte();
                state.TurningLevel = reader.ReadByte();
                state.CannonMountMask = checked((int)ReadUnsigned(reader));
                state.SpareCannons = checked((int)ReadUnsigned(reader));
            }
            if (version >= 4)
            {
                state.ShipLevel = Mathf.Clamp(reader.ReadByte(), 0, ShipProgressionModel.TierCount - 1);
                state.Food = checked((int)ReadUnsigned(reader));
                state.Water = checked((int)ReadUnsigned(reader));
                state.ProvisionClock = ReadUnsigned(reader) / 10f;
                if (version >= 6)
                {
                    for (int i = 0; i < CrewManagementModel.RoleCount; i++)
                        state.SetRoleCrew((CrewRole)i, checked((int)ReadUnsigned(reader)));
                    for (int i = 1; i < CrewManagementModel.PerkCount; i++)
                    for (int rank = 0; rank < PerkRankModel.RankCount; rank++)
                        state.SetPerkCount((CrewPerk)i, (PerkRank)rank, checked((int)ReadUnsigned(reader)));
                    for (int role = 0; role < CrewManagementModel.RoleCount; role++)
                    for (int perk = 1; perk < CrewManagementModel.PerkCount; perk++)
                    for (int rank = 0; rank < PerkRankModel.RankCount; rank++)
                        state.SetEquippedPerkCount((CrewRole)role, (CrewPerk)perk, (PerkRank)rank, checked((int)ReadUnsigned(reader)));
                }
                else if (version >= 5)
                {
                    for (int i = 0; i < Version5RoleCount; i++)
                        state.SetRoleCrew((CrewRole)i, checked((int)ReadUnsigned(reader)));
                    for (int i = 1; i < Version5PerkCount; i++)
                        state.SetPerkCount((CrewPerk)i, checked((int)ReadUnsigned(reader)));
                    for (int role = 0; role < Version5RoleCount; role++)
                    for (int perk = 1; perk < Version5PerkCount; perk++)
                        state.SetEquippedPerkCount((CrewRole)role, (CrewPerk)perk, checked((int)ReadUnsigned(reader)));
                }
                else
                {
                    for (int i = 0; i < Version4RoleCount; i++)
                        state.SetRoleCrew((CrewRole)i, checked((int)ReadUnsigned(reader)));
                    for (int i = 1; i < Version4PerkCount; i++)
                        state.SetPerkCount((CrewPerk)i, checked((int)ReadUnsigned(reader)));
                    for (int i = 0; i < Version4RoleCount; i++)
                        state.EquipPerk((CrewRole)i, (CrewPerk)reader.ReadByte());
                }
            }
            else
            {
                // Existing captains keep the former starter ship and an immediately usable crew layout.
                state.ShipLevel = 1;
                for (int i = 0; i < CrewManagementModel.RoleCount; i++) state.SetRoleCrew((CrewRole)i, 0);
                int gunCrew = Mathf.Min(state.Crew, Mathf.Max(1, ShipCustomizationModel.GetInstalledCannonCount(state)));
                state.SetRoleCrew(CrewRole.Cannons, gunCrew);
                if (state.Crew > gunCrew) state.SetRoleCrew(CrewRole.Helm, 1);
            }
            state.MaxHull = ShipProgressionModel.GetMaxHull(state);
            if (version < 5)
            {
                float oldRatio = state.Hull / (float)Mathf.Max(1, serializedMaxHull);
                state.Hull = Mathf.Clamp(Mathf.CeilToInt(state.MaxHull * oldRatio), 0, state.MaxHull);
            }
            else state.Hull = Mathf.Min(state.Hull, state.MaxHull);
            ReadExploration(reader, state.ExploredChunks);
            ReadResolved(reader, state.ResolvedEvents);
            if (version >= 7) ReadCaptainRecord(reader, state.Captain);
            return state;
        }

        private static void WriteCaptainRecord(BinaryWriter writer, CaptainRecord record)
        {
            WriteUnsigned(writer, (ulong)record.DistanceHundredths);
            WriteUnsigned(writer, (ulong)record.DamageDealt);
            WriteUnsigned(writer, (ulong)record.GoldEarned);
            WriteUnsigned(writer, (ulong)record.WrecksSalvaged);
            WriteUnsigned(writer, (ulong)record.TreasuresFound);
            WriteUnsigned(writer, (ulong)record.PortCalls);
            WriteUnsigned(writer, record.DiscoveredRegionMask);
            for (int i = 0; i < EnemyArchetypeModel.Count; i++)
                WriteUnsigned(writer, (ulong)record.GetEnemyCount((EnemyArchetype)i));
            for (int i = 1; i < BossMutationModel.BossCount; i++)
                WriteUnsigned(writer, (ulong)record.GetBossCount((BossKind)i));
            for (int i = 1; i < BossMutationModel.MutationCount; i++)
                WriteUnsigned(writer, (ulong)record.GetMutationCount((BossMutation)i));
        }

        private static void ReadCaptainRecord(BinaryReader reader, CaptainRecord record)
        {
            record.SetDistanceHundredths(checked((long)ReadUnsigned(reader)));
            record.DamageDealt = checked((long)ReadUnsigned(reader));
            record.GoldEarned = checked((long)ReadUnsigned(reader));
            record.WrecksSalvaged = checked((int)ReadUnsigned(reader));
            record.TreasuresFound = checked((int)ReadUnsigned(reader));
            record.PortCalls = checked((int)ReadUnsigned(reader));
            record.SetDiscoveredRegionMask(checked((uint)ReadUnsigned(reader)));
            for (int i = 0; i < EnemyArchetypeModel.Count; i++)
                record.SetEnemyCount((EnemyArchetype)i, checked((int)ReadUnsigned(reader)));
            for (int i = 1; i < BossMutationModel.BossCount; i++)
                record.SetBossCount((BossKind)i, checked((int)ReadUnsigned(reader)));
            for (int i = 1; i < BossMutationModel.MutationCount; i++)
                record.SetMutationCount((BossMutation)i, checked((int)ReadUnsigned(reader)));
        }

        private static void WriteExploration(BinaryWriter writer, IEnumerable<long> chunks)
        {
            var rows = new SortedDictionary<int, List<int>>();
            foreach (long packed in chunks)
            {
                GameState.UnpackChunk(packed, out int x, out int y);
                if (!rows.TryGetValue(y, out List<int> xs)) rows[y] = xs = new List<int>();
                xs.Add(x);
            }
            WriteUnsigned(writer, (ulong)rows.Count);
            int previousY = 0;
            foreach (var row in rows)
            {
                WriteSigned(writer, row.Key - previousY);
                previousY = row.Key;
                List<int> xs = row.Value.Distinct().OrderBy(x => x).ToList();
                var spans = new List<(int start, int length)>();
                for (int i = 0; i < xs.Count;)
                {
                    int start = xs[i];
                    int end = start;
                    while (++i < xs.Count && xs[i] == end + 1) end = xs[i];
                    spans.Add((start, end - start + 1));
                }
                WriteUnsigned(writer, (ulong)spans.Count);
                int previousEnd = 0;
                foreach (var span in spans)
                {
                    WriteSigned(writer, span.start - previousEnd);
                    WriteUnsigned(writer, (ulong)span.length);
                    previousEnd = span.start + span.length;
                }
            }
        }

        private static void ReadExploration(BinaryReader reader, ISet<long> output)
        {
            int rowCount = checked((int)ReadUnsigned(reader));
            int y = 0;
            for (int row = 0; row < rowCount; row++)
            {
                y += ReadSigned(reader);
                int spanCount = checked((int)ReadUnsigned(reader));
                int previousEnd = 0;
                for (int span = 0; span < spanCount; span++)
                {
                    int start = previousEnd + ReadSigned(reader);
                    int length = checked((int)ReadUnsigned(reader));
                    for (int x = start; x < start + length; x++) output.Add(GameState.PackChunk(x, y));
                    previousEnd = start + length;
                }
            }
        }

        private static void WriteResolved(BinaryWriter writer, IEnumerable<ulong> values)
        {
            ulong[] sorted = values.OrderBy(value => value).ToArray();
            WriteUnsigned(writer, (ulong)sorted.Length);
            ulong previous = 0;
            foreach (ulong value in sorted) { WriteUnsigned(writer, value - previous); previous = value; }
        }

        private static void ReadResolved(BinaryReader reader, ISet<ulong> output)
        {
            int count = checked((int)ReadUnsigned(reader));
            ulong previous = 0;
            for (int i = 0; i < count; i++) { previous += ReadUnsigned(reader); output.Add(previous); }
        }

        public static void WriteUnsigned(BinaryWriter writer, ulong value)
        {
            while (value >= 0x80) { writer.Write((byte)(value | 0x80)); value >>= 7; }
            writer.Write((byte)value);
        }

        public static ulong ReadUnsigned(BinaryReader reader)
        {
            ulong value = 0;
            for (int shift = 0; shift < 64; shift += 7)
            {
                byte next = reader.ReadByte();
                value |= (ulong)(next & 0x7F) << shift;
                if ((next & 0x80) == 0) return value;
            }
            throw new InvalidDataException("Invalid varint.");
        }

        private static void WriteSigned(BinaryWriter writer, int value) => WriteUnsigned(writer, (uint)((value << 1) ^ (value >> 31)));
        private static int ReadSigned(BinaryReader reader) { uint value = (uint)ReadUnsigned(reader); return (int)((value >> 1) ^ (uint)-(int)(value & 1)); }
    }
}
