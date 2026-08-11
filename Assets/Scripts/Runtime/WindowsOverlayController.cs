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

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

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
        [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action, uint param, out Rect value, uint update);
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
#endif

        private void Awake()
        {
            Application.runInBackground = true;
            Screen.SetResolution(WindowWidth, WindowHeight, FullScreenMode.Windowed);
            StartCoroutine(ConfigureWindow());
        }

        public void BeginWindowDrag()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (windowHandle == IntPtr.Zero) windowHandle = GetActiveWindow();
            if (windowHandle == IntPtr.Zero) return;
            ReleaseCapture();
            SendMessage(windowHandle, WmNcLButtonDown, new IntPtr(HtCaption), IntPtr.Zero);
#endif
        }

        private IEnumerator ConfigureWindow()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(1.0f);
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            windowHandle = GetActiveWindow();
            if (windowHandle == IntPtr.Zero) yield break;

            int style = GetWindowLong(windowHandle, GwlStyle);
            style &= ~(WsCaption | WsThickFrame | WsMinimizeBox | WsMaximizeBox | WsSysMenu);
            SetWindowLong(windowHandle, GwlStyle, style);

            int extendedStyle = GetWindowLong(windowHandle, GwlExStyle);
            SetWindowLong(windowHandle, GwlExStyle, extendedStyle | WsExLayered | WsExToolWindow);
            SetLayeredWindowAttributes(windowHandle, 0x00FF00FF, 0, LwaColorKey);

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
                x = workArea.Right - WindowWidth - 18;
                y = workArea.Bottom - WindowHeight - 12;
            }
            else
            {
                x = Screen.currentResolution.width - WindowWidth - 18;
                y = Screen.currentResolution.height - WindowHeight - 60;
            }

            if (hasWorkArea)
            {
                x = Mathf.Clamp(x, workArea.Left, Mathf.Max(workArea.Left, workArea.Right - WindowWidth));
                y = Mathf.Clamp(y, workArea.Top, Mathf.Max(workArea.Top, workArea.Bottom - WindowHeight));
            }

            SetWindowPos(windowHandle, HwndTopmost, x, y, WindowWidth, WindowHeight, SwpNoActivate | SwpFrameChanged);
            Debug.Log($"desktopirates overlay positioned at ({x}, {y}) in work area ({workArea.Left}, {workArea.Top})-({workArea.Right}, {workArea.Bottom}).");
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SaveWindowPosition();
        }

        private void OnApplicationQuit()
        {
            SaveWindowPosition();
        }

        private void SaveWindowPosition()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (windowHandle != IntPtr.Zero && GetWindowRect(windowHandle, out Rect rect))
            {
                PlayerPrefs.SetInt("window_x", rect.Left);
                PlayerPrefs.SetInt("window_y", rect.Top);
                PlayerPrefs.Save();
            }
#endif
        }
    }
}
