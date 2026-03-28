using UnityEngine;

namespace Martian.Audio
{
    [CreateAssetMenu(fileName = "AudioSettings", menuName = "Martian/Audio/Audio Settings")]
    public sealed class AudioSettingsData : ScriptableObject
    {
        public string preferredBackendId = "unity";
        [Min(0f)] public float musicFadeSeconds = 0.25f;
        [Min(1)] public int musicSourceCount = 2;
        [Min(1)] public int oneShotPoolSize = 12;
        [Range(0f, 1f)] public float defaultMasterVolume = 1f;
        [Range(0f, 1f)] public float defaultMusicVolume = 1f;
        [Range(0f, 1f)] public float defaultSfxVolume = 1f;
        [Range(0f, 1f)] public float defaultUiVolume = 1f;

        public float GetDefaultVolume(AudioBus bus)
        {
            return bus switch
            {
                AudioBus.Master => defaultMasterVolume,
                AudioBus.Music => defaultMusicVolume,
                AudioBus.Sfx => defaultSfxVolume,
                AudioBus.Ui => defaultUiVolume,
                _ => 1f
            };
        }
    }
}
