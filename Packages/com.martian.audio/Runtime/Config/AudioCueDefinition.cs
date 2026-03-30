using System;
using UnityEngine;

namespace Martian.Audio
{
    [Serializable]
    public sealed class AudioCueDefinition
    {
        public string id;
        public AudioBus bus = AudioBus.Sfx;
        public AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float baseVolume = 1f;
        [Min(0f)] public float pitchMin = 1f;
        [Min(0f)] public float pitchMax = 1f;
        [Min(0f)] public float cooldownSeconds;
        public bool loop;

        internal bool TryPickClip(out AudioClip clip)
        {
            clip = null;
            if (clips == null || clips.Length == 0)
            {
                return false;
            }

            int startIndex = clips.Length == 1 ? 0 : UnityEngine.Random.Range(0, clips.Length);
            for (int offset = 0; offset < clips.Length; offset++)
            {
                var candidate = clips[(startIndex + offset) % clips.Length];
                if (candidate == null)
                {
                    continue;
                }

                clip = candidate;
                return true;
            }

            return false;
        }

        internal float ResolvePitch(float requestPitchScale)
        {
            float min = Mathf.Max(0.01f, pitchMin <= 0f ? 1f : pitchMin);
            float max = Mathf.Max(min, pitchMax <= 0f ? min : pitchMax);
            return UnityEngine.Random.Range(min, max) * Mathf.Max(0.01f, requestPitchScale <= 0f ? 1f : requestPitchScale);
        }
    }
}
