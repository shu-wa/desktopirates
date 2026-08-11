using System.Collections.Generic;
using UnityEngine;

namespace Desktopirates
{
    public static class ProceduralSceneFactory
    {
        private static readonly Color HullDark = new Color(0.13f, 0.045f, 0.018f);
        private static readonly Color HullWood = new Color(0.38f, 0.15f, 0.035f);
        private static readonly Color DeckWood = new Color(0.55f, 0.28f, 0.075f);
        private static readonly Color SailCanvas = new Color(0.79f, 0.68f, 0.47f);
        private static readonly Color Brass = new Color(0.92f, 0.52f, 0.08f);
        private static readonly Color Rope = new Color(0.49f, 0.28f, 0.09f);
        private static readonly Color Foam = new Color(0.68f, 0.94f, 1f);
        private static readonly Dictionary<int, Material> MaterialCache = new Dictionary<int, Material>();
        private static readonly Dictionary<int, Material> SailMaterialCache = new Dictionary<int, Material>();

        public static Transform CreatePlayerBoat(Transform parent)
        {
            Transform root = CreateShip(parent, false, 1.28f);
            root.name = "Player Ship — Golden Wake";
            CreateWake(root);
            return root;
        }

        public static Transform CreatePoiVisual(PoiKind kind, Transform parent)
        {
            if (kind == PoiKind.Enemy)
            {
                Transform enemy = CreateShip(parent, true, 0.70f);
                enemy.name = "Enemy Corsair";
                return enemy;
            }

            var root = new GameObject($"{kind} POI").transform;
            root.SetParent(parent, false);
            if (kind == PoiKind.Wreck) CreateWreck(root);
            else if (kind == PoiKind.Treasure) CreateTreasure(root);
            else CreateHarbor(root);
            return root;
        }

