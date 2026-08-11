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
        private int topVertexCount;

        public Color Tint
        {
            get => material != null ? material.GetColor("_Tint") : Color.white;
            set { if (material != null) material.SetColor("_Tint", value); }
        }

        public void Initialize()
        {
            mesh = BuildMesh(10, 56);
            mesh.name = "Procedural Ocean Disc";
            GetComponent<MeshFilter>().sharedMesh = mesh;

            Material template = Resources.Load<Material>("OceanMaterial");
            Shader shader = Shader.Find("Desktopirates/Ocean") ?? Shader.Find("Standard");
            material = template != null ? new Material(template) : new Material(shader);
            material.name = "Ocean Material";
            material.SetColor("_Tint", new Color(0.05f, 0.36f, 0.42f));
            GetComponent<MeshRenderer>().sharedMaterial = material;
        }

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
        }

        private Mesh BuildMesh(int rings, int segments)
        {
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();

            vertices.Add(Vector3.zero);
            colors.Add(new Color(0.95f, 1f, 1f));

            for (int ring = 1; ring <= rings; ring++)
            {
                float radius = Radius * ring / rings;
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / segments;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
                    float noise = Mathf.PerlinNoise(segment * 0.31f, ring * 0.47f);
                    colors.Add(Color.Lerp(new Color(0.62f, 0.78f, 0.82f), Color.white, noise * 0.55f));
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
            result.SetTriangles(triangles, 0);
            result.RecalculateNormals();
            result.RecalculateBounds();

            baseVertices = result.vertices;
            animatedVertices = result.vertices;
            return result;
        }
    }
}
