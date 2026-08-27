using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Desktopirates
{
    public sealed class WindowsOverlayController : MonoBehaviour
    {
        private const int WindowWidth = 720;
        private const int WindowHeight = 760;
        public const float MinimumWindowScale = 0.75f;
        public const float MaximumWindowScale = 1.50f;
        public float WindowScale { get; private set; } = 1f;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const int GwlStyle = -16;
        private const int GwlExStyle = -20;
        private const int WsCaption = 0x00C00000;
        private const int WsThickFrame = 0x00040000;
        private const int WsMinimizeBox = 0x00020000;
        private const int WsMaximizeBox = 0x00010000;
        private const int WsSysMenu = 0x00080000;
        private const int WsExLayered = 0x00080000;
        private const int WsExToolWindow = 0x00000080;
        private const uint LwaColorKey = 0x00000001;
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpFrameChanged = 0x0020;
        private const uint WmNcLButtonDown = 0x00A1;
        private const int HtCaption = 2;
        private const uint SpiGetWorkArea = 0x0030;
        private const uint MonitorDefaultToNearest = 0x00000002;
        private static readonly IntPtr HwndTopmost = new IntPtr(-1);

        private IntPtr windowHandle;
        private bool pointerDragging;
        private int recordedWindowX = int.MinValue;
        private int recordedWindowY = int.MinValue;
        private Point dragStartCursor;
        private Rect dragStartWindow;

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public Rect Monitor;
            public Rect Work;
            public uint Flags;
        }

        [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int index);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int index, int value);
        [DllImport("user32.dll")] private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint key, byte alpha, uint flags);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
        [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action, uint param, out Rect value, uint update);
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
#endif

        private void Awake()
        {
            Application.runInBackground = true;
            WindowScale = Mathf.Clamp(PlayerPrefs.GetFloat("window_scale", 1f), MinimumWindowScale, MaximumWindowScale);
            Screen.SetResolution(Mathf.RoundToInt(WindowWidth * WindowScale), Mathf.RoundToInt(WindowHeight * WindowScale), FullScreenMode.Windowed);
            StartCoroutine(ConfigureWindow());
        }

        public void BeginPointerDrag()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (windowHandle == IntPtr.Zero) windowHandle = GetActiveWindow();
            if (windowHandle == IntPtr.Zero) return;
            pointerDragging = GetCursorPos(out dragStartCursor) && GetWindowRect(windowHandle, out dragStartWindow);
#endif
        }

        public void UpdatePointerDrag()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (!pointerDragging || !GetCursorPos(out Point cursor)) return;
            int x = dragStartWindow.Left + cursor.X - dragStartCursor.X;
            int y = dragStartWindow.Top + cursor.Y - dragStartCursor.Y;
            SetWindowPos(windowHandle, HwndTopmost, x, y, 0, 0, SwpNoSize | SwpNoActivate);
#endif
        }

        public void EndPointerDrag()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            pointerDragging = false;
#endif
            RecordWindowPosition(false);
        }

        public void SetWindowScale(float scale)
        {
            WindowScale = Mathf.Clamp(scale, MinimumWindowScale, MaximumWindowScale);
            PlayerPrefs.SetFloat("window_scale", WindowScale);
            PlayerPrefs.Save();
            int width = Mathf.RoundToInt(WindowWidth * WindowScale);
            int height = Mathf.RoundToInt(WindowHeight * WindowScale);
            StartCoroutine(ResizeWindowWhenReady(width, height));
        }

        private IEnumerator ConfigureWindow()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(1.0f);
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            WindowScale = Mathf.Clamp(PlayerPrefs.GetFloat("window_scale", 1f), MinimumWindowScale, MaximumWindowScale);
            int scaledWidth = Mathf.RoundToInt(WindowWidth * WindowScale);
            int scaledHeight = Mathf.RoundToInt(WindowHeight * WindowScale);
            // Awake already requested this resolution. Repeating SetResolution one second
            // later recreated the D3D swap chain and surfaced as an intermittent ~500 ms
            // hitch several frames after the request had returned.
            if (Screen.width != scaledWidth || Screen.height != scaledHeight)
            {
                Screen.SetResolution(scaledWidth, scaledHeight, FullScreenMode.Windowed);
                yield return null;
            }
            yield return new WaitForEndOfFrame();

            if (!ApplyOverlayWindowStyle()) yield break;

            bool hasWorkArea = SystemParametersInfo(SpiGetWorkArea, 0, out Rect workArea, 0);
            IntPtr monitor = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
            var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref monitorInfo))
            {
                workArea = monitorInfo.Work;
                hasWorkArea = true;
            }
            int x;
            int y;
            if (PlayerPrefs.HasKey("window_x") && PlayerPrefs.HasKey("window_y"))
            {
                x = PlayerPrefs.GetInt("window_x");
                y = PlayerPrefs.GetInt("window_y");
            }
            else if (hasWorkArea)
            {
                x = workArea.Right - scaledWidth - 18;
                y = workArea.Bottom - scaledHeight - 12;
            }
            else
            {
                x = Screen.currentResolution.width - scaledWidth - 18;
                y = Screen.currentResolution.height - scaledHeight - 60;
            }

            if (hasWorkArea)
            {
                x = Mathf.Clamp(x, workArea.Left, Mathf.Max(workArea.Left, workArea.Right - scaledWidth));
                y = Mathf.Clamp(y, workArea.Top, Mathf.Max(workArea.Top, workArea.Bottom - scaledHeight));
            }

            SetWindowPos(windowHandle, HwndTopmost, x, y, scaledWidth, scaledHeight, SwpNoActivate | SwpFrameChanged);
            Debug.Log($"desktopirates overlay positioned at ({x}, {y}) in work area ({workArea.Left}, {workArea.Top})-({workArea.Right}, {workArea.Bottom}).");
