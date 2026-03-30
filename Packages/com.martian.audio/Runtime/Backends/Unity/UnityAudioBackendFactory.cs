using UnityEngine;

namespace Martian.Audio
{
    public sealed class UnityAudioBackendFactory : IAudioBackendFactory
    {
        public string BackendId => UnityAudioBackend.BackendIdValue;
        public int Priority => 0;
        public bool IsAvailable => true;

        public IAudioBackend Create(GameObject host, AudioCatalog catalog, AudioSettingsData settings)
        {
            return new UnityAudioBackend(host, catalog, settings);
        }
    }
}
