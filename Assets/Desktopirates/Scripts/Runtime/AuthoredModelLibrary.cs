using System.Collections.Generic;
using UnityEngine;

namespace Desktopirates
{
    /// <summary>Loads Blender-authored FBX models and applies project-owned pixel textures by part role.</summary>
    public static class AuthoredModelLibrary
    {
        private const string HullTexture = "Textures/Models/v02/HullWood_Pixel_v02";
        private const string SailTexture = "Textures/Models/v02/SailCanvas_Pixel_v02";
        private const string EnemySailTexture = "Textures/Models/v02/EnemySail_Pixel_v02";
        private const string HarborStoneTexture = "Textures/Models/v02/HarborStone_Pixel_v02";
        private const string HarborWoodTexture = "Textures/Models/v02/HarborWood_Pixel_v02";

        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static bool TryCreatePlayer(Transform parent, int level, out Transform model)
        {
            int tier = Mathf.Clamp(level, 0, ShipProgressionModel.TierCount - 1);
            model = Instantiate($"Models/Ships/player_tier_{tier}_v02", parent, $"Authored Player Tier {tier + 1}");
            if (model == null) return false;
            model.localScale = Vector3.one * WorldPresentationMetrics.PlayerModelScale;
            ApplyShipMaterials(model, false);
            return true;
        }

        public static bool TryCreateEnemy(Transform parent, float archetypeScale, out Transform model)
        {
            model = Instantiate("Models/Ships/enemy_corsair_v02", parent, "Authored Enemy Corsair");
            if (model == null) return false;
            model.localScale = Vector3.one * archetypeScale * WorldPresentationMetrics.EnemyModelScale;
            ApplyShipMaterials(model, true);
            return true;
        }

        public static bool TryCreatePoi(PoiKind kind, Transform parent, out Transform model)
        {
            string path;
            float scale;
            switch (kind)
            {
                case PoiKind.Port: path = "Models/World/harbor_v02"; scale = WorldPresentationMetrics.HarborModelScale; break;
                case PoiKind.Wreck: path = "Models/World/wreck_v02"; scale = WorldPresentationMetrics.WreckModelScale; break;
                case PoiKind.Treasure: path = "Models/World/treasure_v02"; scale = WorldPresentationMetrics.TreasureModelScale; break;
                default: model = null; return false;
            }

            model = Instantiate(path, parent, $"Authored {kind} Model");
            if (model == null) return false;
            model.localScale = Vector3.one * scale;
            ApplyWorldMaterials(model);
            AddHarborBeacon(model, kind);
            return true;
        }

        public static bool TryCreateBoss(Transform parent, BossKind boss, out Transform model)
        {
            string path;
            float scale;
            switch (boss)
            {
                case BossKind.GangAdmiral: path = "Models/Bosses/gang_admiral_v02"; scale = WorldPresentationMetrics.GangAdmiralModelScale; break;
                case BossKind.GhostShip: path = "Models/Bosses/ghost_ship_v02"; scale = WorldPresentationMetrics.GhostShipModelScale; break;
                case BossKind.Kraken: path = "Models/Bosses/kraken_v02"; scale = WorldPresentationMetrics.KrakenModelScale; break;
                case BossKind.Poseidon: path = "Models/Bosses/poseidon_v02"; scale = WorldPresentationMetrics.PoseidonModelScale; break;
                default: model = null; return false;
            }
            model = Instantiate(path, parent, $"Authored Boss {boss}");
            if (model == null) return false;
            model.localScale = Vector3.one * scale;
            if (boss == BossKind.GangAdmiral || boss == BossKind.GhostShip) ApplyShipMaterials(model, true);
            else ApplyBossMaterials(model, boss);
            return true;
        }

