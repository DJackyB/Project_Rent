using System;

namespace Martian.Audio
{
    public sealed class AudioPlaybackHandle
    {
        public static AudioPlaybackHandle Invalid { get; } = new();

        private Action<float> _stopAction;

        public bool IsValid => _stopAction != null;

        public AudioPlaybackHandle()
        {
        }

        internal AudioPlaybackHandle(Action<float> stopAction)
        {
            _stopAction = stopAction;
        }

        public void Stop(float fadeSeconds = 0f)
        {
            var stopAction = _stopAction;
            if (stopAction == null)
            {
                return;
            }

            _stopAction = null;
            stopAction.Invoke(fadeSeconds);
        }

        internal void Invalidate()
        {
            _stopAction = null;
        }
    }
}
