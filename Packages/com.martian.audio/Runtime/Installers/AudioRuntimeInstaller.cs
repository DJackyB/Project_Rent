using System;
using System.Collections.Generic;
using UnityEngine;

namespace Martian.Audio
{
    public static class AudioRuntimeInstaller
    {
        internal const string HostName = "__MartianAudioRuntime";

        private static GameObject _host;
        private static IAudioBackend _backend;

        public static bool IsInstalled => _backend != null;

        public static IAudioService Install(AudioCatalog catalog, AudioSettingsData settings)
        {
            if (catalog == null || settings == null)
            {
                Debug.LogWarning("[Martian.Audio] Install skipped because catalog or settings are null.");
                Uninstall();
                return AudioServices.Current;
            }

            Uninstall();

            IReadOnlyList<IAudioBackendFactory> factories = AudioBackendRegistry.GetFactoriesInSelectionOrder(settings.preferredBackendId);
            foreach (var factory in factories)
            {
                if (factory == null || !factory.IsAvailable)
                {
                    continue;
                }

                var host = CreateHost();
                try
                {
                    var backend = factory.Create(host, catalog, settings);
                    if (backend == null || !backend.IsAvailable)
                    {
                        throw new InvalidOperationException($"Factory '{factory.BackendId}' did not return an available backend.");
                    }

                    _host = host;
                    _backend = backend;
                    AudioServices.SetCurrent(backend);
                    return backend;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Martian.Audio] Failed to install backend '{factory.BackendId}'. Falling back. {ex.Message}");
                    SafeDisposeHost(host);
                }
            }

            AudioServices.Reset();
            return AudioServices.Current;
        }

        public static void Uninstall()
        {
            if (_backend != null)
            {
                try
                {
                    _backend.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Martian.Audio] Backend dispose threw an exception. {ex.Message}");
                }
            }

            _backend = null;
            SafeDisposeHost(_host);
            _host = null;
            AudioServices.Reset();
        }

        internal static void ResetForTests()
        {
            Uninstall();
            AudioBackendRegistry.ResetForTests();
        }

        private static GameObject CreateHost()
        {
            var host = new GameObject(HostName);
            host.hideFlags = HideFlags.HideInHierarchy;
            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(host);
            }

            return host;
        }

        private static void SafeDisposeHost(GameObject host)
        {
            if (host == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(host);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
