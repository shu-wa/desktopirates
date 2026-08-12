using System.Collections.Generic;
using UnityEngine;

namespace Desktopirates
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class OceanDisc : MonoBehaviour
    {
        public const float Radius = 6.25f;

        private Mesh mesh;
        private Vector3[] baseVertices;
        private Vector3[] animatedVertices;
        private Material material;
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;
        private BoatController boat;
        private int topVertexCount;

        public Vector2 VoyageTextureOffset { get; private set; }

        public Color Tint
        {
            get => material != null ? material.GetColor("_Tint") : Color.white;
            set { if (material != null) material.SetColor("_Tint", value); }
        }

        public void Initialize()
        {
            mesh = BuildMesh(12, 64);
            mesh.name = "Procedural Ocean Disc";
            GetComponent<MeshFilter>().sharedMesh = mesh;

            Material template = Resources.Load<Material>("OceanMaterial");
            Shader shader = Shader.Find("Desktopirates/Ocean") ?? Shader.Find("Standard");
            material = template != null ? new Material(template) : new Material(shader);
            material.name = "Ocean Material";
            material.SetColor("_Tint", new Color(0.05f, 0.36f, 0.42f));
            material.SetColor("_DeepColor", new Color(0.018f, 0.12f, 0.16f));
            material.SetColor("_FoamColor", new Color(0.67f, 0.88f, 0.82f));
            Texture2D surface = Resources.Load<Texture2D>("Textures/Environment/OceanSurface_Faceted_v01");
            if (surface != null)
            {
                surface.filterMode = FilterMode.Point;
                surface.wrapMode = TextureWrapMode.Repeat;
                material.SetTexture("_MainTex", surface);
            }
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            propertyBlock = new MaterialPropertyBlock();
        }

        public void Bind(BoatController player) => boat = player;

        private void Update()
        {
            if (mesh == null || baseVertices == null) return;
            float time = Time.time;
            for (int i = 0; i < topVertexCount; i++)
            {
                Vector3 vertex = baseVertices[i];
                float radial = Mathf.Clamp01(new Vector2(vertex.x, vertex.z).magnitude / Radius);
                float wave = Mathf.Sin(vertex.x * 2.1f + time * 1.35f) * 0.055f;
                wave += Mathf.Sin(vertex.z * 2.8f - time * 1.05f) * 0.035f;
                wave *= Mathf.SmoothStep(1f, 0.35f, radial);
                vertex.y += wave;
                animatedVertices[i] = vertex;
            }

            mesh.vertices = animatedVertices;
            if (Time.frameCount % 4 == 0) mesh.RecalculateNormals();

            if (boat != null && meshRenderer != null)
            {
                Vector2 position = boat.LogicalPosition;
                VoyageTextureOffset = new Vector2(
                    Mathf.Repeat(-position.x * 0.075f, 256f),
                    Mathf.Repeat(-position.y * 0.075f, 256f));
                float radians = boat.HeadingDegrees * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
                float maxSpeed = CruiseModel.GetMaxSpeed(boat.State != null ? boat.State.EngineLevel : 0);
                float speed01 = Mathf.Clamp01(boat.Speed / Mathf.Max(0.01f, maxSpeed));
                propertyBlock.SetVector("_VoyageOffset", new Vector4(VoyageTextureOffset.x, VoyageTextureOffset.y, 0f, 0f));
                propertyBlock.SetVector("_VoyageDirection", new Vector4(direction.x, direction.y, 0f, 0f));
                propertyBlock.SetFloat("_VoyageSpeed", speed01);
                meshRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private Mesh BuildMesh(int rings, int segments)
        {
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            vertices.Add(Vector3.zero);
            colors.Add(new Color(0.95f, 1f, 1f));
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int ring = 1; ring <= rings; ring++)
            {
                float radius = Radius * ring / rings;
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / segments;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
                    float noise = Mathf.PerlinNoise(segment * 0.31f, ring * 0.47f);
                    colors.Add(Color.Lerp(new Color(0.62f, 0.78f, 0.82f), Color.white, noise * 0.55f));
                    uvs.Add(new Vector2(0.5f + Mathf.Cos(angle) * radius / (Radius * 2f), 0.5f + Mathf.Sin(angle) * radius / (Radius * 2f)));
                }
            }

            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                triangles.Add(0);
                triangles.Add(1 + next);
                triangles.Add(1 + segment);
            }

            for (int ring = 1; ring < rings; ring++)
            {
                int innerStart = 1 + (ring - 1) * segments;
                int outerStart = 1 + ring * segments;
                for (int segment = 0; segment < segments; segment++)
                {
                    int next = (segment + 1) % segments;
                    int a = innerStart + segment;
                    int b = innerStart + next;
                    int c = outerStart + segment;
                    int d = outerStart + next;
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    triangles.Add(b); triangles.Add(d); triangles.Add(c);
                }
            }

            topVertexCount = vertices.Count;
            int outerRingStart = 1 + (rings - 1) * segments;
            int skirtStart = vertices.Count;
            for (int segment = 0; segment < segments; segment++)
            {
                Vector3 top = vertices[outerRingStart + segment];
                vertices.Add(new Vector3(top.x, -0.24f, top.z));
                colors.Add(new Color(0.34f, 0.50f, 0.53f));
                uvs.Add(uvs[outerRingStart + segment]);
            }

            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                int topA = outerRingStart + segment;
                int topB = outerRingStart + next;
                int bottomA = skirtStart + segment;
                int bottomB = skirtStart + next;
                triangles.Add(topA); triangles.Add(topB); triangles.Add(bottomA);
                triangles.Add(topB); triangles.Add(bottomB); triangles.Add(bottomA);
            }

            var result = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            result.SetVertices(vertices);
            result.SetColors(colors);
            result.SetUVs(0, uvs);
            result.SetTriangles(triangles, 0);
            result.RecalculateNormals();
            result.RecalculateBounds();

            baseVertices = result.vertices;
            animatedVertices = result.vertices;
            return result;
        }
    }
}
