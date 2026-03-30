using NUnit.Framework;
using UnityEngine;

namespace Martian.Audio.Tests
{
    public class AudioRuntimeInstallerTests
    {
        [SetUp]
        public void SetUp()
        {
            AudioRuntimeInstaller.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            AudioRuntimeInstaller.ResetForTests();
        }

        [Test]
        public void Install_PicksPreferredBackend_WhenAvailable()
        {
            var factory = new FakeBackendFactory("preferred", true, 100);
            AudioBackendRegistry.Register(factory);

            var result = AudioRuntimeInstaller.Install(CreateCatalog(), CreateSettings("preferred"));

            Assert.IsInstanceOf<FakeBackend>(result);
            Assert.AreEqual(1, factory.CreateCount);
            Assert.IsTrue(AudioRuntimeInstaller.IsInstalled);
        }

        [Test]
        public void Install_FallsBackToUnityBackend_WhenPreferredBackendIsMissing()
        {
            var result = AudioRuntimeInstaller.Install(CreateCatalog(), CreateSettings("missing"));

            Assert.IsInstanceOf<UnityAudioBackend>(result);
            Assert.IsTrue(AudioRuntimeInstaller.IsInstalled);
        }

        [Test]
        public void Install_ReplacesExistingBackend_AndKeepsSingleHost()
        {
            var factory = new FakeBackendFactory("preferred", true, 100);
            AudioBackendRegistry.Register(factory);

            AudioRuntimeInstaller.Install(CreateCatalog(), CreateSettings("preferred"));
            var firstBackend = factory.LastCreatedBackend;

            AudioRuntimeInstaller.Install(CreateCatalog(), CreateSettings("preferred"));

            Assert.NotNull(firstBackend);
            Assert.AreEqual(1, firstBackend.DisposeCount);

            var hosts = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int hostCount = 0;
            foreach (var host in hosts)
            {
                if (host != null && host.name == AudioRuntimeInstaller.HostName)
                {
                    hostCount++;
                }
            }

            Assert.AreEqual(1, hostCount);
        }

        [Test]
        public void PlayMusic_AndStopMusic_ForwardFadeValuesToBackend()
        {
            var factory = new FakeBackendFactory("preferred", true, 100);
            AudioBackendRegistry.Register(factory);
            AudioRuntimeInstaller.Install(CreateCatalog(), CreateSettings("preferred"));

            var handle = AudioServices.Current.PlayMusic("bgm.main", 0.5f);
            AudioServices.Current.StopMusic(0.8f);

            Assert.IsTrue(handle.IsValid);
            Assert.NotNull(factory.LastCreatedBackend);
            Assert.AreEqual("bgm.main", factory.LastCreatedBackend.LastMusicCueId);
            Assert.AreEqual(0.5f, factory.LastCreatedBackend.LastPlayMusicFade);
            Assert.AreEqual(0.8f, factory.LastCreatedBackend.LastStopMusicFade);
        }

        [Test]
        public void Install_AfterCustomFactoryIsRemoved_FallsBackToUnityBackend()
        {
            var factory = new FakeBackendFactory("preferred", true, 100);
            AudioBackendRegistry.Register(factory);
            AudioRuntimeInstaller.Install(CreateCatalog(), CreateSettings("preferred"));

            AudioBackendRegistry.Unregister(factory);
            var result = AudioRuntimeInstaller.Install(CreateCatalog(), CreateSettings("preferred"));

            Assert.IsInstanceOf<UnityAudioBackend>(result);
        }

        private static AudioCatalog CreateCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            catalog.Cues.Add(new AudioCueDefinition
            {
                id = "bgm.main",
                bus = AudioBus.Music,
                clips = new[] { AudioClip.Create("music", 256, 1, 44100, false) },
                loop = true
            });
            catalog.MarkLookupDirty();
            return catalog;
        }

        private static AudioSettingsData CreateSettings(string preferredBackendId)
        {
            var settings = ScriptableObject.CreateInstance<AudioSettingsData>();
            settings.preferredBackendId = preferredBackendId;
            settings.musicFadeSeconds = 0.25f;
            settings.oneShotPoolSize = 2;
            return settings;
        }

        private sealed class FakeBackendFactory : IAudioBackendFactory
        {
            public FakeBackendFactory(string backendId, bool isAvailable, int priority)
            {
                BackendId = backendId;
                IsAvailable = isAvailable;
                Priority = priority;
            }

            public string BackendId { get; }
            public int Priority { get; }
            public bool IsAvailable { get; }
            public int CreateCount { get; private set; }
            public FakeBackend LastCreatedBackend { get; private set; }

            public IAudioBackend Create(GameObject host, AudioCatalog catalog, AudioSettingsData settings)
            {
                CreateCount++;
                LastCreatedBackend = new FakeBackend(BackendId);
                return LastCreatedBackend;
            }
        }

        private sealed class FakeBackend : IAudioBackend
        {
            public FakeBackend(string backendId)
            {
                BackendId = backendId;
            }

            public string BackendId { get; }
            public bool IsAvailable => true;
            public int DisposeCount { get; private set; }
            public float LastPlayMusicFade { get; private set; }
            public float LastStopMusicFade { get; private set; }
            public string LastMusicCueId { get; private set; }

            public AudioPlaybackHandle Play(AudioPlayRequest request)
            {
                return new AudioPlaybackHandle();
            }

            public AudioPlaybackHandle PlayMusic(string cueId, float fadeSeconds = 0.25f)
            {
                LastMusicCueId = cueId;
                LastPlayMusicFade = fadeSeconds;
                return new AudioPlaybackHandle(_ => { });
            }

            public void StopMusic(float fadeSeconds = 0.25f)
            {
                LastStopMusicFade = fadeSeconds;
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

            public void Dispose()
            {
                DisposeCount++;
            }
        }
    }
}
