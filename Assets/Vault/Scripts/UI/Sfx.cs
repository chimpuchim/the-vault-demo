using UnityEngine;

namespace Vault
{
    /// <summary>Procedural UI sounds (sine blips) so the demo ships without audio files.</summary>
    public static class Sfx
    {
        static AudioSource _src;
        static AudioClip _tap, _good, _bad, _tick, _go, _win;
        public static bool Muted;

        static AudioSource Source
        {
            get
            {
                if (_src == null)
                {
                    var go = new GameObject("~Sfx");
                    Object.DontDestroyOnLoad(go);
                    _src = go.AddComponent<AudioSource>();
                    _src.playOnAwake = false;
                }
                return _src;
            }
        }

        public static void Tap() => Play(_tap ??= Tone(880f, 0.05f, 0.25f));
        public static void Tick() => Play(_tick ??= Tone(1320f, 0.04f, 0.18f));
        public static void Go() => Play(_go ??= Tone(1760f, 0.12f, 0.35f));
        public static void Good() => Play(_good ??= Chord(new[] { 660f, 990f }, 0.18f, 0.3f));
        public static void Bad() => Play(_bad ??= Chord(new[] { 196f, 185f }, 0.3f, 0.35f));
        public static void Win() => Play(_win ??= Chord(new[] { 523f, 659f, 784f, 1047f }, 0.9f, 0.3f));

        static void Play(AudioClip clip)
        {
            if (Muted || clip == null) return;
            Source.PlayOneShot(clip);
        }

        static AudioClip Tone(float freq, float seconds, float gain) => Chord(new[] { freq }, seconds, gain);

        static AudioClip Chord(float[] freqs, float seconds, float gain)
        {
            const int rate = 44100;
            int n = Mathf.CeilToInt(rate * seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Min(1f, i / (rate * 0.005f)) * Mathf.Pow(1f - i / (float)n, 2f);
                float v = 0;
                foreach (var f in freqs) v += Mathf.Sin(2f * Mathf.PI * f * t);
                data[i] = v / freqs.Length * env * gain;
            }
            var clip = AudioClip.Create("sfx", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
