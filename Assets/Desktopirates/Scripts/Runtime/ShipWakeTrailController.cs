using System;
using System.Collections.Generic;
using UnityEngine;

namespace Desktopirates
{
    public static class WakeTrailMath
    {
        public const float MinimumSpeed = 0.16f;
        public const float SegmentSpacing = 0.28f;
        public const float Lifetime = 5.8f;
        public const float TeleportDistance = 2.5f;

        public static Vector2 GetSternPosition(Vector2 shipPosition, float headingDegrees, int shipLevel)
        {
            float radians = headingDegrees * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
            float sternOffset = 0.62f + Mathf.Clamp(shipLevel, 0, ShipProgressionModel.TierCount - 1) * 0.09f;
            return shipPosition - forward * sternOffset;
        }

        public static bool ShouldEmit(Vector2 lastShipPosition, Vector2 shipPosition, float speed)
            => speed >= MinimumSpeed && Vector2.Distance(lastShipPosition, shipPosition) >= SegmentSpacing;

        public static bool IsTeleport(Vector2 lastShipPosition, Vector2 shipPosition)
            => Vector2.Distance(lastShipPosition, shipPosition) > TeleportDistance;

        public static float GetOpacity(float age, float lifetime)
        {
            if (lifetime <= 0f || age < 0f || age >= lifetime) return 0f;
            float fadeIn = Mathf.Clamp01(age / 0.10f);
            float fadeOut = 1f - Mathf.InverseLerp(lifetime * 0.48f, lifetime, age);
            return fadeIn * Mathf.Clamp01(fadeOut);
        }

        public static Vector2 ToCameraRelative(Vector2 recordedPosition, Vector2 currentShipPosition)
            => recordedPosition - currentShipPosition;
    }

    /// <summary>Records foam at sailed world coordinates, then reprojects it around the centered ship.</summary>
    public sealed class ShipWakeTrailController : MonoBehaviour
    {
        private sealed class WakeSegment
        {
            public Vector2 LogicalPosition;
            public float BornAt;
            public float Strength;
            public Transform Visual;
            public MeshRenderer Renderer;
            public readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
        }

        private readonly List<WakeSegment> segments = new List<WakeSegment>();
        private BoatController boat;
        private Material material;
        private Mesh wakeMesh;
        private Vector2 lastShipPosition;
        private bool hasLastPosition;
        private int emissionSerial;
        private bool previewDiagnostics;
        private float nextDiagnostic;

        public void Initialize(BoatController player)
        {
            boat = player;
            previewDiagnostics = Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--wake-preview");
            wakeMesh = CreateWakeMesh(player != null && player.State != null ? player.State.ShipLevel : 0);
            Material source = Resources.Load<Material>("WakeTrailMaterial") ?? Resources.Load<Material>("StandardMaterial");
            material = source != null ? new Material(source) : new Material(Shader.Find("Standard"));
            material.name = "Recorded Ship Wake";
            if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3f);
            if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
            if (material.HasProperty("_Cull")) material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 20;
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            material.SetColor("_Color", new Color(0.60f, 0.93f, 0.95f, 0.82f));
        }

