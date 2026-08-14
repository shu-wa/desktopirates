using System;
using System.Collections;
using UnityEngine;

namespace Desktopirates
{
    /// <summary>Opt-in visual QA capture used only when a --capture= path is supplied.</summary>
    public sealed class RuntimeScreenshotController : MonoBehaviour
    {
        public void TryStartFromCommandLine()
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (!argument.StartsWith("--capture=", StringComparison.OrdinalIgnoreCase)) continue;
                string path = argument.Substring("--capture=".Length).Trim('"');
                if (!string.IsNullOrWhiteSpace(path)) StartCoroutine(Capture(path));
                return;
            }
        }

        private IEnumerator Capture(string path)
        {
            yield return new WaitForSecondsRealtime(5.5f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path, 1);
            yield return new WaitForSecondsRealtime(1.5f);
            Application.Quit();
        }
    }
}
