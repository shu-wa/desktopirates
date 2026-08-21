using UnityEngine;

namespace Desktopirates
{
    public static class EnemyHudModel
    {
        public static float GetHealthRatio(int health, int maxHealth)
            => Mathf.Clamp01(Mathf.Max(0, health) / (float)Mathf.Max(1, maxHealth));

        public static string GetHealthLabel(int health, int maxHealth)
            => $"{Mathf.Clamp(health, 0, Mathf.Max(1, maxHealth))} / {Mathf.Max(1, maxHealth)}";

        public static float GetBarWidth(float fullWidth, int health, int maxHealth)
            => Mathf.Max(0f, fullWidth) * GetHealthRatio(health, maxHealth);
    }
}
