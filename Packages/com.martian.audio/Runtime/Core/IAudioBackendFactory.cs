using UnityEngine;

namespace Martian.Audio
{
    public interface IAudioBackendFactory
    {
        string BackendId { get; }
        int Priority { get; }
        bool IsAvailable { get; }

        IAudioBackend Create(GameObject host, AudioCatalog catalog, AudioSettingsData settings);
    }
}