        private void LateUpdate()
        {
            if (boat == null) return;
            Vector2 current = boat.LogicalPosition;
            if (!hasLastPosition)
            {
                lastShipPosition = current;
                hasLastPosition = true;
            }
            else if (WakeTrailMath.IsTeleport(lastShipPosition, current))
            {
                lastShipPosition = current;
            }
            else if (WakeTrailMath.ShouldEmit(lastShipPosition, current, boat.Speed))
            {
                Vector2 direction = (current - lastShipPosition).normalized;
                float remaining = Vector2.Distance(lastShipPosition, current);
                while (remaining >= WakeTrailMath.SegmentSpacing)
                {
                    lastShipPosition += direction * WakeTrailMath.SegmentSpacing;
                    Vector2 stern = WakeTrailMath.GetSternPosition(lastShipPosition, boat.HeadingDegrees, boat.State != null ? boat.State.ShipLevel : 0);
                    Emit(stern, boat.HeadingDegrees, Mathf.InverseLerp(WakeTrailMath.MinimumSpeed, Mathf.Max(WakeTrailMath.MinimumSpeed + 0.01f, boat.MaxSpeed), boat.Speed));
                    remaining = Vector2.Distance(lastShipPosition, current);
                }
            }

            for (int i = segments.Count - 1; i >= 0; i--)
            {
                WakeSegment segment = segments[i];
                float age = Time.time - segment.BornAt;
                float opacity = WakeTrailMath.GetOpacity(age, WakeTrailMath.Lifetime);
                if (opacity <= 0f && age >= WakeTrailMath.Lifetime)
                {
                    Destroy(segment.Visual.gameObject);
                    segments.RemoveAt(i);
                    continue;
                }

                Vector2 relative = WakeTrailMath.ToCameraRelative(segment.LogicalPosition, current);
                // Ocean vertices crest near 0.09. Keep foam just above the animated
                // surface so it is not depth-occluded, while remaining below the hull.
                segment.Visual.localPosition = new Vector3(relative.x, 0.12f, relative.y);
                float visibleRadius = OceanDisc.Radius - 0.15f;
                segment.Renderer.enabled = relative.sqrMagnitude <= visibleRadius * visibleRadius;
                Color foam = new Color(0.60f, 0.93f, 0.95f, opacity * segment.Strength);
                segment.Properties.SetColor("_Color", foam);
                segment.Renderer.SetPropertyBlock(segment.Properties);
            }

            if (previewDiagnostics && Time.time >= nextDiagnostic)
            {
                nextDiagnostic = Time.time + 1f;
                Debug.Log($"Wake QA: speed={boat.Speed:0.00} step={boat.CruiseStep} position={boat.LogicalPosition} segments={segments.Count}");
            }
        }

        private void Emit(Vector2 logicalPosition, float headingDegrees, float speedRatio)
        {
            var visualObject = new GameObject("Recorded Wake Segment", typeof(MeshFilter), typeof(MeshRenderer));
            visualObject.transform.SetParent(transform, false);
            visualObject.transform.localRotation = Quaternion.Euler(0f, headingDegrees, 0f);
            MeshFilter filter = visualObject.GetComponent<MeshFilter>();
            filter.sharedMesh = wakeMesh;
            MeshRenderer renderer = visualObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            segments.Add(new WakeSegment
            {
                LogicalPosition = logicalPosition,
                BornAt = Time.time,
                Strength = Mathf.Lerp(0.18f, 0.38f, Mathf.Clamp01(speedRatio)),
                Visual = visualObject.transform,
                Renderer = renderer
            });
            float variation = Mathf.Sin(++emissionSerial * 12.9898f);
            visualObject.transform.localRotation = Quaternion.Euler(0f, headingDegrees + variation * 3.5f, 0f);
            visualObject.transform.localScale = new Vector3(0.88f + Mathf.Abs(variation) * 0.18f, 1f, 0.78f + Mathf.Abs(Mathf.Cos(emissionSerial * 2.17f)) * 0.24f);
        }

        private static Mesh CreateWakeMesh(int shipLevel)
        {
            float spread = 0.16f + Mathf.Clamp(shipLevel, 0, ShipProgressionModel.TierCount - 1) * 0.025f;
            float width = 0.035f + Mathf.Clamp(shipLevel, 0, ShipProgressionModel.TierCount - 1) * 0.004f;
            float front = 0.07f;
            float back = -0.07f;
            Vector3[] vertices =
            {
                new Vector3(-spread + width * 0.45f, 0f, front), new Vector3(-spread - width * 0.45f, 0f, front),
                new Vector3(-spread - width * 0.72f, 0f, back), new Vector3(-spread + width * 0.30f, 0f, back),
                new Vector3(spread - width * 0.45f, 0f, front), new Vector3(spread + width * 0.45f, 0f, front),
                new Vector3(spread + width * 0.72f, 0f, back), new Vector3(spread - width * 0.30f, 0f, back)
            };
            var mesh = new Mesh { name = "Twin Recorded Wake Mesh", vertices = vertices };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6 };
            var normals = new Vector3[vertices.Length];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (wakeMesh != null) Destroy(wakeMesh);
        }
    }
}
