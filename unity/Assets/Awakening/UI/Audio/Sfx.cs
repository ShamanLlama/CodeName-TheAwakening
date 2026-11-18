using System;
using System.Collections.Generic;
using UnityEngine;

namespace Awakening.UI
{
    /// <summary>Sound effects synthesised at startup, so the game ships without audio files. Port of godot/ui/sfx.gd.</summary>
    public sealed class Sfx : MonoBehaviour
    {
        const int Rate = 22050;
        const float Tau = Mathf.PI * 2;

        public float Volume = 0.4f;
        public bool Muted;

        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        readonly List<AudioSource> _players = new List<AudioSource>();

        void Awake()
        {
            _clips["click"] = Render("click", 0.05f, t => Mathf.Sin(Tau * 900f * t) * Mathf.Exp(-t * 90f) * 0.35f);
            _clips["act"] = Render("act", 0.32f, t =>
                (Mathf.Sin(Tau * 110f * t) * 0.6f + Mathf.Sin(Tau * 165f * t + Mathf.Sin(Tau * 3f * t)) * 0.3f) * Mathf.Exp(-t * 11f) * Mathf.Min(1f, t * 200f));
            _clips["refuse"] = Render("refuse", 0.16f, t => Mathf.Sin(Tau * (190f - t * 300f) * t) * Mathf.Exp(-t * 22f) * 0.4f);
            _clips["threat"] = Render("threat", 0.7f, t =>
            {
                float tone = t < 0.28f ? 98f : 87f;
                return Mathf.Sin(Tau * tone * t) * 0.55f * Mathf.Exp(-Mathf.Repeat(t, 0.28f) * 7f) * Mathf.Min(1f, t * 100f);
            });
            _clips["omen"] = Render("omen", 1.1f, t =>
                (Mathf.Sin(Tau * 523f * t) * 0.35f + Mathf.Sin(Tau * 784f * t) * 0.22f + Mathf.Sin(Tau * 1046.5f * t * 1.003f) * 0.12f) * Mathf.Exp(-t * 3.2f) * Mathf.Min(1f, t * 80f));
            _clips["site"] = Render("site", 1.4f, t =>
                (Mathf.Sin(Tau * 65.4f * t) * 0.5f + Mathf.Sin(Tau * 98f * t) * 0.35f + Mathf.Sin(Tau * 130.8f * t) * 0.2f) * Mathf.Exp(-t * 2.2f) * Mathf.Min(1f, t * 30f));
            _clips["end"] = Render("end", 2.6f, t =>
                (Mathf.Sin(Tau * 55f * t) * 0.6f + Mathf.Sin(Tau * 82.4f * t * (1f + 0.002f * Mathf.Sin(Tau * 0.7f * t))) * 0.4f) * Mathf.Exp(-t * 1.1f) * Mathf.Min(1f, t * 8f));
            for (int i = 0; i < 4; i++)
            {
                var player = gameObject.AddComponent<AudioSource>();
                player.playOnAwake = false;
                _players.Add(player);
            }
        }

        public void Play(string sound)
        {
            if (Muted || Volume <= 0 || !_clips.TryGetValue(sound, out AudioClip clip)) return;
            foreach (AudioSource player in _players)
            {
                if (player.isPlaying) continue;
                player.volume = Volume;
                player.PlayOneShot(clip);
                return;
            }
            _players[0].Stop();
            _players[0].volume = Volume;
            _players[0].PlayOneShot(clip);
        }

        static AudioClip Render(string name, float seconds, Func<float, float> wave)
        {
            int count = Mathf.CeilToInt(seconds * Rate);
            var data = new float[count];
            for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
            AudioClip clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
