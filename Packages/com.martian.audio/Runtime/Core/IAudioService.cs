namespace Martian.Audio
{
    public interface IAudioService
    {
        bool IsAvailable { get; }

        AudioPlaybackHandle Play(AudioPlayRequest request);
        AudioPlaybackHandle PlayMusic(string cueId, float fadeSeconds = 0.25f);
        void StopMusic(float fadeSeconds = 0.25f);
        void SetBusVolume(AudioBus bus, float normalized);
        float GetBusVolume(AudioBus bus);
        void PauseAll(bool paused);
    }
}
