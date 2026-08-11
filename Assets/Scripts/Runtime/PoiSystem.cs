using System;
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
        public bool Resolved;
        public Transform Visual;
    }

    public sealed class PoiSystem : MonoBehaviour
    {
        public const float VisibleRadius = 6.1f;
        public IReadOnlyList<PoiRecord> Items => items;
        public string InteractionPrompt { get; private set; }
        public event Action<string> Message;
        public event Action PortRequested;
        public event Action StateChanged;

        private readonly List<PoiRecord> items = new List<PoiRecord>();
        private readonly List<GeneratedEventData> generated = new List<GeneratedEventData>();
        private BoatController player;
        private GameState state;
        private int centerChunkX = int.MinValue;
        private int centerChunkY = int.MinValue;
        private float enemyFireCooldown;
        private float cannonCooldown;

        public void Initialize(BoatController boat, GameState gameState)
        {
            player = boat;
            state = gameState;
            RefreshChunks(true);
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
                if (item.Kind == PoiKind.Enemy) UpdateEnemy(item);
                Vector2 relative = item.LogicalPosition - player.LogicalPosition;
                float distance = relative.magnitude;
                if (distance < closestDistance) { closest = item; closestDistance = distance; }
                bool visible = distance <= VisibleRadius;
                if (item.Visual.gameObject.activeSelf != visible) item.Visual.gameObject.SetActive(visible);
                if (!visible) continue;
                item.Visual.localPosition = new Vector3(relative.x, 0.30f, relative.y);
                if (item.Kind != PoiKind.Enemy) item.Visual.localRotation = Quaternion.identity;
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
                    Transform visual = ProceduralSceneFactory.CreatePoiVisual(data.Kind, transform);
                    visual.gameObject.SetActive(false);
                    items.Add(new PoiRecord
                    {
                        Id = data.Id,
                        Kind = data.Kind,
                        LogicalPosition = data.Position,
                        SpawnPosition = data.Position,
                        Reward = data.Reward,
                        Health = data.Kind == PoiKind.Enemy ? 2 + state.CannonLevel / 2 : 1,
                        Visual = visual
                    });
                }
            }
        }

        private void UpdateEnemy(PoiRecord enemy)
        {
            Vector2 toPlayer = player.LogicalPosition - enemy.LogicalPosition;
            float distance = toPlayer.magnitude;
            bool harborSafe = PlayerInSafeHarbor();
            if (!harborSafe && distance < 7.2f && distance > 1.45f)
                enemy.LogicalPosition += toPlayer.normalized * Time.deltaTime * 1.15f;
            else if (distance >= 7.2f)
            {
                float phase = (float)(enemy.Id & 1023UL) * 0.015f + Time.time * 0.18f;
                Vector2 patrolTarget = enemy.SpawnPosition + new Vector2(Mathf.Sin(phase), Mathf.Cos(phase)) * 1.2f;
                enemy.LogicalPosition = Vector2.MoveTowards(enemy.LogicalPosition, patrolTarget, Time.deltaTime * 0.55f);
            }
            Vector2 heading = player.LogicalPosition - enemy.LogicalPosition;
            if (heading.sqrMagnitude > 0.01f) enemy.Visual.localRotation = Quaternion.Euler(0f, Mathf.Atan2(heading.x, heading.y) * Mathf.Rad2Deg, 0f);

            if (!harborSafe && distance < 2.35f && enemyFireCooldown <= 0f)
            {
                enemyFireCooldown = 2.7f;
                state.Hull = Mathf.Max(0, state.Hull - 1);
                Message?.Invoke("敵の砲撃！ 船体 -1");
                StateChanged?.Invoke();
                if (state.Hull == 0) RescueTow();
            }
        }

        private string BuildPrompt(PoiRecord item, float distance)
        {
            if (item == null) return string.Empty;
            if (item.Kind == PoiKind.Enemy && distance <= 4.2f) return cannonCooldown <= 0f ? "SPACE 砲撃" : "大砲を装填中…";
            if (distance > 1.35f) return string.Empty;
            if (Mathf.Abs(player.Speed) > 1.15f) return "速度を落として接近";
            return item.Kind == PoiKind.Port ? "F 港に入る" : item.Kind == PoiKind.Wreck ? "F 残骸を回収" : "F 宝を引き上げる";
        }

        private void Interact(PoiRecord item, float distance)
        {
            if (distance > 1.35f || Mathf.Abs(player.Speed) > 1.15f || item.Kind == PoiKind.Enemy) return;
            if (item.Kind == PoiKind.Port)
            {
                PortRequested?.Invoke();
                Message?.Invoke("入港しました。ここは安全地帯です");
                return;
            }
            int gold = item.Reward + (item.Kind == PoiKind.Treasure ? 18 : 0);
            state.Gold += gold;
            if (item.Kind == PoiKind.Wreck) state.Supplies += 1 + item.Reward % 3;
            if (item.Kind == PoiKind.Wreck) RevealLocalChart(item.LogicalPosition);
            Resolve(item);
            Message?.Invoke(item.Kind == PoiKind.Wreck ? $"残骸を回収：{gold}G と補給品" : $"宝を発見：{gold}G");
        }

        private bool PlayerInSafeHarbor()
        {
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
            if (cannonCooldown > 0f) return;
            PoiRecord target = null;
            float best = 4.25f;
            foreach (PoiRecord item in items)
            {
                if (item.Resolved || item.Kind != PoiKind.Enemy) continue;
                float distance = Vector2.Distance(item.LogicalPosition, player.LogicalPosition);
                if (distance < best) { best = distance; target = item; }
            }
            if (target == null) { Message?.Invoke("射程内に敵はいません"); return; }
            cannonCooldown = Mathf.Max(0.65f, 1.25f - state.CannonLevel * 0.08f);
            target.Health--;
            if (target.Health <= 0)
            {
                state.Gold += target.Reward;
                Resolve(target);
                Message?.Invoke($"敵船を撃破：{target.Reward}G");
            }
            else Message?.Invoke("命中！");
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
            player.TowTo(new Vector2(3.1f, 3.2f));
            Message?.Invoke($"救助船が最寄りの港へ曳航しました（-{lost}G）");
            StateChanged?.Invoke();
            RefreshChunks(true);
        }
    }
}
