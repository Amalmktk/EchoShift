using System.Collections.Generic;
using UnityEngine;

namespace EchoShift.Game
{
    public enum Sfx
    {
        Tap,
        Back,
        Step,
        EchoStep,
        Bump,
        Toggle,
        PlateOn,
        DoorOpen,
        DoorClose,
        Crystal,
        EchoMade,
        Undo,
        Restart,
        Win,
        Fail,
        Star,
        Locked,
        Purchase
    }

    /// <summary>
    /// Placeholder sound effects synthesised at start-up, so the game has audio with no asset files.
    /// Replace with real clips later by assigning them in <see cref="Override"/>.
    /// </summary>
    public class AudioService : MonoBehaviour
    {
        const int Rate = 44100;
        readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();
        AudioSource _source;
        SaveSystem _save;

        public void Init(SaveSystem save)
        {
            _save = save;
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            _clips[Sfx.Tap] = Tone(1200, 1200, 0.025f, 0.25f, Wave.Square);
            _clips[Sfx.Back] = Tone(900, 700, 0.04f, 0.2f, Wave.Square);
            _clips[Sfx.Step] = Tone(520, 460, 0.05f, 0.25f, Wave.Triangle);
            _clips[Sfx.EchoStep] = Tone(880, 820, 0.04f, 0.08f, Wave.Sine);
            _clips[Sfx.Bump] = Tone(150, 110, 0.07f, 0.35f, Wave.Square);
            _clips[Sfx.Toggle] = Tone(700, 1000, 0.06f, 0.3f, Wave.Square);
            _clips[Sfx.PlateOn] = Tone(500, 800, 0.08f, 0.3f, Wave.Triangle);
            _clips[Sfx.DoorOpen] = Chord(new[] { 440f, 660f }, 0.12f, 0.25f);
            _clips[Sfx.DoorClose] = Chord(new[] { 330f, 220f }, 0.12f, 0.25f);
            _clips[Sfx.Crystal] = Arp(new[] { 988f, 1319f, 1760f }, 0.06f, 0.25f);
            _clips[Sfx.EchoMade] = Tone(220, 990, 0.25f, 0.3f, Wave.Sine);
            _clips[Sfx.Undo] = Tone(700, 400, 0.08f, 0.2f, Wave.Triangle);
            _clips[Sfx.Restart] = Tone(900, 200, 0.2f, 0.25f, Wave.Triangle);
            _clips[Sfx.Win] = Arp(new[] { 523f, 659f, 784f, 1047f }, 0.09f, 0.3f);
            _clips[Sfx.Fail] = Arp(new[] { 392f, 330f, 262f }, 0.1f, 0.3f);
            _clips[Sfx.Star] = Tone(1047, 1568, 0.12f, 0.3f, Wave.Sine);
            _clips[Sfx.Locked] = Tone(200, 200, 0.08f, 0.3f, Wave.Square);
            _clips[Sfx.Purchase] = Arp(new[] { 784f, 988f, 1175f, 1568f }, 0.07f, 0.3f);
        }

        public void Override(Sfx sfx, AudioClip clip) => _clips[sfx] = clip;

        public void Play(Sfx sfx, float volume = 1f)
        {
            if (_save == null || !_save.Data.settings.sfx) return;
            if (_clips.TryGetValue(sfx, out var clip)) _source.PlayOneShot(clip, volume);
        }

        enum Wave { Sine, Square, Triangle }

        static float Sample(Wave w, float phase)
        {
            switch (w)
            {
                case Wave.Square: return Mathf.Sin(phase) >= 0 ? 0.6f : -0.6f;
                case Wave.Triangle: return 2f / Mathf.PI * Mathf.Asin(Mathf.Sin(phase));
                default: return Mathf.Sin(phase);
            }
        }

        static AudioClip Tone(float f0, float f1, float seconds, float volume, Wave wave)
        {
            int n = Mathf.Max(1, (int)(Rate * seconds));
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += 2f * Mathf.PI * f / Rate;
                float env = Mathf.Min(1f, t * 40f) * (1f - t);
                data[i] = Sample(wave, phase) * env * volume;
            }
            var clip = AudioClip.Create("sfx", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Chord(float[] freqs, float seconds, float volume)
        {
            int n = (int)(Rate * seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float env = Mathf.Min(1f, t * 40f) * (1f - t);
                float s = 0f;
                foreach (var f in freqs) s += Mathf.Sin(2f * Mathf.PI * f * i / Rate);
                data[i] = s / freqs.Length * env * volume;
            }
            var clip = AudioClip.Create("sfx", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Arp(float[] notes, float noteSeconds, float volume)
        {
            int per = (int)(Rate * noteSeconds);
            var data = new float[per * notes.Length];
            for (int k = 0; k < notes.Length; k++)
            {
                for (int i = 0; i < per; i++)
                {
                    float t = (float)i / per;
                    float env = Mathf.Min(1f, t * 30f) * (1f - t * 0.7f);
                    data[k * per + i] = Mathf.Sin(2f * Mathf.PI * notes[k] * i / Rate) * env * volume;
                }
            }
            var clip = AudioClip.Create("sfx", data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
