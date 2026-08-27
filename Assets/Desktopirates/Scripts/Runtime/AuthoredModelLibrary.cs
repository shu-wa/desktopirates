using System.Collections.Generic;
using UnityEngine;

namespace Desktopirates
{
    /// <summary>Loads Blender-authored FBX models and applies project-owned pixel textures by part role.</summary>
    public static class AuthoredModelLibrary
    {
        private const string HullTexture = "Textures/Models/v03/HullEbony_SaltWorn_Pixel_v03";
        private const string DeckTexture = "Textures/Models/v03/DeckOak_Warm_Pixel_v03";
        private const string SailTexture = "Textures/Models/v03/SailCanvas_Patched_Pixel_v03";
        private const string EnemyHullTexture = "Textures/Models/v04/EnemyHull_BurgundyIron_Pixel_v04";
        private const string EnemySailTexture = "Textures/Models/v04/EnemySail_RaggedPatch_Pixel_v04";
        private const string HarborStoneTexture = "Textures/Models/v02/HarborStone_Pixel_v02";
        private const string HarborWoodTexture = "Textures/Models/v02/HarborWood_Pixel_v02";
        private const string GangAdmiralTexture = "Textures/Bosses/boss_gang_admiral_v01";
        private const string GhostShipTexture = "Textures/Bosses/boss_ghost_ship_v01";
        private const string KrakenTexture = "Textures/Bosses/boss_kraken_v01";
        private const string PoseidonTexture = "Textures/Bosses/boss_poseidon_v01";

        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static bool TryCreatePlayer(Transform parent, int level, out Transform model)
        {
            int tier = Mathf.Clamp(level, 0, ShipProgressionModel.TierCount - 1);
            model = Instantiate($"Models/ShipsV03/player_tier_{tier}_v03", parent, $"Authored Player Tier {tier + 1} v03");
            if (model == null) return false;
            model.localScale = Vector3.one * WorldPresentationMetrics.PlayerModelScale;
            ApplyShipMaterials(model, false);
            return true;
        }

        public static bool TryCreateEnemy(Transform parent, EnemyArchetype archetype, float archetypeScale, out Transform model)
        {
            model = Instantiate(GetEnemyResourcePath(archetype), parent, $"Authored Enemy {archetype} v04");
            if (model == null) return false;
            model.localScale = Vector3.one * archetypeScale * WorldPresentationMetrics.EnemyModelScale;
            ApplyShipMaterials(model, true, archetype);
            return true;
        }

        public static string GetEnemyResourcePath(EnemyArchetype archetype) => archetype switch
        {
            EnemyArchetype.Skirmisher => "Models/EnemyShipsV04/enemy_skirmisher_v04",
            EnemyArchetype.Gunboat => "Models/EnemyShipsV04/enemy_gunboat_v04",
            EnemyArchetype.FireRaider => "Models/EnemyShipsV04/enemy_fire_raider_v04",
            EnemyArchetype.PlagueRaider => "Models/EnemyShipsV04/enemy_plague_raider_v04",
            EnemyArchetype.FrostCutter => "Models/EnemyShipsV04/enemy_frost_cutter_v04",
            EnemyArchetype.Ironclad => "Models/EnemyShipsV04/enemy_ironclad_v04",
            EnemyArchetype.Hunter => "Models/EnemyShipsV04/enemy_hunter_v04",
            _ => "Models/EnemyShipsV04/enemy_corsair_v04"
        };

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
            string path = GetBossResourcePath(boss);
            float scale = GetBossScale(boss);
            if (string.IsNullOrEmpty(path)) { model = null; return false; }
            model = Instantiate(path, parent, $"Authored Boss {boss}");
            if (model == null) return false;
            model.localScale = Vector3.one * scale;
            if (boss == BossKind.GangAdmiral || boss == BossKind.GhostShip) ApplyBossShipMaterials(model, boss);
            else ApplyBossMaterials(model, boss);
            return true;
        }

