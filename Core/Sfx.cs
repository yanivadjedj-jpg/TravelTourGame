using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace TravelTour.Core
{
    // Effets sonores synthétisés en code (aucun fichier audio requis) — tons rétro
    // façon jeu 8-bit, générés une fois puis mis en cache.
    public static class Sfx
    {
        const int SampleRate = 44100;
        static readonly Dictionary<string, SoundEffect> _cache = new();
        static bool _enabled = true;
        static bool _initFailed = false;

        public static void SetEnabled(bool enabled) => _enabled = enabled;

        static SoundEffect? GetTone(string key, float freq, float durationSec, bool square = true, float volume = 0.25f)
        {
            if (_initFailed) return null;
            if (_cache.TryGetValue(key, out var cached)) return cached;
            try
            {
                int samples = (int)(SampleRate * durationSec);
                var buffer = new byte[samples * 2];
                for (int i = 0; i < samples; i++)
                {
                    float t = i / (float)SampleRate;
                    float phase = 2f * MathF.PI * freq * t;
                    float raw = square ? MathF.Sign(MathF.Sin(phase)) : MathF.Sin(phase);
                    float envelope = 1f - i / (float)samples; // fade-out simple
                    short sample = (short)(raw * short.MaxValue * volume * envelope);
                    buffer[i * 2]     = (byte)(sample & 0xFF);
                    buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
                }
                var fx = new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
                _cache[key] = fx;
                return fx;
            }
            catch
            {
                // Pas de périphérique audio disponible (ex: environnement headless) — on abandonne silencieusement.
                _initFailed = true;
                return null;
            }
        }

        static void Play(string key, float freq, float durationSec, bool square = true, float volume = 0.25f)
        {
            if (!_enabled) return;
            try { GetTone(key, freq, durationSec, square, volume)?.Play(); }
            catch { /* périphérique audio indisponible — ignorer */ }
        }

        public static void Hit()       => Play("hit",      160f, 0.07f, true,  0.22f);
        public static void Attack()    => Play("attack",   320f, 0.05f, true,  0.15f);
        public static void Gold()      => Play("gold",     880f, 0.09f, false, 0.20f);
        public static void LevelUp()   => Play("levelup",  660f, 0.35f, false, 0.28f);
        public static void Victory()   => Play("victory",  523f, 0.5f,  false, 0.30f);
        public static void FishCatch() => Play("fishcatch",740f, 0.22f, false, 0.25f);
        public static void FishMiss()  => Play("fishmiss", 140f, 0.20f, true,  0.18f);
        public static void QuestDone() => Play("questdone",784f, 0.30f, false, 0.28f);
        public static void Buy()       => Play("buy",      500f, 0.12f, false, 0.20f);
        public static void Dock()      => Play("dock",     220f, 0.18f, true,  0.18f);
    }
}
