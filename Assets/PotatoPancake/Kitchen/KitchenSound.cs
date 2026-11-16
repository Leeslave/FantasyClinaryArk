using System;
using System.Collections.Generic;
using UnityEngine;

namespace FantasyClinaryArk
{
    internal enum Sfx { Tap, Ok, Perfect, Bad, Peel, Grate, Drip, Stamp, Tick, Pour, Salt, Flip, Sizzle, Stir, Plop }

    /// <summary>Small synthesized sound effects, rendered once into AudioClips (no audio files needed).</summary>
    internal sealed class KitchenSound
    {
        private const int Rate = 22050;
        private const float Master = .45f;
        private static Dictionary<Sfx, AudioClip[]> clips;
        private readonly AudioSource source;

        public KitchenSound(AudioSource source)
        {
            this.source = source;
            // Clips made at runtime are destroyed when play mode ends, even if the static survives.
            if (clips == null || clips[Sfx.Tap][0] == null)
                Render();
        }

        public void Play(Sfx effect)
        {
            if (source == null || !source.isActiveAndEnabled)
                return;
            AudioClip[] variants = clips[effect];
            source.PlayOneShot(variants[UnityEngine.Random.Range(0, variants.Length)]);
        }

        private enum Wave { Square, Sine, Triangle, Saw }
        private enum Filter { Band, Low, High }

        private sealed class Track
        {
            public readonly float[] Data;
            public Track(float seconds) => Data = new float[Mathf.CeilToInt(seconds * Rate)];

            public Track Tone(float freq, float duration, Wave wave, float volume, float slide = 0, float delay = 0)
            {
                int start = (int)(delay * Rate), count = (int)(duration * Rate);
                float end = Mathf.Max(30, freq + slide), phase = 0;
                for (int i = 0; i < count && start + i < Data.Length; i++)
                {
                    float t = (float)i / count;
                    float f = slide != 0 ? freq * Mathf.Pow(end / freq, t) : freq;
                    phase += f / Rate;
                    phase -= Mathf.Floor(phase);
                    float s = wave switch
                    {
                        Wave.Square => phase < .5f ? 1 : -1,
                        Wave.Sine => Mathf.Sin(phase * Mathf.PI * 2),
                        Wave.Triangle => 1 - 4 * Mathf.Abs(phase - .5f),
                        _ => phase * 2 - 1
                    };
                    Data[start + i] += s * volume * Mathf.Pow(.001f / volume, t) * Master;
                }

                return this;
            }

            public Track Noise(float duration, float volume, float freq, Filter filter = Filter.Band, float delay = 0)
            {
                int start = (int)(delay * Rate), count = (int)(duration * Rate);
                // RBJ biquad, Q = 1 like the WebAudio default.
                float w0 = 2 * Mathf.PI * Mathf.Min(freq, Rate * .45f) / Rate, alpha = Mathf.Sin(w0) / 2, cos = Mathf.Cos(w0);
                float b0, b1, b2, a0 = 1 + alpha, a1 = -2 * cos, a2 = 1 - alpha;
                switch (filter)
                {
                    case Filter.Low: b0 = (1 - cos) / 2; b1 = 1 - cos; b2 = b0; break;
                    case Filter.High: b0 = (1 + cos) / 2; b1 = -(1 + cos); b2 = b0; break;
                    default: b0 = alpha; b1 = 0; b2 = -alpha; break;
                }

                float x1 = 0, x2 = 0, y1 = 0, y2 = 0;
                var random = new System.Random(start * 31 + count);
                for (int i = 0; i < count && start + i < Data.Length; i++)
                {
                    float x0 = (float)random.NextDouble() * 2 - 1;
                    float y0 = (b0 * x0 + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2) / a0;
                    x2 = x1; x1 = x0; y2 = y1; y1 = y0;
                    float t = (float)i / count;
                    Data[start + i] += y0 * volume * Mathf.Pow(.001f / volume, t) * Master;
                }

                return this;
            }

            public AudioClip Clip(string name)
            {
                AudioClip clip = AudioClip.Create(name, Data.Length, 1, Rate, false);
                clip.SetData(Data, 0);
                return clip;
            }
        }

        private static void Render()
        {
            clips = new Dictionary<Sfx, AudioClip[]>();
            void One(Sfx id, Track t) => clips[id] = new[] { t.Clip(id.ToString()) };

            One(Sfx.Tap, new Track(.08f).Tone(660, .06f, Wave.Square, .06f));
            One(Sfx.Ok, new Track(.4f).Tone(523, .1f, Wave.Square, .08f).Tone(659, .1f, Wave.Square, .08f, 0, .09f).Tone(784, .18f, Wave.Square, .08f, 0, .18f));
            var perfect = new Track(.7f);
            int[] notes = { 523, 659, 784, 1046 };
            for (int i = 0; i < notes.Length; i++)
                perfect.Tone(notes[i], .16f, Wave.Square, .08f, 0, i * .08f);
            One(Sfx.Perfect, perfect.Tone(1318, .3f, Wave.Triangle, .06f, 0, .34f));
            One(Sfx.Bad, new Track(.32f).Tone(240, .3f, Wave.Saw, .07f, -90));
            One(Sfx.Peel, new Track(.08f).Noise(.07f, .12f, 3500));
            One(Sfx.Grate, new Track(.08f).Noise(.07f, .2f, 1700));
            var drips = new AudioClip[4];
            for (int i = 0; i < drips.Length; i++)
                drips[i] = new Track(.06f).Tone(900 + i * 140, .05f, Wave.Sine, .06f, -400).Clip("Drip" + i);
            clips[Sfx.Drip] = drips;
            One(Sfx.Stamp, new Track(.18f).Noise(.14f, .3f, 350, Filter.Low).Tone(110, .16f, Wave.Square, .1f, -40));
            One(Sfx.Tick, new Track(.04f).Tone(1250, .03f, Wave.Square, .04f));
            One(Sfx.Pour, new Track(.14f).Noise(.13f, .08f, 800, Filter.Low));
            One(Sfx.Salt, new Track(.07f).Noise(.06f, .12f, 6500, Filter.High));
            One(Sfx.Flip, new Track(.24f).Tone(300, .22f, Wave.Triangle, .1f, 500));
            One(Sfx.Sizzle, new Track(.2f).Noise(.18f, .05f, 5200, Filter.High));
            One(Sfx.Stir, new Track(.1f).Noise(.09f, .05f, 700));
            One(Sfx.Plop, new Track(.14f).Tone(180, .12f, Wave.Sine, .12f, -90));
        }
    }
}
