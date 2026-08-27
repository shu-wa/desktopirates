using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Desktopirates
{
    public enum ShipStatus : byte
    {
        Burning,
        Poisoned,
        Frozen,
        Sticky,
        None = byte.MaxValue
    }

    /// <summary>Shared status state for player and enemy ships. It contains no scene references and is testable.</summary>
    public sealed class ShipStatusRuntime
    {
        public const int StatusCount = 4;
        public const float BurningHullRatePerSecond = 0.006f;
        private readonly float[] remaining = new float[StatusCount];
        private readonly float[] potency = new float[StatusCount];
        private float burningDamageRemainder;

        public bool HasAny
        {
            get
            {
                for (int i = 0; i < StatusCount; i++) if (remaining[i] > 0f) return true;
                return false;
            }
        }

        public bool IsActive(ShipStatus status)
            => status != ShipStatus.None && remaining[(int)status] > 0f;

        public float GetRemaining(ShipStatus status)
            => status == ShipStatus.None ? 0f : remaining[(int)status];

        public void Apply(ShipStatus status, float duration, float strength = 1f)
        {
            if (status == ShipStatus.None || duration <= 0f) return;
            int index = (int)status;
            remaining[index] = Mathf.Max(remaining[index], duration);
            potency[index] = Mathf.Max(potency[index], Mathf.Max(0.1f, strength));
        }

        public int Tick(float deltaTime, int maxHull)
        {
            if (deltaTime <= 0f) return 0;
            float burningTime = IsActive(ShipStatus.Burning)
                ? Mathf.Min(deltaTime, remaining[(int)ShipStatus.Burning])
                : 0f;
            if (burningTime > 0f)
                burningDamageRemainder += Mathf.Max(1, maxHull) * BurningHullRatePerSecond * potency[(int)ShipStatus.Burning] * burningTime;

            for (int i = 0; i < StatusCount; i++)
            {
                remaining[i] = Mathf.Max(0f, remaining[i] - deltaTime);
                if (remaining[i] <= 0f) potency[i] = 0f;
            }

            int damage = Mathf.FloorToInt(burningDamageRemainder);
            burningDamageRemainder -= damage;
            return damage;
        }

        public ShipStatus CurePriority()
        {
            ShipStatus[] order = { ShipStatus.Burning, ShipStatus.Poisoned, ShipStatus.Frozen, ShipStatus.Sticky };
            foreach (ShipStatus status in order)
            {
                if (!IsActive(status)) continue;
                Clear(status);
                return status;
            }
            return ShipStatus.None;
        }

        public void Clear(ShipStatus status)
        {
            if (status == ShipStatus.None) return;
            remaining[(int)status] = 0f;
            potency[(int)status] = 0f;
            if (status == ShipStatus.Burning) burningDamageRemainder = 0f;
        }

        public void ClearAll()
        {
            Array.Clear(remaining, 0, remaining.Length);
            Array.Clear(potency, 0, potency.Length);
            burningDamageRemainder = 0f;
        }

        public static string GetName(ShipStatus status) => status switch
        {
            ShipStatus.Burning => "BURNING",
            ShipStatus.Poisoned => "POISON",
            ShipStatus.Frozen => "FROZEN",
            ShipStatus.Sticky => "STICKY",
            _ => "CLEAR"
        };
    }

    public sealed class PlayerShipConditionController : MonoBehaviour
    {
        public ShipStatusRuntime Conditions { get; } = new ShipStatusRuntime();
        public event Action<string> Message;
        public event Action Changed;
        public event Action HullDepleted;
        public event Action<int> HullDamaged;

        private GameState state;
        private ShipStatusVisualController visuals;
        private float actionTimer;
        private bool treatingStatuses;
        private bool depletionRaised;

        public void Initialize(BoatController boat, GameState gameState)
        {
            state = gameState;
            visuals = boat.Visual.gameObject.AddComponent<ShipStatusVisualController>();
            visuals.Initialize(Conditions);
            ResetActionTimer(false);
        }

        public void ApplyStatus(ShipStatus status, float duration, float potency = 1f)
        {
            if (status == ShipStatus.None) return;
            Conditions.Apply(status, duration, potency);
            treatingStatuses = true;
            actionTimer = Mathf.Min(actionTimer, GetCureInterval());
            Message?.Invoke($"STATUS: {ShipStatusRuntime.GetName(status)}");
        }

        public void ClearAll()
        {
            Conditions.ClearAll();
            ApplyTransientMultipliers();
            depletionRaised = false;
            ResetActionTimer(false);
        }

        private void Update()
        {
            if (state == null) return;
            int dotDamage = Conditions.Tick(Time.deltaTime, state.MaxHull);
            if (dotDamage > 0)
            {
                state.Hull = Mathf.Max(0, state.Hull - dotDamage);
                HullDamaged?.Invoke(dotDamage);
                Changed?.Invoke();
            }
            ApplyTransientMultipliers();

            if (state.Hull <= 0)
            {
                if (!depletionRaised)
                {
                    depletionRaised = true;
                    HullDepleted?.Invoke();
                }
                return;
            }
            depletionRaised = false;

            int repairers = state.GetRoleCrew(CrewRole.Repairer);
            if (repairers <= 0) return;
            bool nowTreating = Conditions.HasAny;
            if (nowTreating != treatingStatuses)
            {
                treatingStatuses = nowTreating;
                ResetActionTimer(nowTreating);
            }
            if (!nowTreating && state.Hull >= state.MaxHull) return;

            actionTimer -= Time.deltaTime;
            if (actionTimer > 0f) return;
            if (nowTreating)
            {
                ShipStatus cured = Conditions.CurePriority();
                if (cured != ShipStatus.None)
                {
                    Message?.Invoke($"REPAIR CREW CURED {ShipStatusRuntime.GetName(cured)}");
                    Changed?.Invoke();
                }
            }
            else
            {
                int amount = Mathf.Max(1, Mathf.CeilToInt(state.MaxHull * 0.03f * CrewManagementModel.GetRepairAmountMultiplier(state)));
                int restored = Mathf.Min(amount, state.MaxHull - state.Hull);
                state.Hull += restored;
                if (restored > 0)
                {
                    Message?.Invoke($"REPAIR CREW +{restored} HULL");
                    Changed?.Invoke();
                }
            }
            treatingStatuses = Conditions.HasAny;
            ResetActionTimer(treatingStatuses);
        }

        private void ApplyTransientMultipliers()
        {
            state.CrewPerformanceMultiplier = Conditions.IsActive(ShipStatus.Poisoned) ? 0.5f : 1f;
            state.SpeedStatusMultiplier = Conditions.IsActive(ShipStatus.Frozen) ? 0.55f : 1f;
            state.TurnStatusMultiplier = Conditions.IsActive(ShipStatus.Sticky) ? 0.55f : 1f;
        }

        private float GetCrewFactor()
            => Mathf.Sqrt(Mathf.Max(1, state.GetRoleCrew(CrewRole.Repairer)));

        private float GetRepairInterval()
            => 15f / GetCrewFactor() * CrewManagementModel.GetRepairIntervalMultiplier(state);

        private float GetCureInterval()
            => 12f / GetCrewFactor() * CrewManagementModel.GetConditionCureIntervalMultiplier(state);

        private void ResetActionTimer(bool cure)
            => actionTimer = cure ? GetCureInterval() : GetRepairInterval();
    }

    /// <summary>Texture-backed status badges plus restrained blocky hull accents.</summary>
    public sealed class ShipStatusVisualController : MonoBehaviour
    {
        private static readonly string[] TexturePaths =
        {
            "Textures/UI/Status/status_burning_v01",
            "Textures/UI/Status/status_poison_v01",
            "Textures/UI/Status/status_frozen_v01",
            "Textures/UI/Status/status_sticky_v01"
        };

        private static readonly Color[] EffectColors =
        {
            new Color(1f, 0.31f, 0.05f),
            new Color(0.50f, 0.92f, 0.10f),
            new Color(0.40f, 0.90f, 1f),
            new Color(0.58f, 0.24f, 0.04f)
        };

        private static readonly Sprite[] SharedIcons = new Sprite[ShipStatusRuntime.StatusCount];
        private static readonly Material[] SharedMaterials = new Material[ShipStatusRuntime.StatusCount];

        private ShipStatusRuntime conditions;
        private Transform iconRoot;
        private readonly SpriteRenderer[] icons = new SpriteRenderer[ShipStatusRuntime.StatusCount];
        private readonly Transform[] accents = new Transform[ShipStatusRuntime.StatusCount];

        public void Initialize(ShipStatusRuntime runtime)
        {
            conditions = runtime;
            iconRoot = new GameObject("Status Badges").transform;
            iconRoot.SetParent(transform, false);
            iconRoot.localPosition = new Vector3(0f, 2.0f, 0f);

            for (int i = 0; i < ShipStatusRuntime.StatusCount; i++)
            {
                Texture2D texture = Resources.Load<Texture2D>(TexturePaths[i]);
                var iconObject = new GameObject($"{(ShipStatus)i} Badge", typeof(SpriteRenderer));
                iconObject.transform.SetParent(iconRoot, false);
                SpriteRenderer renderer = iconObject.GetComponent<SpriteRenderer>();
                if (texture != null && SharedIcons[i] == null)
                    SharedIcons[i] = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.one * 0.5f, texture.width);
                renderer.sprite = SharedIcons[i];
                renderer.sortingOrder = 40;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                icons[i] = renderer;

                GameObject accent = GameObject.CreatePrimitive(PrimitiveType.Cube);
                accent.name = $"{(ShipStatus)i} Subtle Effect";
                accent.transform.SetParent(transform, false);
                accent.transform.localScale = Vector3.one * 0.075f;
                Collider collider = accent.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                MeshRenderer mesh = accent.GetComponent<MeshRenderer>();
                if (SharedMaterials[i] == null) SharedMaterials[i] = MakeMaterial(EffectColors[i]);
                mesh.sharedMaterial = SharedMaterials[i];
                mesh.shadowCastingMode = ShadowCastingMode.Off;
                mesh.receiveShadows = false;
                accents[i] = accent.transform;
            }
        }

        private void LateUpdate()
        {
            if (conditions == null) return;
            Camera camera = Camera.main;
            if (camera != null) iconRoot.rotation = camera.transform.rotation;
            int activeCount = 0;
            for (int i = 0; i < icons.Length; i++) if (conditions.IsActive((ShipStatus)i)) activeCount++;
            int activeIndex = 0;
            for (int i = 0; i < icons.Length; i++)
            {
                bool active = conditions.IsActive((ShipStatus)i);
                if (icons[i].gameObject.activeSelf != active) icons[i].gameObject.SetActive(active);
                if (accents[i].gameObject.activeSelf != active) accents[i].gameObject.SetActive(active);
                if (!active) continue;
                icons[i].transform.localPosition = new Vector3((activeIndex - (activeCount - 1) * 0.5f) * 0.56f, 0f, 0f);
                icons[i].transform.localScale = Vector3.one * 0.50f;
                AnimateAccent((ShipStatus)i, accents[i], Time.time + i * 0.73f);
                activeIndex++;
            }
        }

        private static void AnimateAccent(ShipStatus status, Transform accent, float time)
        {
            switch (status)
            {
                case ShipStatus.Burning:
                    accent.localPosition = new Vector3(Mathf.Sin(time * 2.8f) * 0.22f, 0.76f + Mathf.PingPong(time * 0.20f, 0.13f), 0.08f);
                    accent.localScale = Vector3.one * (0.055f + Mathf.PingPong(time * 0.025f, 0.035f));
                    break;
                case ShipStatus.Poisoned:
                    accent.localPosition = new Vector3(0.24f, 0.50f + Mathf.Sin(time * 1.8f) * 0.08f, Mathf.Cos(time) * 0.28f);
                    accent.localScale = Vector3.one * 0.065f;
                    break;
                case ShipStatus.Frozen:
                    accent.localPosition = new Vector3(-0.30f, 0.40f, Mathf.Sin(time * 1.2f) * 0.35f);
                    accent.localRotation = Quaternion.Euler(0f, time * 34f, 45f);
                    accent.localScale = new Vector3(0.045f, 0.13f, 0.045f);
                    break;
                case ShipStatus.Sticky:
                    accent.localPosition = new Vector3(Mathf.Sin(time) * 0.34f, 0.24f, -0.18f);
                    accent.localScale = new Vector3(0.12f, 0.035f, 0.10f);
                    break;
            }
        }

        private static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }
    }
}
