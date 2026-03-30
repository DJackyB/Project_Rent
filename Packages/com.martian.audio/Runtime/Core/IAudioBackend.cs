using System;

namespace Martian.Audio
{
    public interface IAudioBackend : IAudioService, IDisposable
    {
        string BackendId { get; }
    }
}
