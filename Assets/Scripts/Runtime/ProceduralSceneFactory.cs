using UnityEngine;

namespace Desktopirates
{
    public static class ProceduralSceneFactory
    {
        public static Transform CreatePlayerBoat(Transform parent)
        {
            var root = new GameObject("Player Ship").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, 0.28f, 0f);

            CreatePart(root, PrimitiveType.Cube, "Hull", new Vector3(0f, 0.2f, 0f), new Vector3(0.82f, 0.34f, 1.75f), new Color(0.24f, 0.10f, 0.045f));
            CreatePart(root, PrimitiveType.Cube, "Deck", new Vector3(0f, 0.43f, 0.08f), new Vector3(0.68f, 0.12f, 1.30f), new Color(0.47f, 0.25f, 0.09f));
            CreatePart(root, PrimitiveType.Cylinder, "Mast", new Vector3(0f, 1.15f, 0.08f), new Vector3(0.07f, 0.82f, 0.07f), new Color(0.31f, 0.16f, 0.065f));
            CreateSail(root);

            Transform lanternLeft = CreatePart(root, PrimitiveType.Sphere, "Lantern L", new Vector3(-0.35f, 0.63f, -0.48f), Vector3.one * 0.11f, new Color(1f, 0.55f, 0.08f));
            Transform lanternRight = CreatePart(root, PrimitiveType.Sphere, "Lantern R", new Vector3(0.35f, 0.63f, -0.48f), Vector3.one * 0.11f, new Color(1f, 0.55f, 0.08f));
            AddLanternLight(lanternLeft);
            AddLanternLight(lanternRight);

            CreatePart(root, PrimitiveType.Cube, "Wake L", new Vector3(-0.27f, -0.13f, -1.20f), new Vector3(0.08f, 0.025f, 1.05f), new Color(0.55f, 0.86f, 0.92f));
            CreatePart(root, PrimitiveType.Cube, "Wake R", new Vector3(0.27f, -0.13f, -1.20f), new Vector3(0.08f, 0.025f, 1.05f), new Color(0.55f, 0.86f, 0.92f));
            return root;
        }

        public static Transform CreatePoiVisual(PoiKind kind, Transform parent)
        {
            var root = new GameObject($"{kind} POI").transform;
            root.SetParent(parent, false);

            if (kind == PoiKind.Enemy)
            {
                CreatePart(root, PrimitiveType.Cube, "Enemy Hull", new Vector3(0f, 0.24f, 0f), new Vector3(0.52f, 0.26f, 1.05f), new Color(0.38f, 0.055f, 0.035f));
                CreatePart(root, PrimitiveType.Cylinder, "Enemy Mast", new Vector3(0f, 0.84f, 0f), new Vector3(0.04f, 0.53f, 0.04f), new Color(0.22f, 0.10f, 0.04f));
            }
            else if (kind == PoiKind.Wreck)
            {
                CreatePart(root, PrimitiveType.Cube, "Wreck A", new Vector3(-0.12f, 0.08f, 0f), new Vector3(0.18f, 0.12f, 1.15f), new Color(0.33f, 0.19f, 0.07f), Quaternion.Euler(0f, 38f, 12f));
                CreatePart(root, PrimitiveType.Cube, "Wreck B", new Vector3(0.15f, 0.12f, 0.08f), new Vector3(0.16f, 0.10f, 0.95f), new Color(0.40f, 0.24f, 0.08f), Quaternion.Euler(0f, -42f, -9f));
            }
            else
            {
                CreatePart(root, PrimitiveType.Cube, "Chest", new Vector3(0f, 0.18f, 0f), new Vector3(0.65f, 0.36f, 0.46f), new Color(0.58f, 0.29f, 0.04f));
                CreatePart(root, PrimitiveType.Cube, "Gold Band", new Vector3(0f, 0.20f, -0.24f), new Vector3(0.14f, 0.30f, 0.04f), new Color(1f, 0.68f, 0.08f));
            }

            return root;
        }

        public static Material CreateMaterial(Color color)
        {
            Material template = Resources.Load<Material>("StandardMaterial");
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse");
            var material = template != null ? new Material(template) : new Material(shader);
            material.color = color;
            material.hideFlags = HideFlags.DontSave;
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0f);
            return material;
        }

        private static Transform CreatePart(Transform parent, PrimitiveType primitive, string name, Vector3 position, Vector3 scale, Color color, Quaternion? rotation = null)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation ?? Quaternion.identity;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = CreateMaterial(color);
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            return part.transform;
        }

        private static void CreateSail(Transform root)
        {
            var sailObject = new GameObject("Main Sail");
            sailObject.transform.SetParent(root, false);
            sailObject.transform.localPosition = new Vector3(0f, 1.25f, 0.05f);
            var filter = sailObject.AddComponent<MeshFilter>();
            var renderer = sailObject.AddComponent<MeshRenderer>();
            var mesh = new Mesh
            {
                name = "Low Poly Sail",
                vertices = new[]
                {
                    new Vector3(0.02f, 0.58f, 0f),
                    new Vector3(0.02f, -0.55f, 0f),
                    new Vector3(0.72f, -0.38f, 0f),
                    new Vector3(0.54f, 0.28f, 0f)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 },
                uv = new[] { Vector2.up, Vector2.zero, Vector2.right, Vector2.one }
            };
            mesh.RecalculateNormals();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = CreateMaterial(new Color(0.82f, 0.72f, 0.52f));
        }

        private static void AddLanternLight(Transform lantern)
        {
            var light = lantern.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.48f, 0.12f);
            light.range = 2.1f;
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;
        }
    }
}