        /// <summary>Forces one boss model and its large authored texture into native caches.</summary>
        public static bool PrewarmBossAssets(BossKind boss)
        {
            string modelPath = GetBossResourcePath(boss);
            string texturePath = GetBossTexturePath(boss);
            if (string.IsNullOrEmpty(modelPath) || string.IsNullOrEmpty(texturePath)) return false;
            GameObject prefab = Resources.Load<GameObject>(modelPath);
            Texture2D texture = Resources.Load<Texture2D>(texturePath);
            if (prefab == null || texture == null) return false;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.GetNativeTexturePtr();
            foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh != null) filter.sharedMesh.UploadMeshData(false);
            return true;
        }

        public static string GetBossResourcePath(BossKind boss) => boss switch
        {
            BossKind.GangAdmiral => "Models/Bosses/gang_admiral_v02",
            BossKind.GhostShip => "Models/Bosses/ghost_ship_v02",
            BossKind.Kraken => "Models/Bosses/kraken_v02",
            BossKind.Poseidon => "Models/Bosses/poseidon_v02",
            _ => string.Empty
        };

        public static string GetBossTexturePath(BossKind boss) => boss switch
        {
            BossKind.GangAdmiral => GangAdmiralTexture,
            BossKind.GhostShip => GhostShipTexture,
            BossKind.Kraken => KrakenTexture,
            BossKind.Poseidon => PoseidonTexture,
            _ => string.Empty
        };

        private static float GetBossScale(BossKind boss) => boss switch
        {
            BossKind.GangAdmiral => WorldPresentationMetrics.GangAdmiralModelScale,
            BossKind.GhostShip => WorldPresentationMetrics.GhostShipModelScale,
            BossKind.Kraken => WorldPresentationMetrics.KrakenModelScale,
            BossKind.Poseidon => WorldPresentationMetrics.PoseidonModelScale,
            _ => 1f
        };

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

        private static void ApplyShipMaterials(Transform root, bool enemy, EnemyArchetype archetype = EnemyArchetype.Corsair)
        {
            Color enemyAccent = GetEnemyAccent(archetype);
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                string part = renderer.gameObject.name.ToLowerInvariant();
                if (part.Contains("sail"))
                    renderer.sharedMaterial = GetTextured(enemy ? EnemySailTexture : SailTexture, enemy ? GetEnemySailTint(archetype) : Color.white);
                else if (enemy && (part.Contains("flag") || part.Contains("pennant") || part.Contains("aft_fin")))
                    renderer.sharedMaterial = GetTextured(EnemySailTexture, GetEnemySailTint(archetype));
                else if (enemy && (part.Contains("fire") || part.Contains("plague") && part.Contains("glow") || part.Contains("frost") || part.Contains("tracking_lantern")))
                    renderer.sharedMaterial = GetFlat(archetype + " Accent", enemyAccent, true);
                else if (part.Contains("deck") || part.Contains("fightingtop") || part.Contains("crate"))
                    renderer.sharedMaterial = GetTextured(DeckTexture, Color.white);
                else if (part.Contains("brass") || part.Contains("gilded") || part.Contains("figurehead") || part.Contains("rail") || part.Contains("crest"))
                    renderer.sharedMaterial = GetFlat("Brass", new Color(0.92f, 0.52f, 0.08f));
                else if (part.Contains("cannon") || part.Contains("gunport") || part.Contains("anchor") || part.Contains("iron"))
                    renderer.sharedMaterial = GetFlat("Iron", new Color(0.075f, 0.08f, 0.075f));
                else if (part.Contains("lantern") || part.Contains("window"))
                    renderer.sharedMaterial = GetFlat("Warm Glass", new Color(1f, 0.35f, 0.03f), true);
                else if (part.Contains("mast") || part.Contains("yard") || part.Contains("crossbeam") || part.Contains("rigging") || part.Contains("rope") || part.Contains("lashing"))
                    renderer.sharedMaterial = GetTextured(HarborWoodTexture, Color.white);
                else
                    renderer.sharedMaterial = GetTextured(enemy ? EnemyHullTexture : HullTexture, enemy ? GetEnemyHullTint(archetype) : Color.white);
            }
        }

        private static void ApplyBossShipMaterials(Transform root, BossKind boss)
        {
            string texturePath = GetBossTexturePath(boss);
            Color hullTint = boss == BossKind.GhostShip ? new Color(0.42f, 0.66f, 0.62f) : new Color(0.88f, 0.61f, 0.34f);
            Color sailTint = boss == BossKind.GhostShip ? new Color(0.54f, 0.82f, 0.78f) : new Color(0.72f, 0.18f, 0.12f);
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                string part = renderer.gameObject.name.ToLowerInvariant();
                if (part.Contains("sail") || part.Contains("flag") || part.Contains("pennant"))
                    renderer.sharedMaterial = GetTextured(texturePath, sailTint);
                else if (part.Contains("deck") || part.Contains("crate"))
                    renderer.sharedMaterial = GetTextured(DeckTexture, boss == BossKind.GhostShip ? new Color(0.48f, 0.68f, 0.63f) : Color.white);
                else if (part.Contains("brass") || part.Contains("gilded") || part.Contains("crest") || part.Contains("figurehead"))
                    renderer.sharedMaterial = GetFlat("Brass", new Color(0.92f, 0.52f, 0.08f));
                else if (part.Contains("cannon") || part.Contains("gunport") || part.Contains("anchor") || part.Contains("iron"))
                    renderer.sharedMaterial = GetFlat("Iron", new Color(0.075f, 0.08f, 0.075f));
                else if (part.Contains("lantern") || part.Contains("window") || part.Contains("glow"))
                    renderer.sharedMaterial = GetFlat(boss + " Glow", boss == BossKind.GhostShip ? new Color(0.16f, 0.94f, 0.78f) : new Color(1f, 0.32f, 0.04f), true);
                else
                    renderer.sharedMaterial = GetTextured(texturePath, hullTint);
            }
        }

        private static Color GetEnemySailTint(EnemyArchetype archetype) => archetype switch
        {
            EnemyArchetype.Skirmisher => new Color(0.92f, 0.68f, 0.16f),
            EnemyArchetype.Gunboat => new Color(0.43f, 0.49f, 0.52f),
            EnemyArchetype.FireRaider => new Color(0.92f, 0.22f, 0.06f),
            EnemyArchetype.PlagueRaider => new Color(0.42f, 0.72f, 0.15f),
            EnemyArchetype.FrostCutter => new Color(0.42f, 0.88f, 1.00f),
            EnemyArchetype.Ironclad => new Color(0.58f, 0.62f, 0.64f),
            EnemyArchetype.Hunter => new Color(0.62f, 0.20f, 0.82f),
            _ => new Color(0.48f, 0.10f, 0.09f)
        };

        private static Color GetEnemyHullTint(EnemyArchetype archetype) => archetype switch
        {
            EnemyArchetype.Skirmisher => new Color(1.00f, 0.78f, 0.42f),
            EnemyArchetype.Gunboat => new Color(0.72f, 0.76f, 0.78f),
            EnemyArchetype.FireRaider => new Color(1.00f, 0.48f, 0.32f),
            EnemyArchetype.PlagueRaider => new Color(0.64f, 0.82f, 0.42f),
            EnemyArchetype.FrostCutter => new Color(0.70f, 0.90f, 1.00f),
            EnemyArchetype.Ironclad => new Color(0.62f, 0.66f, 0.68f),
            EnemyArchetype.Hunter => new Color(0.76f, 0.46f, 0.88f),
            _ => new Color(0.78f, 0.48f, 0.44f)
        };

        private static Color GetEnemyAccent(EnemyArchetype archetype) => archetype switch
        {
            EnemyArchetype.FireRaider => new Color(1.00f, 0.20f, 0.025f),
            EnemyArchetype.PlagueRaider => new Color(0.36f, 0.95f, 0.08f),
            EnemyArchetype.FrostCutter => new Color(0.28f, 0.88f, 1.00f),
            EnemyArchetype.Hunter => new Color(0.74f, 0.12f, 1.00f),
            _ => new Color(1.00f, 0.40f, 0.04f)
        };

        private static void ApplyWorldMaterials(Transform root)
        {
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                string part = renderer.gameObject.name.ToLowerInvariant();
                if (part.Contains("stone") || part.Contains("shoal") || part.Contains("lighthouse"))
                    renderer.sharedMaterial = GetTextured(HarborStoneTexture, new Color(0.92f, 0.96f, 0.92f));
                else if (part.Contains("brass") || part.Contains("gold"))
                    renderer.sharedMaterial = GetFlat("Brass", new Color(0.92f, 0.52f, 0.08f));
                else if (part.Contains("metal") || part.Contains("lantern_room"))
                    renderer.sharedMaterial = GetFlat("Iron", new Color(0.075f, 0.08f, 0.075f));
                else if (part.Contains("beacon") || part.Contains("window"))
                    renderer.sharedMaterial = GetFlat("Beacon", new Color(1f, 0.45f, 0.04f), true);
                else if (part.Contains("roof"))
                    renderer.sharedMaterial = GetTextured(HullTexture, new Color(1f, 0.62f, 0.42f));
                else if (part.Contains("harbor") || part.Contains("pier") || part.Contains("warehouse") || part.Contains("crane"))
                    renderer.sharedMaterial = GetTextured(HarborWoodTexture, new Color(1f, 0.88f, 0.68f));
                else
                    renderer.sharedMaterial = GetTextured(HullTexture, Color.white);
            }
        }

        private static void ApplyBossMaterials(Transform root, BossKind boss)
        {
            string texturePath = GetBossTexturePath(boss);
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
