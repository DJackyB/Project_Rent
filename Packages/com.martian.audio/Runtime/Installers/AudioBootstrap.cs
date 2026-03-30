using UnityEngine;

namespace Martian.Audio
{
    public sealed class AudioBootstrap : MonoBehaviour
    {
        [SerializeField] private AudioCatalog _catalog;
        [SerializeField] private AudioSettingsData _settings;
        [SerializeField] private bool _installOnAwake = true;
        [SerializeField] private bool _uninstallOnDestroy;

        public AudioCatalog Catalog => _catalog;
        public AudioSettingsData Settings => _settings;

        private void Awake()
        {
            if (_installOnAwake)
            {
                AudioRuntimeInstaller.Install(_catalog, _settings);
            }
        }

        private void OnDestroy()
        {
            if (_uninstallOnDestroy)
            {
                AudioRuntimeInstaller.Uninstall();
            }
        }
    }
}
