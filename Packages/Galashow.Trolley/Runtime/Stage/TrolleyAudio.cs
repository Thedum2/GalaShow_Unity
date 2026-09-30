using System.Collections.Generic;
using UnityEngine;

namespace Galashow.Trolley
{
    /// <summary>
    /// 트롤리 효과음 재생. 프로필의 클립을 쓰고, 비어 있으면 합성음으로 대신한다.
    /// </summary>
    public class TrolleyAudio : MonoBehaviour
    {
        public enum Sfx
        {
            Tick, TickUrgent, Heartbeat, Vote, HostReady, InputClosed, Snare, Slam, Whoosh,
            Lever, Screech, Impact, Boom, Cymbal, CountPop, Award, AllEliminated, Thunder, Horn
        }

        TrolleyFxProfile _profile;
        readonly List<AudioSource> _sources = new List<AudioSource>();
        AudioSource _engine;
        readonly Dictionary<Sfx, AudioClip> _synth = new Dictionary<Sfx, AudioClip>();
        int _next;

        public void Setup(TrolleyFxProfile profile)
        {
            _profile = profile;
            for (int i = 0; i < 8; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _sources.Add(source);
            }

            _engine = gameObject.AddComponent<AudioSource>();
            _engine.playOnAwake = false;
            _engine.loop = true;
            _engine.clip = profile.engine != null ? profile.engine : TrolleySynth.Engine();
        }

        public void Play(Sfx sfx, float volume = 1f, float pitch = 1f)
        {
            var clip = Clip(sfx);
            if (clip == null)
            {
                return;
            }

            var source = _sources[_next];
            _next = (_next + 1) % _sources.Count;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume * _profile.masterVolume);
        }

        public void SetEngine(bool on, float pitch = 1f)
        {
            if (_engine == null) return;
            _engine.pitch = pitch;
            _engine.volume = _profile.engineVolume * _profile.masterVolume;
            if (on && !_engine.isPlaying) _engine.Play();
            if (!on) _engine.Stop();
        }

        AudioClip Clip(Sfx sfx)
        {
            var p = _profile;
            AudioClip clip = null;
            switch (sfx)
            {
                case Sfx.Tick: clip = p.tick; break;
                case Sfx.TickUrgent: clip = p.tickUrgent; break;
                case Sfx.Heartbeat: clip = p.heartbeat; break;
                case Sfx.Vote: clip = p.vote; break;
                case Sfx.HostReady: clip = p.hostReady; break;
                case Sfx.InputClosed: clip = p.inputClosed; break;
                case Sfx.Snare: clip = p.snare; break;
                case Sfx.Slam: clip = p.slam; break;
                case Sfx.Whoosh: clip = p.whoosh; break;
                case Sfx.Lever: clip = p.lever; break;
                case Sfx.Screech: clip = p.screech; break;
                case Sfx.Impact: clip = p.impact; break;
                case Sfx.Boom: clip = p.boom; break;
                case Sfx.Cymbal: clip = p.cymbal; break;
                case Sfx.CountPop: clip = p.countPop; break;
                case Sfx.Award: clip = p.award; break;
                case Sfx.AllEliminated: clip = p.allEliminated; break;
                case Sfx.Thunder: clip = p.thunder; break;
                case Sfx.Horn: clip = p.horn; break;
            }

            if (clip != null)
            {
                return clip;
            }

            if (!_synth.TryGetValue(sfx, out clip))
            {
                clip = TrolleySynth.For(sfx);
                _synth[sfx] = clip;
            }
            return clip;
        }

