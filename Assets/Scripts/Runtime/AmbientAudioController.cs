using UnityEngine;

namespace Desktopirates
{
    public sealed class AmbientAudioController : MonoBehaviour
    {
        private void Awake()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = CreateOceanLoop();
            source.loop = true;
            source.volume = 0.22f;
            source.spatialBlend = 0f;
            source.Play();
        }

        private static AudioClip CreateOceanLoop()
        {
            const int sampleRate = 22050;
            const int seconds = 5;
            float[] samples = new float[sampleRate * seconds];
            uint random = 0xD35C0A57;
            float smooth = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                random = random * 1664525u + 1013904223u;
                float noise = ((random >> 8) / 16777215f) * 2f - 1f;
                smooth = Mathf.Lerp(smooth, noise, 0.012f);
                float swell = 0.55f + 0.45f * Mathf.Sin(i / (float)sampleRate * Mathf.PI * 0.42f);
                samples[i] = smooth * swell * 0.28f;
            }
            AudioClip clip = AudioClip.Create("Procedural Ocean", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