#endif
        }

        private IEnumerator ResizeWindowWhenReady(int width, int height)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (windowHandle == IntPtr.Zero) windowHandle = GetActiveWindow();
            Rect originalRect = default;
            bool hasOriginalRect = windowHandle != IntPtr.Zero && GetWindowRect(windowHandle, out originalRect);
            int oldWidth = hasOriginalRect ? originalRect.Right - originalRect.Left : width;
            int oldHeight = hasOriginalRect ? originalRect.Bottom - originalRect.Top : height;
            int anchorX = hasOriginalRect ? originalRect.Left + Mathf.RoundToInt(oldWidth * 0.5f) : 0;
            int anchorY = hasOriginalRect ? originalRect.Top + Mathf.RoundToInt(oldHeight * WindowScaleMath.MenuCircleTopRatio) : 0;
#endif
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            yield return null;
            yield return new WaitForEndOfFrame();
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            // SetResolution may recreate the player HWND or restore its overlapped-window
            // decoration. Always reacquire it and strip the non-client frame before sizing.
            if (!ApplyOverlayWindowStyle()) yield break;
            if (hasOriginalRect)
            {
                Vector2Int position = WindowScaleMath.KeepMenuCircleFixed(
                    anchorX - Mathf.RoundToInt(oldWidth * 0.5f),
                    anchorY - Mathf.RoundToInt(oldHeight * WindowScaleMath.MenuCircleTopRatio),
                    oldWidth,
                    oldHeight,
                    width,
                    height);

                IntPtr monitor = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
                var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref monitorInfo))
                {
                    position.x = Mathf.Clamp(position.x, monitorInfo.Work.Left, Mathf.Max(monitorInfo.Work.Left, monitorInfo.Work.Right - width));
                    position.y = Mathf.Clamp(position.y, monitorInfo.Work.Top, Mathf.Max(monitorInfo.Work.Top, monitorInfo.Work.Bottom - height));
                }

                SetWindowPos(windowHandle, HwndTopmost, position.x, position.y, width, height, SwpNoActivate | SwpFrameChanged);
                // Unity can make one final style pass after SetResolution. Reapply our overlay
                // style on the next frame and force the exact outer size a second time.
                yield return null;
                if (!ApplyOverlayWindowStyle()) yield break;
                SetWindowPos(windowHandle, HwndTopmost, position.x, position.y, width, height, SwpNoActivate | SwpFrameChanged);
                PlayerPrefs.SetInt("window_x", position.x);
                PlayerPrefs.SetInt("window_y", position.y);
                PlayerPrefs.Save();
                Debug.Log($"desktopirates frameless overlay reapplied after resize to {width}x{height}.");
            }
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private bool ApplyOverlayWindowStyle()
        {
            windowHandle = GetActiveWindow();
            if (windowHandle == IntPtr.Zero) return false;

            int style = GetWindowLong(windowHandle, GwlStyle);
            style &= ~(WsCaption | WsThickFrame | WsMinimizeBox | WsMaximizeBox | WsSysMenu);
            SetWindowLong(windowHandle, GwlStyle, style);

            int extendedStyle = GetWindowLong(windowHandle, GwlExStyle);
            SetWindowLong(windowHandle, GwlExStyle, extendedStyle | WsExLayered | WsExToolWindow);
            SetLayeredWindowAttributes(windowHandle, 0x00FF00FF, 0, LwaColorKey);
            return true;
        }
#endif

        private void OnApplicationFocus(bool hasFocus)
        {
            // A desktop companion loses focus constantly while the user works elsewhere.
            // Record the position without forcing a synchronous registry/disk flush.
            if (!hasFocus) RecordWindowPosition(false);
        }

        private void OnApplicationQuit()
        {
            RecordWindowPosition(true);
        }

        private void RecordWindowPosition(bool flush)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (windowHandle != IntPtr.Zero && GetWindowRect(windowHandle, out Rect rect))
            {
                if (recordedWindowX != rect.Left || recordedWindowY != rect.Top)
                {
                    recordedWindowX = rect.Left;
                    recordedWindowY = rect.Top;
                    PlayerPrefs.SetInt("window_x", rect.Left);
                    PlayerPrefs.SetInt("window_y", rect.Top);
                }
                if (flush) PlayerPrefs.Save();
            }
#endif
        }
    }
}
