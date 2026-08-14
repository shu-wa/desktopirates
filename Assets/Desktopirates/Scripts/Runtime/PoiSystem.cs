using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Desktopirates
{
    public sealed class PoiRecord
    {
        public ulong Id;
        public PoiKind Kind;
        public Vector2 LogicalPosition;
        public Vector2 SpawnPosition;
        public int Reward;
        public int Health;
        public int MaxHealth;
        public BossKind Boss;
        public bool Resolved;
        public bool IsUnderFire;
        public bool IsSinking;
        public bool IsPreview;
        public Transform Visual;
        public readonly ShipStatusRuntime Conditions = new ShipStatusRuntime();
        public ShipStatusVisualController StatusVisual;
    }

    public sealed class PoiSystem : MonoBehaviour
    {
        public const float VisibleRadius = 6.8f;
        public IReadOnlyList<PoiRecord> Items => items;
        public string InteractionPrompt { get; private set; }
        public event Action<string> Message;
        public event Action PortRequested;
        public event Action StateChanged;

        private readonly List<PoiRecord> items = new List<PoiRecord>();
        private readonly List<GeneratedEventData> generated = new List<GeneratedEventData>();
        private BoatController player;
        private GameState state;
        private CombatVfxController combatVfx;
        private PlayerShipConditionController playerConditions;
        private readonly List<CannonSlot> firingSlots = new List<CannonSlot>();
        private int centerChunkX = int.MinValue;
        private int centerChunkY = int.MinValue;
        private float enemyFireCooldown;
        private float cannonCooldown;
        private ulong cannonSalvoSerial;

        public void Initialize(BoatController boat, GameState gameState, CombatVfxController effects = null, PlayerShipConditionController conditions = null)
        {
            player = boat;
            state = gameState;
            combatVfx = effects;
            playerConditions = conditions;
            if (playerConditions != null)
            {
                playerConditions.Message += value => Message?.Invoke(value);
                playerConditions.Changed += () => StateChanged?.Invoke();
                playerConditions.HullDepleted += RescueTow;
            }
            RefreshChunks(true);
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--combat-preview")) StartCoroutine(CombatPreviewRoutine());
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--dock-preview")) StartCoroutine(DockPreviewRoutine());
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--boss-preview")) StartCoroutine(BossPreviewRoutine());
        }

        private void Update()
        {
            if (player == null) return;
            RefreshChunks(false);
            enemyFireCooldown = Mathf.Max(0f, enemyFireCooldown - Time.deltaTime);
            cannonCooldown = Mathf.Max(0f, cannonCooldown - Time.deltaTime);

            PoiRecord closest = null;
            float closestDistance = float.MaxValue;
            foreach (PoiRecord item in items)
            {
                if (item.Resolved) continue;
                if (item.Kind == PoiKind.Enemy && !item.IsSinking)
                {
                    int dotDamage = item.Conditions.Tick(Time.deltaTime, item.MaxHealth);
                    if (dotDamage > 0) item.Health = Mathf.Max(0, item.Health - dotDamage);
                    if (item.Health <= 0)
                    {
                        BeginStatusSinking(item);
                        continue;
                    }
                }
                if (item.Kind == PoiKind.Enemy && !item.IsSinking && !item.IsPreview) UpdateEnemy(item);
                Vector2 relative = item.LogicalPosition - player.LogicalPosition;
                float distance = relative.magnitude;
                if (distance < closestDistance) { closest = item; closestDistance = distance; }
                bool visible = distance <= VisibleRadius;
                if (item.Visual.gameObject.activeSelf != visible) item.Visual.gameObject.SetActive(visible);
                if (!visible) continue;
                if (item.IsSinking)
                {
                    // A sinking ship no longer navigates, but its rendered position must keep
                    // following the camera-relative projection of its frozen world coordinate.
                    Vector3 current = item.Visual.localPosition;
                    item.Visual.localPosition = new Vector3(relative.x, current.y, relative.y);
                }
                else item.Visual.localPosition = new Vector3(relative.x, 0.30f, relative.y);
                if (item.Kind != PoiKind.Enemy && !item.IsSinking) item.Visual.localRotation = Quaternion.identity;
            }

            InteractionPrompt = BuildPrompt(closest, closestDistance);
            if (Input.GetKeyDown(KeyCode.F) && closest != null) Interact(closest, closestDistance);
            if (Input.GetKeyDown(KeyCode.Space)) FireCannon();
        }

        private void RefreshChunks(bool force)
        {
            int chunkX = Mathf.FloorToInt(player.LogicalPosition.x / WorldGenerator.ChunkSize);
            int chunkY = Mathf.FloorToInt(player.LogicalPosition.y / WorldGenerator.ChunkSize);
            state.ExploredChunks.Add(GameState.PackChunk(chunkX, chunkY));
            if (!force && chunkX == centerChunkX && chunkY == centerChunkY) return;
            if (!force && (player.IsAutoNavigating || items.Exists(item => item.IsUnderFire || item.IsSinking))) return;
            centerChunkX = chunkX;
            centerChunkY = chunkY;
            foreach (PoiRecord item in items) if (item.Visual != null) Destroy(item.Visual.gameObject);
            items.Clear();

            for (int y = chunkY - 3; y <= chunkY + 3; y++)
            for (int x = chunkX - 3; x <= chunkX + 3; x++)
            {
                WorldGenerator.GenerateChunk(state.WorldSeed, x, y, generated);
                foreach (GeneratedEventData data in generated)
                {
                    if (state.ResolvedEvents.Contains(data.Id)) continue;
                    Transform visual = ProceduralSceneFactory.CreatePoiVisual(data.Kind, transform, data.Boss);
                    visual.gameObject.SetActive(false);
                    int health = data.Boss != BossKind.None ? BossModel.GetHull(data.Boss) : data.Kind == PoiKind.Enemy ? 140 + data.Reward * 8 : 1;
                    var record = new PoiRecord
                    {
                        Id = data.Id,
                        Kind = data.Kind,
                        LogicalPosition = data.Position,
                        SpawnPosition = data.Position,
                        Reward = data.Reward,
                        Health = health,
                        MaxHealth = health,
                        Boss = data.Boss,
                        Visual = visual
                    };
                    AttachStatusVisual(record);
                    items.Add(record);
                }
            }
        }

        private void UpdateEnemy(PoiRecord enemy)
        {
            Vector2 toPlayer = player.LogicalPosition - enemy.LogicalPosition;
            float distance = toPlayer.magnitude;
            bool harborSafe = PlayerInSafeHarbor();
            float frozenSpeed = enemy.Conditions.IsActive(ShipStatus.Frozen) ? 0.55f : 1f;
            float stickyTurn = enemy.Conditions.IsActive(ShipStatus.Sticky) ? 0.55f : 1f;
            if (!harborSafe && distance < 7.2f && distance > 1.45f)
                enemy.LogicalPosition += toPlayer.normalized * Time.deltaTime * 1.15f * frozenSpeed;
            else if (distance >= 7.2f)
            {
                float phase = (float)(enemy.Id & 1023UL) * 0.015f + Time.time * 0.18f;
                Vector2 patrolTarget = enemy.SpawnPosition + new Vector2(Mathf.Sin(phase), Mathf.Cos(phase)) * 1.2f;
                enemy.LogicalPosition = Vector2.MoveTowards(enemy.LogicalPosition, patrolTarget, Time.deltaTime * 0.55f * frozenSpeed);
            }
            Vector2 heading = player.LogicalPosition - enemy.LogicalPosition;
            if (heading.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.Euler(0f, Mathf.Atan2(heading.x, heading.y) * Mathf.Rad2Deg, 0f);
                enemy.Visual.localRotation = Quaternion.RotateTowards(enemy.Visual.localRotation, targetRotation, Time.deltaTime * 105f * stickyTurn);
            }

            if (!harborSafe && distance < 2.35f && enemyFireCooldown <= 0f)
            {
                enemyFireCooldown = 2.7f;
                if (combatVfx != null) combatVfx.PlayEnemyShot(enemy.Visual, player.Visual, () => ApplyEnemyHit(enemy));
                else ApplyEnemyHit(enemy);
            }
        }

        private void ApplyEnemyHit(PoiRecord attacker)
        {
            int damage = attacker != null ? BossModel.GetContactDamage(attacker.Boss) : 1;
            if (attacker != null && attacker.Conditions.IsActive(ShipStatus.Poisoned)) damage = Mathf.Max(1, Mathf.CeilToInt(damage * 0.5f));
            state.Hull = Mathf.Max(0, state.Hull - damage);
            ShipStatus inflicted = attacker != null ? BossModel.GetInflictedStatus(attacker.Boss) : ShipStatus.None;
            if (inflicted != ShipStatus.None && playerConditions != null)
                playerConditions.ApplyStatus(inflicted, 12f, attacker.Boss == BossKind.Poseidon ? 1.25f : 1f);
            Message?.Invoke(inflicted == ShipStatus.None
                ? $"ENEMY HIT  -{damage} HULL"
                : $"ENEMY HIT  -{damage}  {ShipStatusRuntime.GetName(inflicted)}");
            StateChanged?.Invoke();
            if (state.Hull == 0) RescueTow();
        }

        private string BuildPrompt(PoiRecord item, float distance)
        {
            if (player.IsAutoNavigating) return "HARBOR PILOT — 自動接岸中…";
            if (player.IsMoored) return item != null && item.Kind == PoiKind.Port ? "F PORT SERVICES" : string.Empty;
            if (item == null) return string.Empty;
            if (item.Kind == PoiKind.Enemy && distance <= 4.2f)
            {
                float bearing = ShipCustomizationModel.GetRelativeBearing(player.HeadingDegrees, player.LogicalPosition, item.LogicalPosition);
                SalvoSolution salvo = ShipCustomizationModel.GetSalvo(state, bearing);
                if (cannonCooldown > 0f) return "大砲を装填中…";
                if (salvo.CannonsInArc <= 0) return $"{salvo.ArcName} 射界なし — 船を旋回";
                if (salvo.CannonsFiring <= 0) return $"{salvo.ArcName} 砲員が必要";
                return $"SPACE {salvo.ArcName}斉射  {salvo.CannonsFiring}/{salvo.CannonsInArc}門";
            }
            if (item.Kind == PoiKind.Port && distance <= 3.4f) return "F AUTO-DOCK  港へ入港";
            if (distance > 1.35f) return string.Empty;
            if (Mathf.Abs(player.Speed) > 1.15f) return "速度を落として接近";
            return item.Kind == PoiKind.Port ? "F 港に入る" : item.Kind == PoiKind.Wreck ? "F 残骸を回収" : "F 宝を引き上げる";
        }

        private void Interact(PoiRecord item, float distance)
        {
            if (item.Kind == PoiKind.Port)
            {
                if (distance > 3.4f || player.IsAutoNavigating) return;
                if (player.IsMoored)
                {
                    PortRequested?.Invoke();
                    return;
                }
                player.BeginDocking(item.LogicalPosition, () =>
                {
                    Message?.Invoke("係留完了 — 港内は安全です");
                    PortRequested?.Invoke();
                    StateChanged?.Invoke();
                });
                Message?.Invoke("水先案内人が操船を引き継ぎました");
                return;
            }
            if (distance > 1.35f || Mathf.Abs(player.Speed) > 1.15f || item.Kind == PoiKind.Enemy) return;
            int gold = item.Reward + (item.Kind == PoiKind.Treasure ? 18 : 0);
            state.Gold += gold;
            string cargo = string.Empty;
            if (item.Kind == PoiKind.Wreck)
            {
                state.Supplies += 1 + item.Reward % 3;
                RevealLocalChart(item.LogicalPosition);
                SalvageDrop[] drops = SalvageInventory.RollWreck(item.Id, item.Reward);
                foreach (SalvageDrop drop in drops) state.AddPart(drop.Kind, drop.Amount);
                cargo = $"  {SalvageInventory.GetDisplayName(drops[0].Kind)} +{drops[0].Amount}  {SalvageInventory.GetDisplayName(drops[1].Kind)} +{drops[1].Amount}";
            }
            else if (item.Kind == PoiKind.Treasure)
            {
                SalvageDrop drop = SalvageInventory.RollTreasure(item.Id);
                state.AddPart(drop.Kind, drop.Amount);
                cargo = $"  {SalvageInventory.GetDisplayName(drop.Kind)} +{drop.Amount}";
            }
            Resolve(item);
            Message?.Invoke(item.Kind == PoiKind.Wreck ? $"WRECK SALVAGED  {gold}G{cargo}" : $"TREASURE FOUND  {gold}G{cargo}");
        }

        private bool PlayerInSafeHarbor()
        {
            if (player.IsAutoNavigating || player.IsMoored) return true;
            foreach (PoiRecord item in items)
                if (!item.Resolved && item.Kind == PoiKind.Port && Vector2.Distance(item.LogicalPosition, player.LogicalPosition) < 2.8f) return true;
            return false;
        }

        private void RevealLocalChart(Vector2 position)
        {
            int centerX = Mathf.FloorToInt(position.x / WorldGenerator.ChunkSize);
            int centerY = Mathf.FloorToInt(position.y / WorldGenerator.ChunkSize);
            for (int y = centerY - 1; y <= centerY + 1; y++)
            for (int x = centerX - 1; x <= centerX + 1; x++) state.ExploredChunks.Add(GameState.PackChunk(x, y));
        }

        private void FireCannon()
        {
            if (cannonCooldown > 0f || player.IsAutoNavigating || player.IsMoored) return;
            PoiRecord target = FindCombatTarget(out float best);
            if (target == null) { Message?.Invoke("射程内に敵はいません"); return; }
            float bearing = ShipCustomizationModel.GetRelativeBearing(player.HeadingDegrees, player.LogicalPosition, target.LogicalPosition);
            SalvoSolution salvo = ShipCustomizationModel.GetSalvo(state, bearing);
            if (salvo.CannonsInArc <= 0) { Message?.Invoke($"{salvo.ArcName}側に砲台がありません — 船を旋回してください"); return; }
            if (salvo.CannonsFiring <= 0) { Message?.Invoke("砲台を操作する船員がいません"); return; }
            ShipCustomizationModel.GetFiringSlots(state, bearing, firingSlots);
            cannonCooldown = Mathf.Max(0.48f, (1.25f - state.CannonLevel * 0.08f) * CrewManagementModel.GetReloadMultiplier(state));
            int damage = ShipCustomizationModel.GetSalvoDamage(state, salvo);
            ulong salvoEntropy = WorldGenerator.Hash(state.WorldSeed, unchecked((int)target.Id), unchecked((int)(target.Id >> 32)), unchecked((int)++cannonSalvoSerial));
            target.Health -= damage;
            bool willSink = target.Health <= 0;
            target.IsUnderFire = true;
            target.IsSinking = willSink;
            Message?.Invoke($"{salvo.ArcName}斉射 {salvo.CannonsFiring}門 — FIRE!");

            Action impact = () =>
            {
                string statuses = ApplyPlayerRoundStatuses(target, salvoEntropy);
                Message?.Invoke(willSink
                    ? $"DIRECT HIT — {damage}  SINKING!"
                    : string.IsNullOrEmpty(statuses) ? $"DIRECT HIT — {damage} DAMAGE" : $"DIRECT HIT — {damage}  {statuses}");
            };
            Action completed = () =>
            {
                target.IsUnderFire = false;
                if (!willSink) return;
                CompleteDefeat(target);
            };
            if (combatVfx != null) combatVfx.PlayPlayerSalvo(player.Visual, firingSlots.ToArray(), target.Visual, willSink, impact, completed);
            else { impact(); completed(); }
        }

        private string ApplyPlayerRoundStatuses(PoiRecord target, ulong salvoEntropy)
        {
            if (target == null || target.Resolved) return string.Empty;
            var labels = new List<string>();
            if (RollStatusProc(CrewPerk.Firebrand, salvoEntropy))
            {
                target.Conditions.Apply(ShipStatus.Burning, CrewManagementModel.GetStatusDuration(state, CrewPerk.Firebrand, 8f));
                labels.Add("BURNING");
            }
            if (RollStatusProc(CrewPerk.VenomShot, salvoEntropy))
            {
                target.Conditions.Apply(ShipStatus.Poisoned, CrewManagementModel.GetStatusDuration(state, CrewPerk.VenomShot, 10f));
                labels.Add("POISON");
            }
            if (RollStatusProc(CrewPerk.FrostShot, salvoEntropy))
            {
                target.Conditions.Apply(ShipStatus.Frozen, CrewManagementModel.GetStatusDuration(state, CrewPerk.FrostShot, 8f));
                labels.Add("FROZEN");
            }
            if (RollStatusProc(CrewPerk.TarShot, salvoEntropy))
            {
                target.Conditions.Apply(ShipStatus.Sticky, CrewManagementModel.GetStatusDuration(state, CrewPerk.TarShot, 8f));
                labels.Add("STICKY");
            }
            return string.Join(" + ", labels);
        }

        private bool RollStatusProc(CrewPerk perk, ulong salvoEntropy)
        {
            if (state.GetEquippedPerkCount(CrewRole.Cannons, perk) <= 0) return false;
            ulong entropy = WorldGenerator.Hash(unchecked((int)salvoEntropy), unchecked((int)(salvoEntropy >> 32)), (int)perk, 8309);
            return CrewManagementModel.RollStatusProc(state, perk, entropy);
        }

        private PoiRecord FindCombatTarget(out float bestDistance)
        {
            PoiRecord target = null;
            bestDistance = 4.25f;
            foreach (PoiRecord item in items)
            {
                if (item.Resolved || item.IsUnderFire || item.IsSinking || item.Kind != PoiKind.Enemy) continue;
                float distance = Vector2.Distance(item.LogicalPosition, player.LogicalPosition);
                if (distance < bestDistance) { bestDistance = distance; target = item; }
            }
            return target;
        }

        private void BeginStatusSinking(PoiRecord target)
        {
            if (target == null || target.Resolved || target.IsSinking) return;
            target.IsSinking = true;
            target.IsUnderFire = true;
            Message?.Invoke("STATUS DAMAGE — ENEMY SHIP SINKING!");
            if (combatVfx != null) combatVfx.PlaySinking(target.Visual, () => CompleteDefeat(target));
            else CompleteDefeat(target);
        }

        private void CompleteDefeat(PoiRecord target)
        {
            if (target == null || target.Resolved) return;
            target.IsUnderFire = false;
            state.Gold += target.Reward;
            PerkDrop drop = BossModel.RollPerkDrop(target.Boss, target.Id);
            if (!drop.IsEmpty) state.AddPerk(drop.Perk, drop.Rank);
            Resolve(target);
            Message?.Invoke(drop.IsEmpty
                ? $"ENEMY SUNK — {target.Reward}G SALVAGED"
                : $"BOSS DEFEATED — {target.Reward}G  {CrewManagementModel.GetPerkName(drop.Perk)} {PerkRankModel.GetLabel(drop.Rank)}");
        }

        private static void AttachStatusVisual(PoiRecord record)
        {
            if (record == null || record.Kind != PoiKind.Enemy || record.Visual == null) return;
            record.StatusVisual = record.Visual.gameObject.AddComponent<ShipStatusVisualController>();
            record.StatusVisual.Initialize(record.Conditions);
        }

        private void Resolve(PoiRecord item)
        {
            item.Resolved = true;
            state.ResolvedEvents.Add(item.Id);
            item.Visual.gameObject.SetActive(false);
            StateChanged?.Invoke();
        }

        private void RescueTow()
        {
            int lost = Mathf.Min(state.Gold, Mathf.Max(10, state.Gold / 5));
            state.Gold -= lost;
            state.Hull = state.MaxHull;
            playerConditions?.ClearAll();
            player.TowTo(new Vector2(3.1f, 3.2f));
            Message?.Invoke($"救助船が最寄りの港へ曳航しました（-{lost}G）");
            StateChanged?.Invoke();
            RefreshChunks(true);
        }

        private IEnumerator CombatPreviewRoutine()
        {
            yield return new WaitForSeconds(4.95f);
            player.TowTo(Vector2.zero);
            RefreshChunks(true);
            foreach (PoiRecord item in items)
            {
                if (item.Kind != PoiKind.Enemy) continue;
                item.Resolved = true;
                item.Visual.gameObject.SetActive(false);
            }
            state.Crew = Mathf.Max(state.Crew, 2);
            state.CannonMountMask |= 1 << (int)CannonSlot.Bow;
            player.RefreshCustomizationVisual();
            float radians = player.HeadingDegrees * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
            PoiRecord enemy = AddPreviewPoi(0xD35C70A1UL, PoiKind.Enemy, player.LogicalPosition + direction * 2.35f, 42);
            enemy.Visual.localScale *= 1.3f;
            enemy.Health = 99;
            enemyFireCooldown = 999f;
            yield return null;
            FireCannon();
            yield return new WaitForSeconds(1.4f);
            enemy.Health = 1;
            FireCannon();
        }

        private IEnumerator DockPreviewRoutine()
        {
            yield return new WaitForSeconds(3.8f);
            Vector2 portPosition = Vector2.zero;
            player.TowTo(DockingModel.GetApproach(portPosition) + Vector2.down * 0.55f);
            RefreshChunks(true);
            PoiRecord port = AddPreviewPoi(0xD0C0A11UL, PoiKind.Port, portPosition, 0);
            Interact(port, Vector2.Distance(player.LogicalPosition, portPosition));
        }

        private IEnumerator BossPreviewRoutine()
        {
            yield return new WaitForSeconds(0.3f);
            player.TowTo(Vector2.zero);
            RefreshChunks(true);
            foreach (PoiRecord item in items) { item.Resolved = true; item.Visual.gameObject.SetActive(false); }
            (BossKind boss, Vector2 position)[] lineup =
            {
                (BossKind.GangAdmiral, new Vector2(-3.0f, 1.9f)),
                (BossKind.GhostShip, new Vector2(3.0f, 1.9f)),
                (BossKind.Kraken, new Vector2(-3.0f, -2.0f)),
                (BossKind.Poseidon, new Vector2(3.0f, -2.0f))
            };
            for (int i = 0; i < lineup.Length; i++)
            {
                PoiRecord boss = AddPreviewPoi(0xB0550000UL + (ulong)i, PoiKind.Enemy, lineup[i].position, 0, lineup[i].boss);
                boss.IsPreview = true;
                boss.Conditions.Apply((ShipStatus)i, 999f);
            }
        }

        private PoiRecord AddPreviewPoi(ulong id, PoiKind kind, Vector2 position, int reward, BossKind boss = BossKind.None)
        {
            Transform visual = ProceduralSceneFactory.CreatePoiVisual(kind, transform, boss);
            visual.gameObject.SetActive(true);
            Vector2 relative = position - player.LogicalPosition;
            visual.localPosition = new Vector3(relative.x, 0.30f, relative.y);
            int health = boss != BossKind.None ? BossModel.GetHull(boss) : kind == PoiKind.Enemy ? 180 : 1;
            var record = new PoiRecord
            {
                Id = id,
                Kind = kind,
                LogicalPosition = position,
                SpawnPosition = position,
                Reward = reward,
                Health = health,
                MaxHealth = health,
                Boss = boss,
                Visual = visual
            };
            AttachStatusVisual(record);
            items.Add(record);
            return record;
        }
    }
}
