namespace Martian.Audio
{
    public sealed class NoOpAudioService : IAudioService
    {
        public static readonly NoOpAudioService Instance = new();

        private NoOpAudioService()
        {
        }

        public bool IsAvailable => false;

        public AudioPlaybackHandle Play(AudioPlayRequest request)
        {
            return AudioPlaybackHandle.Invalid;
        }

        public AudioPlaybackHandle PlayMusic(string cueId, float fadeSeconds = 0.25f)
        {
            return AudioPlaybackHandle.Invalid;
        }

        public void StopMusic(float fadeSeconds = 0.25f)
        {
        }

        public void SetBusVolume(AudioBus bus, float normalized)
        {
        }

        public float GetBusVolume(AudioBus bus)
        {
            return 1f;
        }

        public void PauseAll(bool paused)
        {
        }
    }
}