        void OnDestroy()
        {
            foreach (var clip in _synth.Values)
            {
                if (clip != null) Destroy(clip);
            }
        }
    }

    /// <summary>
    /// 효과음 합성 (프로필 클립이 없을 때만 사용)
    /// </summary>
    public static class TrolleySynth
    {
        const int Rate = 44100;

        public static AudioClip For(TrolleyAudio.Sfx sfx)
        {
            switch (sfx)
            {
                case TrolleyAudio.Sfx.Tick: return Tone("tick", 0.05f, 1400f, 0f, 0.5f, 60f);
                case TrolleyAudio.Sfx.TickUrgent: return Tone("tickUrgent", 0.09f, 1900f, 0f, 0.7f, 40f);
                case TrolleyAudio.Sfx.Heartbeat: return Heartbeat();
                case TrolleyAudio.Sfx.Vote: return Tone("vote", 0.04f, 900f, 300f, 0.3f, 80f);
                case TrolleyAudio.Sfx.HostReady: return Tone("hostReady", 0.18f, 660f, 440f, 0.5f, 12f);
                case TrolleyAudio.Sfx.InputClosed: return Tone("inputClosed", 0.45f, 440f, -200f, 0.8f, 6f);
                case TrolleyAudio.Sfx.Snare: return Noise("snare", 0.12f, 0.6f, 30f, 0f);
                case TrolleyAudio.Sfx.Slam: return Tone("slam", 0.35f, 120f, -80f, 1f, 9f, 0.25f);
                case TrolleyAudio.Sfx.Whoosh: return Noise("whoosh", 0.5f, 0.5f, 0f, 1f);
                case TrolleyAudio.Sfx.Lever: return Tone("lever", 0.25f, 180f, -60f, 0.9f, 14f, 0.4f);
                case TrolleyAudio.Sfx.Screech: return Tone("screech", 0.7f, 2400f, -600f, 0.35f, 3f, 0.15f);
                case TrolleyAudio.Sfx.Impact: return Noise("impact", 0.8f, 1f, 5f, 0f);
                case TrolleyAudio.Sfx.Boom: return Tone("boom", 1.2f, 70f, -40f, 1f, 3f, 0.3f);
                case TrolleyAudio.Sfx.Cymbal: return Noise("cymbal", 1.5f, 0.6f, 2.5f, 0f);
                case TrolleyAudio.Sfx.CountPop: return Tone("pop", 0.06f, 1200f, 800f, 0.4f, 50f);
                case TrolleyAudio.Sfx.Award: return Arpeggio("award", new[] { 523f, 659f, 784f, 1047f });
                case TrolleyAudio.Sfx.AllEliminated: return Arpeggio("allEliminated", new[] { 392f, 330f, 262f });
                case TrolleyAudio.Sfx.Thunder: return Noise("thunder", 2f, 0.9f, 1.5f, 0f, lowPass: 0.02f);
                case TrolleyAudio.Sfx.Horn: return Horn();
                default: return null;
            }
        }

        public static AudioClip Engine()
        {
            int n = Rate;
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                data[i] = 0.25f * Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 55f * t)) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 8f * t));
            }
            return Make("engine", data);
        }

        /// <summary>
        /// 기차 경적: 두 음을 겹친 사각파, 약한 떨림
        /// </summary>
        static AudioClip Horn()
        {
            int n = Mathf.CeilToInt(1.1f * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, t * 20f) * Mathf.Min(1f, (1.1f - t) * 6f);
                float vib = 1f + 0.004f * Mathf.Sin(2f * Mathf.PI * 6f * t);
                float a = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 330f * vib * t));
                float b = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 415f * vib * t));
                data[i] = 0.16f * env * (a + b);
            }
            return Make("horn", data);
        }

        static AudioClip Tone(string name, float length, float freq, float sweep, float gain, float decay, float noise = 0f)
        {
            int n = Mathf.CeilToInt(length * Rate);
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                phase += 2f * Mathf.PI * (freq + sweep * t / length) / Rate;
                float env = Mathf.Exp(-decay * t) * Mathf.Min(1f, i / 80f);
                float s = Mathf.Sin(phase) + noise * ((float)rng.NextDouble() * 2f - 1f);
                data[i] = gain * env * s;
            }
            return Make(name, data);
        }

        static AudioClip Noise(string name, float length, float gain, float decay, float swell, float lowPass = 0.35f)
        {
            int n = Mathf.CeilToInt(length * Rate);
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            float y = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = swell > 0f ? Mathf.Sin(Mathf.PI * t / length) : Mathf.Exp(-decay * t);
                y += lowPass * (((float)rng.NextDouble() * 2f - 1f) - y);
                data[i] = gain * env * y * 2f;
            }
            return Make(name, data);
        }

        static AudioClip Heartbeat()
        {
            int n = Mathf.CeilToInt(0.6f * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float a = Beat(t, 0f) + 0.7f * Beat(t, 0.22f);
                data[i] = 0.9f * a * Mathf.Sin(2f * Mathf.PI * 50f * t);
            }
            return Make("heartbeat", data);
        }

        static float Beat(float t, float at) => t < at ? 0f : Mathf.Exp(-(t - at) * 18f);

        static AudioClip Arpeggio(string name, float[] notes)
        {
            float step = 0.11f;
            int n = Mathf.CeilToInt((step * notes.Length + 0.5f) * Rate);
            var data = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                int start = Mathf.CeilToInt(k * step * Rate);
                for (int i = start; i < n; i++)
                {
                    float t = (float)(i - start) / Rate;
                    data[i] += 0.3f * Mathf.Exp(-4f * t) * (Mathf.Sin(2f * Mathf.PI * notes[k] * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * notes[k] * t));
                }
            }
            return Make(name, data);
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create($"synth_{name}", data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
