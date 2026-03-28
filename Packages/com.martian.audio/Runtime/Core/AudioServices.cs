namespace Martian.Audio
{
    public static class AudioServices
    {
        private static IAudioService _current;

        public static IAudioService Current => _current ??= NoOpAudioService.Instance;

        public static void Reset()
        {
            _current = NoOpAudioService.Instance;
        }

        internal static void SetCurrent(IAudioService service)
        {
            _current = service ?? NoOpAudioService.Instance;
        }
    }
}
