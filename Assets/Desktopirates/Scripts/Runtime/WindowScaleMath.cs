using UnityEngine;

namespace Desktopirates
{
    public static class WindowScaleMath
    {
        // The menu-circle center is 66 reference pixels below the top edge.
        public const float MenuCircleTopRatio = 66f / 760f;

        public static Vector2Int KeepMenuCircleFixed(
            int oldLeft,
            int oldTop,
            int oldWidth,
            int oldHeight,
            int newWidth,
            int newHeight)
        {
            int anchorX = oldLeft + Mathf.RoundToInt(oldWidth * 0.5f);
            int anchorY = oldTop + Mathf.RoundToInt(oldHeight * MenuCircleTopRatio);
            return new Vector2Int(
                anchorX - Mathf.RoundToInt(newWidth * 0.5f),
                anchorY - Mathf.RoundToInt(newHeight * MenuCircleTopRatio));
        }
    }
}
