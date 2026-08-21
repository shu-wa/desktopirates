using UnityEngine;

namespace Desktopirates
{
    /// <summary>Staggers the authored menu branches as if they unfold from the menu circle.</summary>
    public sealed class MenuBranchAnimator : MonoBehaviour
    {
        private RectTransform[] rows;
        private CanvasGroup[] groups;
        private float startedAt;

        public void Initialize(RectTransform[] branchRows)
        {
            rows = branchRows;
            groups = new CanvasGroup[rows.Length];
            for (int i = 0; i < rows.Length; i++)
                groups[i] = rows[i].gameObject.GetComponent<CanvasGroup>() ?? rows[i].gameObject.AddComponent<CanvasGroup>();
            Restart();
        }

        private void OnEnable()
        {
            if (rows != null) Restart();
        }

        private void Restart()
        {
            startedAt = Time.unscaledTime;
            for (int i = 0; i < rows.Length; i++)
            {
                groups[i].alpha = 0f;
                groups[i].interactable = false;
                groups[i].blocksRaycasts = false;
                rows[i].localScale = new Vector3(0.82f, 0.82f, 1f);
            }
        }

        private void Update()
        {
            if (rows == null) return;
            for (int i = 0; i < rows.Length; i++)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - startedAt - i * 0.055f) * 8.5f);
                t = t * t * (3f - 2f * t);
                groups[i].alpha = t;
                groups[i].interactable = t >= 0.98f;
                groups[i].blocksRaycasts = t >= 0.98f;
                rows[i].localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, t);
            }
        }
    }
}
