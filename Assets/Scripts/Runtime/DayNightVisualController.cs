using System;
using UnityEngine;

namespace Desktopirates
{
    public sealed class DayNightVisualController : MonoBehaviour
    {
        public float CurrentHour { get; private set; }

        private OceanDisc ocean;
        private Light sun;
        private float nextUpdate;
        private float previewOffset;

        public void Initialize(OceanDisc oceanDisc, Light directionalLight)
        {
            ocean = oceanDisc;
            sun = directionalLight;
            Refresh();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                previewOffset += 6f;
                Refresh();
            }

            if (Time.unscaledTime >= nextUpdate) Refresh();
        }

        private void Refresh()
        {
            DateTime now = DateTime.Now;
            CurrentHour = DayCycle.WrapHour(now.Hour + now.Minute / 60f + previewOffset);
            DayCycleState state = DayCycle.Evaluate(CurrentHour);
            if (ocean != null) ocean.Tint = state.Water;
            RenderSettings.ambientLight = state.Ambient;
            if (sun != null)
            {
                sun.color = Color.Lerp(state.SkyBottom, Color.white, 0.35f);
                sun.intensity = state.Phase == TimeOfDayPhase.Night ? 0.66f : 0.95f;
                sun.transform.rotation = Quaternion.Euler(48f, CurrentHour * 15f - 90f, 0f);
            }
            nextUpdate = Time.unscaledTime + 20f;
        }
    }
}
