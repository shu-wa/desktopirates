using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Desktopirates
{
    public static class CombatVfxMath
    {
        public const float BurningDuration = 5f;
        public const float SinkingDuration = 3f;

        public static float GetProjectileDuration(float distance)
            => Mathf.Clamp(distance / 7.5f, 0.28f, 0.68f);

        public static float GetSinkProgress(float elapsed)
            => Mathf.Clamp01((elapsed - BurningDuration) / SinkingDuration);
    }

    /// <summary>Small opaque, low-poly combat effects that remain readable in the desktop overlay.</summary>
    public sealed class CombatVfxController : MonoBehaviour
    {
        private Transform effectRoot;
        private Material ember;
        private Material flame;
        private Material smoke;
        private Material shot;

        public void Initialize(Transform worldRoot)
        {
            effectRoot = new GameObject("Blocky Combat Effects").transform;
            effectRoot.SetParent(worldRoot, false);
            ember = MakeMaterial("Ember", new Color(1f, 0.38f, 0.05f));
            flame = MakeMaterial("Flame", new Color(1f, 0.72f, 0.12f));
            smoke = MakeMaterial("Powder Smoke", new Color(0.44f, 0.48f, 0.46f));
            shot = MakeMaterial("Cannon Ball", new Color(0.07f, 0.08f, 0.075f));
        }

        public void PlayPlayerSalvo(
            Transform ship,
            IReadOnlyList<CannonSlot> firingSlots,
            Transform target,
            bool sinking,
            Action impact,
            Action completed)
        {
            if (ship == null || target == null || firingSlots == null || firingSlots.Count == 0)
            {
                impact?.Invoke();
                completed?.Invoke();
                return;
            }
            StartCoroutine(SalvoRoutine(ship, firingSlots, target, sinking, impact, completed));
        }

        public void PlayEnemyShot(Transform source, Transform target, Action impact)
        {
            if (source == null || target == null) { impact?.Invoke(); return; }
            StartCoroutine(EnemyShotRoutine(source, target, impact));
        }

        public void PlaySinking(Transform target, Action completed)
        {
            if (target == null) { completed?.Invoke(); return; }
            StartCoroutine(StandaloneSinkRoutine(target, completed));
        }

        private IEnumerator StandaloneSinkRoutine(Transform target, Action completed)
        {
            yield return SinkRoutine(target);
            completed?.Invoke();
        }

        private IEnumerator SalvoRoutine(Transform ship, IReadOnlyList<CannonSlot> slots, Transform target, bool sinking, Action impact, Action completed)
        {
            Vector3 targetPoint = target.position + Vector3.up * 0.22f;
            float longestFlight = 0f;
            for (int i = 0; i < slots.Count; i++)
            {
                Transform carriage = FindCarriage(ship, slots[i]);
                Vector3 muzzle = carriage != null ? carriage.TransformPoint(0f, 0.07f, 0.32f) : ship.position + Vector3.up * 0.28f;
                Vector3 direction = carriage != null ? carriage.forward : (targetPoint - muzzle).normalized;
                EmitMuzzle(muzzle, direction);
                float duration = CombatVfxMath.GetProjectileDuration(Vector3.Distance(muzzle, targetPoint));
                longestFlight = Mathf.Max(longestFlight, duration);
                // A broadside is one command: every manned cannon in the arc fires on
                // the same frame, then the whole battery shares one reload cooldown.
                StartCoroutine(ProjectileRoutine(muzzle, target, duration, 0f));
            }

            yield return new WaitForSeconds(longestFlight);
            if (target == null) { completed?.Invoke(); yield break; }
            EmitImpact(target.position + Vector3.up * 0.24f);
            impact?.Invoke();
            if (sinking) yield return SinkRoutine(target);
            completed?.Invoke();
        }

        private IEnumerator EnemyShotRoutine(Transform source, Transform target, Action impact)
        {
            Vector3 muzzle = source.position + Vector3.up * 0.30f;
            Vector3 direction = (target.position - source.position).normalized;
            EmitMuzzle(muzzle, direction);
            float duration = CombatVfxMath.GetProjectileDuration(Vector3.Distance(source.position, target.position));
            yield return ProjectileRoutine(muzzle, target, duration, 0f);
            if (target != null) EmitImpact(target.position + Vector3.up * 0.24f);
            impact?.Invoke();
        }

        private IEnumerator ProjectileRoutine(Vector3 start, Transform target, float duration, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            GameObject ball = MakeBlock("Cannon Ball", start, Vector3.one * 0.095f, shot);
            Vector3 destination = target != null ? target.position + Vector3.up * 0.22f : start;
            for (float elapsed = 0f; elapsed < duration && ball != null; elapsed += Time.deltaTime)
            {
                if (target != null) destination = target.position + Vector3.up * 0.22f;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 point = Vector3.Lerp(start, destination, t);
                point.y += Mathf.Sin(t * Mathf.PI) * 0.42f;
                ball.transform.position = point;
                yield return null;
            }
            if (ball != null) Destroy(ball);
        }

        private IEnumerator SinkRoutine(Transform target)
        {
            Vector3 startingPosition = target.localPosition;
            Quaternion startingRotation = target.localRotation;
            float nextParticle = 0f;
            float duration = CombatVfxMath.BurningDuration + CombatVfxMath.SinkingDuration;
            for (float elapsed = 0f; elapsed < duration && target != null; elapsed += Time.deltaTime)
            {
                float sinkT = CombatVfxMath.GetSinkProgress(elapsed);
                // X/Z are maintained by PoiSystem so the wreck stays at its world coordinate.
                Vector3 currentPosition = target.localPosition;
                target.localPosition = new Vector3(currentPosition.x, startingPosition.y - sinkT * 1.20f, currentPosition.z);
                target.localRotation = startingRotation * Quaternion.Euler(19f * sinkT, 0f, 31f * sinkT);
                if (elapsed >= nextParticle)
                {
                    Vector3 center = target.position + Vector3.up * 0.28f;
                    Vector3 jitter = new Vector3(Mathf.Sin(elapsed * 17f), 0f, Mathf.Cos(elapsed * 13f)) * 0.20f;
                    StartCoroutine(BlockParticle(center + jitter, Vector3.up * 0.46f, 0.55f, Vector3.one * 0.23f, flame, false));
                    StartCoroutine(BlockParticle(center - jitter, Vector3.up * 0.34f, 0.94f, Vector3.one * 0.26f, smoke, true));
                    nextParticle += sinkT <= 0f ? 0.22f : 0.13f;
                }
                yield return null;
            }
        }

        private void EmitMuzzle(Vector3 position, Vector3 direction)
        {
            Vector3 flat = new Vector3(direction.x, 0.12f, direction.z).normalized;
            for (int i = 0; i < 4; i++)
            {
                Vector3 fan = Quaternion.Euler(0f, -18f + i * 12f, 0f) * flat;
                StartCoroutine(BlockParticle(position, fan * (0.55f + i * 0.11f), 0.32f, Vector3.one * (0.10f + i * 0.018f), i < 2 ? flame : ember, false));
            }
            for (int i = 0; i < 3; i++)
                StartCoroutine(BlockParticle(position + flat * 0.06f, flat * (0.18f + i * 0.07f) + Vector3.up * 0.18f, 0.78f + i * 0.1f, Vector3.one * (0.18f + i * 0.04f), smoke, true));
        }

        private void EmitImpact(Vector3 position)
        {
            for (int i = 0; i < 9; i++)
            {
                float angle = i * 137.5f * Mathf.Deg2Rad;
                Vector3 velocity = new Vector3(Mathf.Sin(angle), 0.35f + (i % 3) * 0.16f, Mathf.Cos(angle)) * (0.48f + (i % 2) * 0.24f);
                StartCoroutine(BlockParticle(position, velocity, 0.46f + (i % 3) * 0.08f, Vector3.one * 0.13f, i % 3 == 0 ? flame : ember, false));
            }
            for (int i = 0; i < 4; i++)
                StartCoroutine(BlockParticle(position, new Vector3((i - 1.5f) * 0.09f, 0.34f, (1.5f - i) * 0.07f), 0.72f + i * 0.08f, Vector3.one * (0.16f + i * 0.035f), smoke, true));
        }

        private IEnumerator BlockParticle(Vector3 position, Vector3 velocity, float lifetime, Vector3 size, Material material, bool grows)
        {
            GameObject block = MakeBlock("Pixel Effect", position, size, material);
            for (float elapsed = 0f; elapsed < lifetime && block != null; elapsed += Time.deltaTime)
            {
                block.transform.position += velocity * Time.deltaTime;
                velocity += Vector3.down * (grows ? -0.08f : 1.15f) * Time.deltaTime;
                float t = elapsed / lifetime;
                block.transform.localScale = size * (grows ? Mathf.Lerp(0.65f, 1.6f, t) : Mathf.Lerp(1f, 0.12f, t));
                yield return null;
            }
            if (block != null) Destroy(block);
        }

        private GameObject MakeBlock(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(effectRoot, true);
            block.transform.position = position;
            block.transform.localScale = scale;
            Collider collider = block.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            MeshRenderer renderer = block.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return block;
        }

        private static Transform FindCarriage(Transform ship, CannonSlot slot)
        {
            Transform mounts = ship.Find("Custom Cannon Mounts");
            return mounts != null ? mounts.Find($"{slot} Carriage") : null;
        }

        private static Material MakeMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }
    }
}