        public static Material CreateMaterial(Color color, bool emissive = false)
        {
            Color32 c = color;
            int key = c.r | c.g << 8 | c.b << 16 | c.a << 24;
            if (emissive) key ^= unchecked((int)0x5A000000);
            if (MaterialCache.TryGetValue(key, out Material cached)) return cached;

            Material template = Resources.Load<Material>("StandardMaterial");
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse");
            Material material = template != null ? new Material(template) : new Material(shader);
            material.name = $"Palette {ColorUtility.ToHtmlStringRGB(color)}";
            material.color = color;
            material.hideFlags = HideFlags.DontSave;
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", emissive ? 0.35f : 0.05f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", emissive ? 0.35f : 0.05f);
            if (emissive && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.7f);
            }
            MaterialCache[key] = material;
            return material;
        }

        private static Transform CreateShip(Transform parent, bool enemy, float scale)
        {
            var root = new GameObject(enemy ? "Corsair Ship" : "Adventurer Ship").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, 0.30f, 0f);
            root.localScale = Vector3.one * scale;

            Color hull = enemy ? new Color(0.34f, 0.035f, 0.025f) : HullWood;
            Color trim = enemy ? new Color(0.08f, 0.012f, 0.018f) : Brass;
            Color sail = enemy ? new Color(0.055f, 0.045f, 0.065f) : SailCanvas;
            CreateHull(root, hull, HullDark);
            CreatePart(root, PrimitiveType.Cube, "Raised Deck", new Vector3(0f, 0.38f, -0.07f), new Vector3(0.66f, 0.10f, 1.34f), DeckWood);
            CreatePart(root, PrimitiveType.Cube, "Stern Cabin", new Vector3(0f, 0.61f, -0.57f), new Vector3(0.58f, 0.36f, 0.43f), enemy ? HullDark : HullWood);
            CreatePart(root, PrimitiveType.Cube, "Cabin Roof", new Vector3(0f, 0.83f, -0.57f), new Vector3(0.67f, 0.08f, 0.50f), trim);
            CreatePart(root, PrimitiveType.Cube, "Port Rail", new Vector3(-0.39f, 0.58f, -0.03f), new Vector3(0.045f, 0.13f, 1.22f), Rope);
            CreatePart(root, PrimitiveType.Cube, "Starboard Rail", new Vector3(0.39f, 0.58f, -0.03f), new Vector3(0.045f, 0.13f, 1.22f), Rope);
            CreatePart(root, PrimitiveType.Cube, "Stern Rail", new Vector3(0f, 0.59f, -0.78f), new Vector3(0.74f, 0.12f, 0.04f), Rope);

            CreatePart(root, PrimitiveType.Cylinder, "Main Mast", new Vector3(0f, 1.20f, 0.12f), new Vector3(0.055f, 0.80f, 0.055f), Rope);
            CreatePart(root, PrimitiveType.Cylinder, "Fore Mast", new Vector3(0f, 0.93f, 0.62f), new Vector3(0.04f, 0.48f, 0.04f), Rope);
            CreateBeamBetween(root, "Main Yard", new Vector3(-0.57f, 1.47f, 0.12f), new Vector3(0.57f, 1.47f, 0.12f), 0.028f, Rope);
            CreateBeamBetween(root, "Lower Yard", new Vector3(-0.50f, 0.88f, 0.12f), new Vector3(0.50f, 0.88f, 0.12f), 0.025f, Rope);
            CreateSail(root, "Main Sail", new Vector3(0f, 1.24f, 0.105f), 0.88f, 1.04f, sail, false);
            CreateSail(root, "Fore Sail", new Vector3(0f, 1.02f, 0.59f), 0.48f, 0.62f, sail * 0.92f, true);
            CreatePart(root, PrimitiveType.Cylinder, "Sail Sun Emblem", new Vector3(0.10f, 1.24f, 0.065f), new Vector3(0.14f, 0.018f, 0.14f), enemy ? new Color(0.58f, 0.03f, 0.025f) : Brass, Quaternion.Euler(90f, 0f, 0f), true);
            CreateFlag(root, enemy ? new Color(0.72f, 0.04f, 0.04f) : new Color(0.94f, 0.57f, 0.10f));

            CreateBeamBetween(root, "Port Rigging", new Vector3(0f, 1.84f, 0.12f), new Vector3(-0.38f, 0.55f, -0.64f), 0.008f, Rope);
            CreateBeamBetween(root, "Starboard Rigging", new Vector3(0f, 1.84f, 0.12f), new Vector3(0.38f, 0.55f, -0.64f), 0.008f, Rope);
            CreateBeamBetween(root, "Bow Rigging", new Vector3(0f, 1.84f, 0.12f), new Vector3(0f, 0.46f, 0.98f), 0.008f, Rope);

            for (int side = -1; side <= 1; side += 2)
            {
                CreatePart(root, PrimitiveType.Cylinder, side < 0 ? "Port Cannon" : "Starboard Cannon", new Vector3(side * 0.43f, 0.48f, -0.05f), new Vector3(0.065f, 0.18f, 0.065f), new Color(0.08f, 0.075f, 0.065f), Quaternion.Euler(0f, 0f, 90f));
                Transform lantern = CreatePart(root, PrimitiveType.Sphere, side < 0 ? "Port Lantern" : "Starboard Lantern", new Vector3(side * 0.34f, 0.72f, -0.70f), Vector3.one * 0.085f, enemy ? new Color(1f, 0.12f, 0.03f) : new Color(1f, 0.50f, 0.06f), null, true);
                AddLanternLight(lantern, enemy ? new Color(1f, 0.08f, 0.02f) : new Color(1f, 0.42f, 0.06f));
            }
            return root;
        }

        private static void CreateHull(Transform root, Color hull, Color keel)
        {
            var vertices = new[]
            {
                new Vector3(0f, 0.34f, 1.03f), new Vector3(-0.43f, 0.31f, 0.48f), new Vector3(0.43f, 0.31f, 0.48f),
                new Vector3(-0.37f, 0.29f, -0.78f), new Vector3(0.37f, 0.29f, -0.78f),
                new Vector3(0f, -0.03f, 0.88f), new Vector3(-0.22f, -0.12f, 0.31f), new Vector3(0.22f, -0.12f, 0.31f),
                new Vector3(-0.24f, -0.07f, -0.71f), new Vector3(0.24f, -0.07f, -0.71f)
            };
            int[] triangles =
            {
                0,1,2, 1,3,4, 1,4,2,
                0,5,1, 1,5,6, 1,6,8, 1,8,3,
                2,7,5, 0,2,5, 2,4,9, 2,9,7,
                3,8,9, 3,9,4, 5,7,6, 6,7,9, 6,9,8
            };
            CreateMeshPart(root, "Faceted Hull", vertices, triangles, hull);
            CreatePart(root, PrimitiveType.Cube, "Keel", new Vector3(0f, -0.09f, -0.02f), new Vector3(0.10f, 0.12f, 1.55f), keel);
            CreatePart(root, PrimitiveType.Cube, "Golden Gunwale L", new Vector3(-0.41f, 0.36f, -0.08f), new Vector3(0.04f, 0.05f, 1.24f), Brass);
            CreatePart(root, PrimitiveType.Cube, "Golden Gunwale R", new Vector3(0.41f, 0.36f, -0.08f), new Vector3(0.04f, 0.05f, 1.24f), Brass);
        }

        private static void CreateWake(Transform root)
        {
            for (int i = 0; i < 4; i++)
            {
                float z = -1.05f - i * 0.34f;
                float width = 0.10f - i * 0.012f;
                float spread = 0.25f + i * 0.105f;
                Color color = Color.Lerp(Foam, new Color(0.22f, 0.58f, 0.64f), i / 4f);
                CreatePart(root, PrimitiveType.Cube, $"Wake Port {i}", new Vector3(-spread, -0.24f, z), new Vector3(width, 0.018f, 0.47f), color, Quaternion.Euler(0f, -10f - i * 2f, 0f), true);
                CreatePart(root, PrimitiveType.Cube, $"Wake Starboard {i}", new Vector3(spread, -0.24f, z), new Vector3(width, 0.018f, 0.47f), color, Quaternion.Euler(0f, 10f + i * 2f, 0f), true);
            }
        }

        private static void CreateHarbor(Transform root)
        {
            CreatePart(root, PrimitiveType.Cylinder, "Rocky Harbor Island", new Vector3(-0.38f, -0.06f, 0.20f), new Vector3(1.15f, 0.12f, 0.82f), new Color(0.17f, 0.21f, 0.19f));
            CreatePart(root, PrimitiveType.Cylinder, "Moss Shelf", new Vector3(-0.36f, 0.02f, 0.18f), new Vector3(0.98f, 0.08f, 0.70f), new Color(0.20f, 0.29f, 0.19f));
            CreatePart(root, PrimitiveType.Cube, "Stone Quay", new Vector3(0.30f, 0.13f, -0.12f), new Vector3(1.55f, 0.18f, 0.52f), new Color(0.31f, 0.34f, 0.31f));
            for (int i = 0; i < 6; i++)
                CreatePart(root, PrimitiveType.Cube, $"Pier Plank {i}", new Vector3(0.66f, 0.19f, -0.44f - i * 0.18f), new Vector3(0.54f, 0.07f, 0.14f), i % 2 == 0 ? DeckWood : HullWood);
            CreatePart(root, PrimitiveType.Cube, "Warehouse", new Vector3(-0.35f, 0.43f, -0.02f), new Vector3(0.72f, 0.62f, 0.52f), HullWood);
            CreatePart(root, PrimitiveType.Cube, "Warehouse Roof", new Vector3(-0.35f, 0.79f, -0.02f), new Vector3(0.82f, 0.12f, 0.65f), new Color(0.15f, 0.08f, 0.045f), Quaternion.Euler(0f, 0f, 7f));
            CreatePart(root, PrimitiveType.Cylinder, "Lighthouse Tower", new Vector3(-0.90f, 0.63f, 0.30f), new Vector3(0.22f, 0.62f, 0.22f), new Color(0.68f, 0.62f, 0.47f));
            CreatePart(root, PrimitiveType.Cylinder, "Lighthouse Stripe", new Vector3(-0.90f, 0.62f, 0.30f), new Vector3(0.235f, 0.10f, 0.235f), new Color(0.38f, 0.10f, 0.055f));
            CreatePart(root, PrimitiveType.Cylinder, "Lantern Room", new Vector3(-0.90f, 1.26f, 0.30f), new Vector3(0.27f, 0.14f, 0.27f), new Color(0.12f, 0.12f, 0.10f));
            Transform beacon = CreatePart(root, PrimitiveType.Sphere, "Harbor Beacon", new Vector3(-0.90f, 1.31f, 0.30f), Vector3.one * 0.19f, new Color(1f, 0.62f, 0.08f), null, true);
            AddLanternLight(beacon, new Color(1f, 0.42f, 0.06f));
            CreateBeamBetween(root, "Dock Crane Post", new Vector3(0.18f, 0.22f, 0.18f), new Vector3(0.18f, 0.92f, 0.18f), 0.04f, Rope);
            CreateBeamBetween(root, "Dock Crane Arm", new Vector3(0.18f, 0.88f, 0.18f), new Vector3(0.75f, 0.75f, 0.05f), 0.035f, Rope);
        }

        private static void CreateWreck(Transform root)
        {
            Transform halfA = CreatePart(root, PrimitiveType.Cube, "Broken Hull Port", new Vector3(-0.18f, 0.08f, 0f), new Vector3(0.28f, 0.17f, 1.16f), HullWood, Quaternion.Euler(4f, 34f, 17f));
            Transform halfB = CreatePart(root, PrimitiveType.Cube, "Broken Hull Starboard", new Vector3(0.25f, 0.04f, 0.14f), new Vector3(0.24f, 0.14f, 0.92f), HullDark, Quaternion.Euler(-6f, -38f, -12f));
            CreateBeamBetween(root, "Snapped Mast", new Vector3(-0.40f, 0.10f, -0.35f), new Vector3(0.62f, 0.32f, 0.42f), 0.045f, Rope);
            CreateSail(root, "Torn Sail", new Vector3(0.20f, 0.32f, 0.12f), 0.32f, 0.28f, new Color(0.31f, 0.27f, 0.20f), true);
            CreatePart(root, PrimitiveType.Cylinder, "Floating Barrel", new Vector3(-0.62f, 0.05f, 0.38f), new Vector3(0.13f, 0.18f, 0.13f), DeckWood, Quaternion.Euler(75f, 0f, 16f));
            halfA.name += " — algae worn";
            halfB.name += " — waterlogged";
        }

        private static void CreateTreasure(Transform root)
        {
            CreatePart(root, PrimitiveType.Cylinder, "Treasure Shoal", new Vector3(0f, -0.03f, 0f), new Vector3(0.62f, 0.08f, 0.48f), new Color(0.17f, 0.31f, 0.28f));
            CreatePart(root, PrimitiveType.Cube, "Treasure Chest", new Vector3(0f, 0.24f, 0f), new Vector3(0.66f, 0.38f, 0.48f), HullWood);
            CreatePart(root, PrimitiveType.Cylinder, "Rounded Lid", new Vector3(0f, 0.46f, 0f), new Vector3(0.32f, 0.22f, 0.23f), DeckWood, Quaternion.Euler(0f, 0f, 90f));
            CreatePart(root, PrimitiveType.Cube, "Gold Lock", new Vector3(0f, 0.27f, -0.25f), new Vector3(0.16f, 0.22f, 0.05f), Brass, null, true);
            CreatePart(root, PrimitiveType.Cube, "Gold Band L", new Vector3(-0.23f, 0.31f, 0f), new Vector3(0.07f, 0.40f, 0.50f), Brass);
            CreatePart(root, PrimitiveType.Cube, "Gold Band R", new Vector3(0.23f, 0.31f, 0f), new Vector3(0.07f, 0.40f, 0.50f), Brass);
        }

        private static void CreateSail(Transform root, string name, Vector3 center, float width, float height, Color color, bool triangle)
        {
            var objectRoot = new GameObject(name);
            objectRoot.transform.SetParent(root, false);
            objectRoot.transform.localPosition = center;
            var filter = objectRoot.AddComponent<MeshFilter>();
            var renderer = objectRoot.AddComponent<MeshRenderer>();
            Vector3[] face = triangle
                ? new[] { new Vector3(0f, height * 0.5f, 0f), new Vector3(0f, -height * 0.5f, 0f), new Vector3(width, -height * 0.35f, 0f) }
                : new[] { new Vector3(-width * 0.48f, height * 0.5f, 0f), new Vector3(-width * 0.42f, -height * 0.5f, 0f), new Vector3(width * 0.48f, -height * 0.42f, 0f), new Vector3(width * 0.42f, height * 0.36f, 0f) };
            int count = face.Length;
            var vertices = new Vector3[count * 2];
            for (int i = 0; i < count; i++) { vertices[i] = face[i] + Vector3.back * 0.025f; vertices[i + count] = face[i] + Vector3.forward * 0.025f; }
            var tris = new List<int>();
            if (triangle)
            {
                tris.AddRange(new[] { 0, 1, 2, 3, 5, 4 });
            }
            else
            {
                tris.AddRange(new[] { 0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6 });
            }
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                tris.Add(i); tris.Add(next); tris.Add(i + count);
                tris.Add(next); tris.Add(next + count); tris.Add(i + count);
            }
            var mesh = new Mesh { name = name + " Extruded Mesh", vertices = vertices, triangles = tris.ToArray() };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); filter.sharedMesh = mesh; renderer.sharedMaterial = CreateMaterial(color, true);
        }

        private static void CreateFlag(Transform root, Color color)
        {
            var flag = new GameObject("Masthead Pennant");
            flag.transform.SetParent(root, false); flag.transform.localPosition = new Vector3(0.02f, 1.88f, 0.12f);
            var filter = flag.AddComponent<MeshFilter>(); var renderer = flag.AddComponent<MeshRenderer>();
            var mesh = new Mesh
            {
                vertices = new[] { Vector3.zero, new Vector3(0f, -0.22f, 0f), new Vector3(0.48f, -0.12f, 0f) },
                triangles = new[] { 0, 1, 2, 0, 2, 1 }
            };
            mesh.RecalculateNormals(); filter.sharedMesh = mesh; renderer.sharedMaterial = CreateSailMaterial(color);
        }

        private static Material CreateSailMaterial(Color color)
        {
            Color32 c = color; int key = c.r | c.g << 8 | c.b << 16 | c.a << 24;
            if (SailMaterialCache.TryGetValue(key, out Material cached)) return cached;
            Shader shader = Shader.Find("Desktopirates/Sail") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = $"Canvas {ColorUtility.ToHtmlStringRGB(color)}", color = color, hideFlags = HideFlags.DontSave };
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            SailMaterialCache[key] = material; return material;
        }

        private static Transform CreateMeshPart(Transform parent, string name, Vector3[] vertices, int[] triangles, Color color)
        {
            var part = new GameObject(name).transform; part.SetParent(parent, false);
            var filter = part.gameObject.AddComponent<MeshFilter>(); var renderer = part.gameObject.AddComponent<MeshRenderer>();
            var mesh = new Mesh { name = name + " Mesh", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); filter.sharedMesh = mesh; renderer.sharedMaterial = CreateMaterial(color);
            return part;
        }

        private static Transform CreatePart(Transform parent, PrimitiveType primitive, string name, Vector3 position, Vector3 scale, Color color, Quaternion? rotation = null, bool emissive = false)
        {
            GameObject part = GameObject.CreatePrimitive(primitive); part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localRotation = rotation ?? Quaternion.identity; part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = CreateMaterial(color, emissive);
            Collider collider = part.GetComponent<Collider>(); if (collider != null) Object.Destroy(collider);
            return part.transform;
        }

        private static Transform CreateBeamBetween(Transform parent, string name, Vector3 start, Vector3 end, float radius, Color color)
        {
            Vector3 direction = end - start;
            Transform beam = CreatePart(parent, PrimitiveType.Cylinder, name, (start + end) * 0.5f, new Vector3(radius, direction.magnitude * 0.5f, radius), color);
            beam.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            return beam;
        }

        private static void AddLanternLight(Transform lantern, Color color)
        {
            Light light = lantern.gameObject.AddComponent<Light>(); light.type = LightType.Point; light.color = color;
            light.range = 2.25f; light.intensity = 1.35f; light.shadows = LightShadows.None;
        }
    }
}
