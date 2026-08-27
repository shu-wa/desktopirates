using System;
using UnityEngine;

namespace Desktopirates
{
    /// <summary>Opt-in build diagnostic. It has no production Update cost unless explicitly requested.</summary>
    public sealed class RuntimePerformanceProbe : MonoBehaviour
    {
        private const float WarmupSeconds = 2f;
        private const float SampleSeconds = 8f;
        private BoatController boat;
        private PoiSystem pois;
        private float startedAt;
        private float accumulatedFrameTime;
        private float worstFrameTime;
        private int sampledFrames;
        private int framesOverBudget;
        private int maximumPoiCount;
        private float worstFrameAt;
        private int startingGen0Collections;
        private int startingGen1Collections;
        private long startingAllocatedBytes;
        private bool sampling;

        public void Initialize(BoatController player, PoiSystem poiSystem)
        {
            boat = player;
            pois = poiSystem;
            startedAt = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            float elapsed = Time.realtimeSinceStartup - startedAt;
            if (!sampling && elapsed >= WarmupSeconds)
            {
                sampling = true;
                accumulatedFrameTime = 0f;
                worstFrameTime = 0f;
                sampledFrames = 0;
                framesOverBudget = 0;
                worstFrameAt = 0f;
                startingGen0Collections = GC.CollectionCount(0);
                startingGen1Collections = GC.CollectionCount(1);
                startingAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
            }
            if (!sampling) return;

            float frameTime = Time.unscaledDeltaTime;
            accumulatedFrameTime += frameTime;
            if (frameTime > worstFrameTime)
            {
                worstFrameTime = frameTime;
                worstFrameAt = elapsed - WarmupSeconds;
            }
            sampledFrames++;
            if (frameTime > 1f / 24f) framesOverBudget++;
            if (pois != null) maximumPoiCount = Mathf.Max(maximumPoiCount, pois.Items.Count);

            if (elapsed < WarmupSeconds + SampleSeconds) return;
            float averageFrameTime = accumulatedFrameTime / Mathf.Max(1, sampledFrames);
            Debug.Log(
                $"PERFORMANCE_QA averageFps={1f / Mathf.Max(0.0001f, averageFrameTime):0.0} " +
                $"worstFrameMs={worstFrameTime * 1000f:0.0}@{worstFrameAt:0.00}s over41ms={framesOverBudget}/{sampledFrames} " +
                $"gc0={GC.CollectionCount(0) - startingGen0Collections} gc1={GC.CollectionCount(1) - startingGen1Collections} " +
                $"managedAllocKb={(GC.GetAllocatedBytesForCurrentThread() - startingAllocatedBytes) / 1024f:0.0} " +
                $"maxLivePois={maximumPoiCount} distance={boat?.State?.Captain.DistanceSailed:0.0}");
            Application.Quit(0);
            enabled = false;
        }
    }
}