        private static Transform Instantiate(string resourcePath, Transform parent, string name)
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null) return null;
            GameObject instance = Object.Instantiate(prefab, parent, false);
            instance.name = name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            return instance.transform;
        }

        private static void ApplyShipMaterials(Transform root, bool enemy)
        {
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                string part = renderer.gameObject.name.ToLowerInvariant();
                if (part.Contains("sail"))
                    renderer.sharedMaterial = GetTextured(enemy ? EnemySailTexture : SailTexture, Color.white);
                else if (part.Contains("brass") || part.Contains("gunwale") || part.Contains("roof") || part.Contains("quarterdeck"))
                    renderer.sharedMaterial = GetFlat("Brass", new Color(0.92f, 0.52f, 0.08f));
                else if (part.Contains("cannon") || part.Contains("metal"))
                    renderer.sharedMaterial = GetFlat("Iron", new Color(0.075f, 0.08f, 0.075f));
                else if (part.Contains("mast") || part.Contains("yard") || part.Contains("crossbeam"))
                    renderer.sharedMaterial = GetTextured(HarborWoodTexture, Color.white);
                else
                    renderer.sharedMaterial = GetTextured(HullTexture, enemy ? new Color(0.72f, 0.42f, 0.38f) : Color.white);
            }
        }

        private static void ApplyWorldMaterials(Transform root)
        {
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                string part = renderer.gameObject.name.ToLowerInvariant();
                if (part.Contains("stone") || part.Contains("shoal") || part.Contains("lighthouse"))
                    renderer.sharedMaterial = GetTextured(HarborStoneTexture, Color.white);
                else if (part.Contains("brass") || part.Contains("gold"))
                    renderer.sharedMaterial = GetFlat("Brass", new Color(0.92f, 0.52f, 0.08f));
                else if (part.Contains("metal") || part.Contains("lantern_room"))
                    renderer.sharedMaterial = GetFlat("Iron", new Color(0.075f, 0.08f, 0.075f));
                else if (part.Contains("beacon"))
                    renderer.sharedMaterial = GetFlat("Beacon", new Color(1f, 0.45f, 0.04f), true);
                else if (part.Contains("harbor") || part.Contains("pier") || part.Contains("warehouse") || part.Contains("crane"))
                    renderer.sharedMaterial = GetTextured(HarborWoodTexture, Color.white);
                else
                    renderer.sharedMaterial = GetTextured(HullTexture, Color.white);
            }
        }

        private static void ApplyBossMaterials(Transform root, BossKind boss)
        {
            string texturePath = boss == BossKind.Kraken ? "Textures/Bosses/boss_kraken_v01" : "Textures/Bosses/boss_poseidon_v01";
            Color tint = boss == BossKind.Kraken ? new Color(0.58f, 0.48f, 0.72f) : new Color(0.68f, 0.86f, 0.82f);
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                string part = renderer.gameObject.name.ToLowerInvariant();
                if (part.Contains("glow") || part.Contains("eye") || part.Contains("beard"))
                    renderer.sharedMaterial = GetFlat("Boss Glow", boss == BossKind.Kraken ? new Color(0.16f, 0.95f, 0.84f) : new Color(0.18f, 0.78f, 0.96f), true);
                else if (part.Contains("brass") || part.Contains("trident"))
                    renderer.sharedMaterial = GetFlat("Brass", new Color(0.92f, 0.52f, 0.08f));
                else
                    renderer.sharedMaterial = GetTextured(texturePath, tint);
            }
        }

        private static void AddHarborBeacon(Transform root, PoiKind kind)
        {
            if (kind != PoiKind.Port) return;
            Transform beacon = FindChildContaining(root, "beacon");
            if (beacon == null) return;
            Light light = beacon.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.42f, 0.06f);
            light.range = 3.6f;
            light.intensity = 1.45f;
            light.shadows = LightShadows.None;
        }

        private static Transform FindChildContaining(Transform root, string value)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name.ToLowerInvariant().Contains(value)) return child;
            return null;
        }

        private static Material GetTextured(string resourcePath, Color tint)
        {
            string key = resourcePath + ":" + ColorUtility.ToHtmlStringRGBA(tint);
            if (Materials.TryGetValue(key, out Material cached)) return cached;
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Repeat;
            }
            Material material = CreateMaterial(key, tint, false);
            material.mainTexture = texture;
            Materials[key] = material;
            return material;
        }

        private static Material GetFlat(string name, Color color, bool emissive = false)
        {
            string key = name + ":" + ColorUtility.ToHtmlStringRGBA(color) + ":" + emissive;
            if (Materials.TryGetValue(key, out Material cached)) return cached;
            Material material = CreateMaterial(name, color, emissive);
            Materials[key] = material;
            return material;
        }

        private static Material CreateMaterial(string name, Color color, bool emissive)
        {
            Material template = Resources.Load<Material>("StandardMaterial");
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse");
            Material material = template != null ? new Material(template) : new Material(shader);
            material.name = "Authored " + name;
            material.color = color;
            material.hideFlags = HideFlags.DontSave;
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.025f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.025f);
            if (emissive && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.55f);
            }
            return material;
        }
    }
}
