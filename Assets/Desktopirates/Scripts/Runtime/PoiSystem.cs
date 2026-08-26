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
        public int SourceChunkX;
        public int SourceChunkY;
        public int Reward;
        public int Health;
        public int MaxHealth;
        public int Level;
        public string DisplayName;
        public BossKind Boss;
        public BossMutation BossMutation;
        public EnemyArchetype EnemyArchetype;
        public float NextFireTime;
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
        public const float SalvageRange = 1.70f;
        public IReadOnlyList<PoiRecord> Items => items;
        public string InteractionPrompt { get; private set; }
        public bool AttackMode { get; private set; }
        public event Action<string> Message;
        public event Action PortRequested;
        public event Action StateChanged;

        private readonly List<PoiRecord> items = new List<PoiRecord>();
        private readonly List<GeneratedEventData> generated = new List<GeneratedEventData>();
        private readonly HashSet<long> loadedChunks = new HashSet<long>();
        private BoatController player;
        private GameState state;
        private CombatVfxController combatVfx;
        private PlayerShipConditionController playerConditions;
        private readonly List<CannonSlot> firingSlots = new List<CannonSlot>();
        private int centerChunkX = int.MinValue;
        private int centerChunkY = int.MinValue;
        private readonly float[] cannonReadyAt = new float[ShipCustomizationModel.CannonSlotCount];
        private ulong cannonSalvoSerial;
        private ulong enemyShotSerial;

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
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--enemy-types-preview-a")) StartCoroutine(EnemyTypesPreviewRoutine(0));
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--enemy-types-preview-b")) StartCoroutine(EnemyTypesPreviewRoutine(4));
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--tag-scale-preview")) StartCoroutine(TagScalePreviewRoutine());
        }

        private void Update()
        {
            if (player == null) return;
            RefreshChunks(false);

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
            if (Input.GetKeyDown(KeyCode.Space)) ToggleAttackMode();
            if (AttackMode) TryAutomaticFire();
        }

        private void RefreshChunks(bool force)
        {
            Vector2Int centerChunk = WorldStreamingModel.GetCenterChunk(player.LogicalPosition);
            int chunkX = centerChunk.x;
            int chunkY = centerChunk.y;
            state.ExploredChunks.Add(GameState.PackChunk(chunkX, chunkY));
            if (!force && chunkX == centerChunkX && chunkY == centerChunkY) return;
            centerChunkX = chunkX;
            centerChunkY = chunkY;

            // The chart and the sea must advance from the same infinite-world source.  The
            // previous implementation paused all streaming during combat, sinking and
            // auto-docking. A moving ship could therefore enter newly explored chunks while
            // the chart showed their landmarks but the sea still owned the old POI set.
            // Reconcile chunks incrementally instead: active VFX survive, old idle chunks are
            // released, and newly entered chunks are always populated.
            if (force) ClearStreamedItems();

            var desiredChunks = new HashSet<long>();
            for (int y = chunkY - WorldStreamingModel.LoadRadius; y <= chunkY + WorldStreamingModel.LoadRadius; y++)
            for (int x = chunkX - WorldStreamingModel.LoadRadius; x <= chunkX + WorldStreamingModel.LoadRadius; x++)
                desiredChunks.Add(GameState.PackChunk(x, y));

            for (int index = items.Count - 1; index >= 0; index--)
            {
                PoiRecord item = items[index];
                long source = GameState.PackChunk(item.SourceChunkX, item.SourceChunkY);
                if (desiredChunks.Contains(source) || item.IsUnderFire || item.IsSinking || item.IsPreview) continue;
                if (item.Visual != null) Destroy(item.Visual.gameObject);
                items.RemoveAt(index);
            }

            for (int y = chunkY - WorldStreamingModel.LoadRadius; y <= chunkY + WorldStreamingModel.LoadRadius; y++)
            for (int x = chunkX - WorldStreamingModel.LoadRadius; x <= chunkX + WorldStreamingModel.LoadRadius; x++)
            {
                long packedChunk = GameState.PackChunk(x, y);
                if (!force && loadedChunks.Contains(packedChunk)) continue;
                WorldGenerator.GenerateChunk(state.WorldSeed, x, y, generated);
                foreach (GeneratedEventData data in generated)
                {
                    if (state.ResolvedEvents.Contains(data.Id)) continue;
                    if (items.Exists(item => item.Id == data.Id)) continue;
                    int level = EnemyIdentityModel.GetLevel(data.Boss, data.Reward, data.Position);
                    BossMutation mutation = BossMutationModel.Roll(data.Id, data.Boss, data.Position);
                    Transform visual = ProceduralSceneFactory.CreatePoiVisual(data.Kind, transform, data.Boss, data.EnemyArchetype, mutation);
                    visual.gameObject.SetActive(false);
                    int health = data.Boss != BossKind.None
                        ? BossMutationModel.ApplyHull(BossModel.GetHull(data.Boss), mutation)
                        : data.Kind == PoiKind.Enemy ? EnemyArchetypeModel.GetHull(data.EnemyArchetype, data.Reward, level) : 1;
                    string baseName = EnemyIdentityModel.GetName(data.Id, data.Boss, data.EnemyArchetype);
                    var record = new PoiRecord
                    {
                        Id = data.Id,
                        Kind = data.Kind,
                        LogicalPosition = data.Position,
                        SpawnPosition = data.Position,
                        SourceChunkX = x,
                        SourceChunkY = y,
                        Reward = data.Reward,
                        Health = health,
                        MaxHealth = health,
                        Level = level,
                        DisplayName = mutation == BossMutation.None ? baseName : $"{BossMutationModel.GetLabel(mutation)} {baseName}",
                        Boss = data.Boss,
                        BossMutation = mutation,
                        EnemyArchetype = data.EnemyArchetype,
                        NextFireTime = Time.time + 0.8f + (data.Id & 255UL) / 255f * 1.5f,
                        Visual = visual
                    };
                    AttachStatusVisual(record);
                    items.Add(record);
                }
            }

            loadedChunks.Clear();
            foreach (long packedChunk in desiredChunks) loadedChunks.Add(packedChunk);
        }

        private void ClearStreamedItems()
        {
            foreach (PoiRecord item in items)
                if (item.Visual != null) Destroy(item.Visual.gameObject);
            items.Clear();
            loadedChunks.Clear();
        }

        private void UpdateEnemy(PoiRecord enemy)
        {
            Vector2 toPlayer = player.LogicalPosition - enemy.LogicalPosition;
            float distance = toPlayer.magnitude;
            bool harborSafe = PlayerInSafeHarbor();
            EnemyProfile profile = EnemyArchetypeModel.Get(enemy.EnemyArchetype);
            SeaRegionProfile region = SeaRegionModel.At(state.WorldSeed, enemy.LogicalPosition);
            float frozenSpeed = enemy.Conditions.IsActive(ShipStatus.Frozen) ? 0.55f : 1f;
            float stickyTurn = enemy.Conditions.IsActive(ShipStatus.Sticky) ? 0.55f : 1f;
            float desiredRange = enemy.Boss != BossKind.None ? 1.65f : profile.PreferredRange;
            float pursuitSpeed = 1.15f * profile.SpeedMultiplier * BossMutationModel.SpeedMultiplier(enemy.BossMutation) * region.EnemySpeedMultiplier;
            float aggressionRange = enemy.Boss != BossKind.None ? 8.8f : 7.2f;
            if (!harborSafe && distance < aggressionRange && distance > desiredRange)
                enemy.LogicalPosition += toPlayer.normalized * Time.deltaTime * pursuitSpeed * frozenSpeed;
            else if (distance >= aggressionRange)
            {
                float phase = (float)(enemy.Id & 1023UL) * 0.015f + Time.time * 0.18f;
                Vector2 patrolTarget = enemy.SpawnPosition + new Vector2(Mathf.Sin(phase), Mathf.Cos(phase)) * 1.2f;
                enemy.LogicalPosition = Vector2.MoveTowards(enemy.LogicalPosition, patrolTarget, Time.deltaTime * 0.55f * profile.SpeedMultiplier * frozenSpeed);
            }
            Vector2 heading = player.LogicalPosition - enemy.LogicalPosition;
            if (heading.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.Euler(0f, Mathf.Atan2(heading.x, heading.y) * Mathf.Rad2Deg, 0f);
                enemy.Visual.localRotation = Quaternion.RotateTowards(enemy.Visual.localRotation, targetRotation, Time.deltaTime * 105f * stickyTurn * profile.SpeedMultiplier);
            }

            float fireRange = enemy.Boss != BossKind.None ? 2.85f : profile.FireRange;
            if (!harborSafe && distance < fireRange && Time.time >= enemy.NextFireTime)
            {
                float reload = (enemy.Boss != BossKind.None ? 2.70f : profile.ReloadSeconds)
                    * BossMutationModel.ReloadMultiplier(enemy.BossMutation) * region.EnemyReloadMultiplier;
                enemy.NextFireTime = Time.time + reload;
                if (combatVfx != null) combatVfx.PlayEnemyShot(enemy.Visual, player.Visual, () => ApplyEnemyHit(enemy));
                else ApplyEnemyHit(enemy);
            }
        }

        private void ApplyEnemyHit(PoiRecord attacker)
        {
            int damage = attacker == null ? 1 : attacker.Boss != BossKind.None
                ? Mathf.RoundToInt(BossModel.GetContactDamage(attacker.Boss) * BossMutationModel.DamageMultiplier(attacker.BossMutation))
                : EnemyArchetypeModel.GetDamage(attacker.EnemyArchetype, attacker.Level);
            if (attacker != null && attacker.Conditions.IsActive(ShipStatus.Poisoned)) damage = Mathf.Max(1, Mathf.CeilToInt(damage * 0.5f));
            state.Hull = Mathf.Max(0, state.Hull - damage);
            ShipStatus inflicted = RollEnemyStatus(attacker);
            if (inflicted != ShipStatus.None && playerConditions != null)
                playerConditions.ApplyStatus(inflicted, 12f, attacker.Boss == BossKind.Poseidon ? 1.25f * BossMutationModel.StatusPotency(attacker.BossMutation) : BossMutationModel.StatusPotency(attacker.BossMutation));
            Message?.Invoke(inflicted == ShipStatus.None
                ? $"ENEMY HIT  -{damage} HULL"
                : $"ENEMY HIT  -{damage}  {ShipStatusRuntime.GetName(inflicted)}");
            StateChanged?.Invoke();
            if (state.Hull == 0) RescueTow();
        }

        private ShipStatus RollEnemyStatus(PoiRecord attacker)
        {
            if (attacker == null) return ShipStatus.None;
            if (attacker.Boss != BossKind.None) return BossModel.GetInflictedStatus(attacker.Boss);
            EnemyProfile profile = EnemyArchetypeModel.Get(attacker.EnemyArchetype);
            if (profile.Status == ShipStatus.None) return ShipStatus.None;
            float chance = profile.StatusChance;
            SeaRegionKind region = SeaRegionModel.At(state.WorldSeed, attacker.LogicalPosition).Kind;
            if ((region == SeaRegionKind.EmberCurrent && profile.Status == ShipStatus.Burning)
                || (region == SeaRegionKind.Miasma && profile.Status == ShipStatus.Poisoned)
                || (region == SeaRegionKind.Frostwake && profile.Status == ShipStatus.Frozen)
                || (region == SeaRegionKind.TarSea && profile.Status == ShipStatus.Sticky)) chance += 0.16f;
            ulong entropy = WorldGenerator.Hash(state.WorldSeed, unchecked((int)attacker.Id), unchecked((int)++enemyShotSerial), 9143);
            return (entropy & 0xFFFFUL) / 65535f < chance ? profile.Status : ShipStatus.None;
        }

        private string BuildPrompt(PoiRecord item, float distance)
        {
            if (player.IsDocking) return GameLocalization.Choose("HARBOR PILOT — AUTO-DOCKING…", "HARBOR PILOT — 自動接岸中…");
            if (player.IsMoored) return item != null && item.Kind == PoiKind.Port ? GameLocalization.Choose("F  PORT SERVICES", "F  港湾サービス") : string.Empty;
            if (item != null && item.Kind == PoiKind.Enemy && distance <= GetLongestCannonRange())
            {
                float bearing = ShipCustomizationModel.GetRelativeBearing(player.HeadingDegrees, player.LogicalPosition, item.LogicalPosition);
                SalvoSolution salvo = ShipCustomizationModel.GetSalvo(state, bearing);
                if (!AttackMode) return GameLocalization.Choose("SPACE  ENGAGE ATTACK MODE", "SPACE  攻撃モードへ");
                if (salvo.CannonsInArc <= 0) return GameLocalization.Choose($"ATTACK MODE — TURN FOR A FIRING ARC", $"ATTACK MODE — {salvo.ArcName} 射界なし");
                if (salvo.CannonsFiring <= 0) return GameLocalization.Choose("ATTACK MODE — CANNON CREW REQUIRED", "ATTACK MODE — 砲員が必要");
                return GameLocalization.Choose($"ATTACK MODE — AUTO FIRE  {salvo.CannonsFiring} GUNS", $"ATTACK MODE — 自動砲撃  {salvo.CannonsFiring}門");
            }
            if (item != null && item.Kind == PoiKind.Port && distance <= 3.4f)
                return GameLocalization.Choose("F  AUTO-DOCK", "F  港へ自動入港");
            if (item != null && distance <= SalvageRange && item.Kind != PoiKind.Enemy)
                return item.Kind == PoiKind.Wreck
                    ? GameLocalization.Choose("F  SALVAGE WRECK", "F  残骸を回収")
                    : GameLocalization.Choose("F  RECOVER TREASURE", "F  宝を引き上げる");
            return AttackMode
                ? GameLocalization.Choose("ATTACK MODE — AUTO FIRE", "ATTACK MODE — 自動砲撃")
                : GameLocalization.Choose("WATCH MODE — SPACE TO ENGAGE", "WATCH MODE — SPACEで攻撃態勢");
        }

        private void Interact(PoiRecord item, float distance)
        {
            if (item.Kind == PoiKind.Port)
            {
                if (distance > 3.4f || player.IsDocking) return;
                if (player.IsMoored)
                {
                    PortRequested?.Invoke();
                    return;
                }
                player.BeginDocking(item.LogicalPosition, () =>
                {
                    state.Captain.PortCalls++;
                    Message?.Invoke("係留完了 — 港内は安全です");
                    PortRequested?.Invoke();
                    StateChanged?.Invoke();
                });
                Message?.Invoke("水先案内人が操船を引き継ぎました");
                return;
            }
            if (distance > SalvageRange || item.Kind == PoiKind.Enemy) return;
            int gold = item.Reward + (item.Kind == PoiKind.Treasure ? 18 : 0);
            state.Gold += gold;
            state.Captain.GoldEarned += gold;
            string cargo = string.Empty;
            if (item.Kind == PoiKind.Wreck)
            {
                state.Captain.WrecksSalvaged++;
                state.Supplies += 1 + item.Reward % 3;
                RevealLocalChart(item.LogicalPosition);
                SalvageDrop[] drops = SalvageInventory.RollWreck(item.Id, item.Reward);
                foreach (SalvageDrop drop in drops) state.AddPart(drop.Kind, drop.Amount);
                cargo = $"  {SalvageInventory.GetDisplayName(drops[0].Kind)} +{drops[0].Amount}  {SalvageInventory.GetDisplayName(drops[1].Kind)} +{drops[1].Amount}";
            }
            else if (item.Kind == PoiKind.Treasure)
            {
                state.Captain.TreasuresFound++;
                SalvageDrop drop = SalvageInventory.RollTreasure(item.Id);
                state.AddPart(drop.Kind, drop.Amount);
                cargo = $"  {SalvageInventory.GetDisplayName(drop.Kind)} +{drop.Amount}";
            }
            Resolve(item);
            Message?.Invoke(item.Kind == PoiKind.Wreck ? $"WRECK SALVAGED  {gold}G{cargo}" : $"TREASURE FOUND  {gold}G{cargo}");
        }

        private bool PlayerInSafeHarbor()
        {
            if (player.IsDocking || player.IsMoored) return true;
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

        private void ToggleAttackMode()
        {
            if (player.IsDocking || player.IsMoored) return;
            SetAttackMode(!AttackMode, true);
        }

        public void SetAttackMode(bool enabled, bool announce = true)
        {
            if (AttackMode == enabled) return;
            AttackMode = enabled;
            if (announce)
                Message?.Invoke(AttackMode
                    ? GameLocalization.Choose("ATTACK MODE — BATTERIES WILL FIRE AUTOMATICALLY", "ATTACK MODE — 各砲台が自動砲撃します")
                    : GameLocalization.Choose("WATCH MODE — FIRE CONTROL SAFE", "WATCH MODE — 砲撃を停止しました"));
            StateChanged?.Invoke();
        }

        private void TryAutomaticFire(bool previewOverride = false)
        {
            if ((!AttackMode && !previewOverride) || player.IsDocking || player.IsMoored) return;
            PoiRecord target = FindAutomaticTarget(out float best);
            if (target == null) return;
            float bearing = ShipCustomizationModel.GetRelativeBearing(player.HeadingDegrees, player.LogicalPosition, target.LogicalPosition);
            SalvoSolution salvo = ShipCustomizationModel.GetSalvo(state, bearing);
            ShipCustomizationModel.GetFiringSlots(state, bearing, firingSlots);
            for (int i = firingSlots.Count - 1; i >= 0; i--)
            {
                CannonSlot slot = firingSlots[i];
                if (Time.time < cannonReadyAt[(int)slot] || best > CannonUpgradeModel.GetRange(state, slot)) firingSlots.RemoveAt(i);
            }
            if (firingSlots.Count == 0) return;
            int damage = 0;
            for (int i = 0; i < firingSlots.Count; i++)
            {
                CannonSlot slot = firingSlots[i];
                damage += CannonUpgradeModel.GetDamage(state, slot);
                cannonReadyAt[(int)slot] = Time.time + CannonUpgradeModel.GetReloadSeconds(state, slot);
            }
            ulong salvoEntropy = WorldGenerator.Hash(state.WorldSeed, unchecked((int)target.Id), unchecked((int)(target.Id >> 32)), unchecked((int)++cannonSalvoSerial));
            state.Captain.DamageDealt += Mathf.Min(target.Health, damage);
            target.Health -= damage;
            bool willSink = target.Health <= 0;
            target.IsUnderFire = true;
            target.IsSinking = willSink;
            Message?.Invoke(GameLocalization.Choose(
                $"{salvo.ArcName} AUTO SALVO — {firingSlots.Count} GUNS — FIRE!",
                $"{salvo.ArcName} 自動斉射 — {firingSlots.Count}門 — FIRE!"));

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

        private PoiRecord FindAutomaticTarget(out float bestDistance)
        {
            PoiRecord target = null;
            bestDistance = float.MaxValue;
            foreach (PoiRecord item in items)
            {
                if (item.Resolved || item.IsUnderFire || item.IsSinking || item.Kind != PoiKind.Enemy) continue;
                float distance = Vector2.Distance(item.LogicalPosition, player.LogicalPosition);
                if (distance >= bestDistance) continue;
                float bearing = ShipCustomizationModel.GetRelativeBearing(player.HeadingDegrees, player.LogicalPosition, item.LogicalPosition);
                ShipCustomizationModel.GetFiringSlots(state, bearing, firingSlots);
                bool canFire = false;
                for (int i = 0; i < firingSlots.Count; i++)
                {
                    CannonSlot slot = firingSlots[i];
                    if (Time.time >= cannonReadyAt[(int)slot] && distance <= CannonUpgradeModel.GetRange(state, slot)) { canFire = true; break; }
                }
                if (!canFire) continue;
                bestDistance = distance;
                target = item;
            }
            return target;
        }

        private float GetLongestCannonRange()
        {
            float range = CannonUpgradeModel.BaseRange;
            for (int i = 0; i < ShipCustomizationModel.CannonSlotCount; i++)
                if (ShipCustomizationModel.HasCannon(state, (CannonSlot)i))
                    range = Mathf.Max(range, CannonUpgradeModel.GetRange(state, (CannonSlot)i));
            return range;
        }

        public PoiRecord FindNearestActiveEnemy(float range)
        {
            PoiRecord nearest = null;
            float bestDistanceSquared = range * range;
            foreach (PoiRecord item in items)
            {
                if (item.Resolved || item.IsSinking || item.Kind != PoiKind.Enemy) continue;
                float distanceSquared = (item.LogicalPosition - player.LogicalPosition).sqrMagnitude;
                if (distanceSquared >= bestDistanceSquared) continue;
                bestDistanceSquared = distanceSquared;
                nearest = item;
            }
            return nearest;
        }

        public bool TryAutoCollectNearby(bool wrecks, bool treasures)
        {
            if ((!wrecks && !treasures) || player.IsDocking || player.IsMoored) return false;
            PoiRecord nearest = null;
            float bestDistance = AutoVoyageModel.AutoCollectRange;
            foreach (PoiRecord item in items)
            {
                if (item.Resolved || (item.Kind != PoiKind.Wreck && item.Kind != PoiKind.Treasure)) continue;
                if ((item.Kind == PoiKind.Wreck && !wrecks) || (item.Kind == PoiKind.Treasure && !treasures)) continue;
                float distance = Vector2.Distance(item.LogicalPosition, player.LogicalPosition);
                if (distance > bestDistance) continue;
                bestDistance = distance;
                nearest = item;
            }
            if (nearest == null) return false;
            Interact(nearest, Mathf.Min(bestDistance, SalvageRange));
            return nearest.Resolved;
        }

        public bool TryAutoDock(ulong portId)
        {
            if (player.IsDocking || player.IsMoored) return false;
            foreach (PoiRecord item in items)
            {
                if (item.Resolved || item.Id != portId || item.Kind != PoiKind.Port) continue;
                float distance = Vector2.Distance(item.LogicalPosition, player.LogicalPosition);
                if (distance > AutoVoyageModel.PortPilotRange) return false;
                Interact(item, distance);
                return player.IsDocking;
            }
            return false;
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
            int reward = target.Boss == BossKind.None
                ? target.Reward
                : Mathf.RoundToInt(target.Reward * BossMutationModel.RewardMultiplier(target.BossMutation));
            state.Gold += reward;
            state.Captain.GoldEarned += reward;
            if (target.Boss == BossKind.None) state.Captain.RecordEnemy(target.EnemyArchetype);
            else state.Captain.RecordBoss(target.Boss, target.BossMutation);
            PerkDrop drop = BossModel.RollPerkDrop(target.Boss, target.Id);
            if (!drop.IsEmpty) state.AddPerk(drop.Perk, drop.Rank);
            Resolve(target);
            Message?.Invoke(drop.IsEmpty
                ? $"ENEMY SUNK — {reward}G SALVAGED"
                : $"BOSS DEFEATED — {reward}G  {CrewManagementModel.GetPerkName(drop.Perk)} {PerkRankModel.GetLabel(drop.Rank)}");
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
            enemy.NextFireTime = float.PositiveInfinity;
            yield return null;
            TryAutomaticFire(true);
            yield return new WaitForSeconds(1.4f);
            enemy.Health = 1;
            TryAutomaticFire(true);
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

        private IEnumerator EnemyTypesPreviewRoutine(int startIndex)
        {
            yield return new WaitForSeconds(0.3f);
            player.TowTo(Vector2.zero);
            RefreshChunks(true);
            foreach (PoiRecord item in items) { item.Resolved = true; item.Visual.gameObject.SetActive(false); }
            Vector2[] positions =
            {
                new Vector2(-2.9f, 2.0f), new Vector2(2.9f, 2.0f),
                new Vector2(-2.9f, -2.0f), new Vector2(2.9f, -2.0f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                EnemyArchetype archetype = (EnemyArchetype)(startIndex + i);
                PoiRecord enemy = AddPreviewPoi(0xE1100000UL + (ulong)(startIndex + i), PoiKind.Enemy, positions[i], 35 + i * 5, BossKind.None, archetype);
                enemy.IsPreview = true;
                enemy.NextFireTime = float.PositiveInfinity;
                // The gallery doubles as a visual regression scene for the HUD: four
                // distinct ratios make a broken or stale fill immediately obvious.
                enemy.Health = Mathf.Max(1, Mathf.RoundToInt(enemy.MaxHealth * ((i + 1f) / positions.Length)));
            }
        }

        private IEnumerator TagScalePreviewRoutine()
        {
            yield return new WaitForSeconds(0.3f);
            player.TowTo(Vector2.zero);
            RefreshChunks(true);
            foreach (PoiRecord item in items) { item.Resolved = true; item.Visual.gameObject.SetActive(false); }
            AddPreviewPoi(0x7A600001UL, PoiKind.Wreck, new Vector2(-8f, 8f), 20);
            AddPreviewPoi(0x7A600002UL, PoiKind.Enemy, new Vector2(22f, 22f), 30, BossKind.None, EnemyArchetype.Gunboat);
            AddPreviewPoi(0x7A600003UL, PoiKind.Treasure, new Vector2(42f, -42f), 40);
            AddPreviewPoi(0x7A600004UL, PoiKind.Port, new Vector2(-62f, -62f), 0);
        }

        private PoiRecord AddPreviewPoi(ulong id, PoiKind kind, Vector2 position, int reward, BossKind boss = BossKind.None, EnemyArchetype? previewArchetype = null)
        {
            EnemyArchetype archetype = previewArchetype ?? EnemyArchetypeModel.Roll(id, position);
            BossMutation mutation = BossMutationModel.Roll(id, boss, position);
            Transform visual = ProceduralSceneFactory.CreatePoiVisual(kind, transform, boss, archetype, mutation);
            visual.gameObject.SetActive(true);
            Vector2 relative = position - player.LogicalPosition;
            visual.localPosition = new Vector3(relative.x, 0.30f, relative.y);
            int level = EnemyIdentityModel.GetLevel(boss, reward, position);
            int health = boss != BossKind.None ? BossMutationModel.ApplyHull(BossModel.GetHull(boss), mutation)
                : kind == PoiKind.Enemy ? EnemyArchetypeModel.GetHull(archetype, reward, level) : 1;
            string baseName = EnemyIdentityModel.GetName(id, boss, archetype);
            var record = new PoiRecord
            {
                Id = id,
                Kind = kind,
                LogicalPosition = position,
                SpawnPosition = position,
                Reward = reward,
                Health = health,
                MaxHealth = health,
                Level = level,
                DisplayName = mutation == BossMutation.None ? baseName : $"{BossMutationModel.GetLabel(mutation)} {baseName}",
                Boss = boss,
                BossMutation = mutation,
                EnemyArchetype = archetype,
                Visual = visual
            };
            AttachStatusVisual(record);
            items.Add(record);
            return record;
        }
    }
}
