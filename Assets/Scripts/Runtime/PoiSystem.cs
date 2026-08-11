using System.Collections.Generic;
using UnityEngine;

namespace Desktopirates
{
    public sealed class PoiRecord
    {
        public PoiKind Kind;
        public Vector2 LogicalPosition;
        public Transform Visual;
    }

    public sealed class PoiSystem : MonoBehaviour
    {
        public const float VisibleRadius = 6.1f;
        public IReadOnlyList<PoiRecord> Items => items;

        private readonly List<PoiRecord> items = new List<PoiRecord>();
        private BoatController player;

        public void Initialize(BoatController boat)
        {
            player = boat;
            Add(PoiKind.Enemy, new Vector2(20f, 25f));
            Add(PoiKind.Wreck, new Vector2(-34f, 12f));
            Add(PoiKind.Treasure, new Vector2(29f, -31f));
        }

        private void Update()
        {
            if (player == null) return;
            foreach (PoiRecord item in items)
            {
                Vector2 relative = item.LogicalPosition - player.LogicalPosition;
                float distance = relative.magnitude;
                bool visible = distance <= VisibleRadius;
                if (item.Visual.gameObject.activeSelf != visible) item.Visual.gameObject.SetActive(visible);
                if (!visible) continue;

                item.Visual.localPosition = new Vector3(relative.x, 0.30f, relative.y);
                item.Visual.localRotation = Quaternion.Euler(0f, item.Kind == PoiKind.Enemy ? 160f : 0f, 0f);
            }
        }

        private void Add(PoiKind kind, Vector2 logicalPosition)
        {
            Transform visual = ProceduralSceneFactory.CreatePoiVisual(kind, transform);
            visual.gameObject.SetActive(false);
            items.Add(new PoiRecord
            {
                Kind = kind,
                LogicalPosition = logicalPosition,
                Visual = visual
            });
        }
    }
}
