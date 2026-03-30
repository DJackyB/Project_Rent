using UnityEngine;

namespace Martian.Audio
{
    public sealed class AudioPlayRequest
    {
        public string CueId;
        public AudioBus? OverrideBus;
        public Vector3 Position;
        public Transform FollowTarget;
        public float VolumeScale = 1f;
        public float PitchScale = 1f;
        public bool Loop;
        public float FadeInSeconds;

        public static AudioPlayRequest Create(string cueId)
        {
            return new AudioPlayRequest
            {
                CueId = cueId
            };
        }
    }
}
