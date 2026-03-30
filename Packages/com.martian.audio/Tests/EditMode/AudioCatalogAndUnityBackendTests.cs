using NUnit.Framework;
using UnityEngine;

namespace Martian.Audio.Tests
{
    public class AudioCatalogAndUnityBackendTests
    {
        [SetUp]
        public void SetUp()
        {
            AudioRuntimeInstaller.ResetForTests();
            PlayerPrefs.DeleteKey(UnityAudioBackend.GetPlayerPrefsKey(AudioBus.Master));
            PlayerPrefs.DeleteKey(UnityAudioBackend.GetPlayerPrefsKey(AudioBus.Music));
            PlayerPrefs.DeleteKey(UnityAudioBackend.GetPlayerPrefsKey(AudioBus.Sfx));
            PlayerPrefs.DeleteKey(UnityAudioBackend.GetPlayerPrefsKey(AudioBus.Ui));
        }

        [TearDown]
        public void TearDown()
        {
            AudioRuntimeInstaller.ResetForTests();
            PlayerPrefs.DeleteKey(UnityAudioBackend.GetPlayerPrefsKey(AudioBus.Master));
            PlayerPrefs.DeleteKey(UnityAudioBackend.GetPlayerPrefsKey(AudioBus.Music));
            PlayerPrefs.DeleteKey(UnityAudioBackend.GetPlayerPrefsKey(AudioBus.Sfx));
            PlayerPrefs.DeleteKey(UnityAudioBackend.GetPlayerPrefsKey(AudioBus.Ui));
        }

        [Test]
        public void Catalog_TryGetCue_ReturnsExpectedDefinition()
        {
            var catalog = CreateCatalog();

            bool found = catalog.TryGetCue("sfx.confirm", out var cue);

            Assert.IsTrue(found);
            Assert.NotNull(cue);
            Assert.AreEqual(AudioBus.Sfx, cue.bus);
        }

        [Test]
        public void UnityBackend_Play_ReturnsInvalidForMissingCue()
        {
            var backend = new UnityAudioBackend(new GameObject("Host"), CreateCatalog(), CreateSettings());

            var handle = backend.Play(AudioPlayRequest.Create("missing"));

            Assert.IsFalse(handle.IsValid);
            backend.Dispose();
            Object.DestroyImmediate(GameObject.Find("Host"));
        }

        [Test]
        public void UnityBackend_Play_RespectsCueCooldown()
        {
            var backend = new UnityAudioBackend(new GameObject("Host"), CreateCatalog(), CreateSettings());

            var first = backend.Play(AudioPlayRequest.Create("sfx.confirm"));
            var second = backend.Play(AudioPlayRequest.Create("sfx.confirm"));

            Assert.IsTrue(first.IsValid);
            Assert.IsFalse(second.IsValid);
            backend.Dispose();
            Object.DestroyImmediate(GameObject.Find("Host"));
        }

        [Test]
        public void UnityBackend_SetBusVolume_PersistsAcrossInstallations()
        {
            AudioRuntimeInstaller.Install(CreateCatalog(), CreateSettings());
            AudioServices.Current.SetBusVolume(AudioBus.Sfx, 0.42f);
            AudioRuntimeInstaller.Uninstall();

            AudioRuntimeInstaller.Install(CreateCatalog(), CreateSettings());

            Assert.AreEqual(0.42f, AudioServices.Current.GetBusVolume(AudioBus.Sfx), 0.001f);
        }

        private static AudioCatalog CreateCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            catalog.Cues.Add(new AudioCueDefinition
            {
                id = "sfx.confirm",
                bus = AudioBus.Sfx,
                clips = new[] { AudioClip.Create("confirm", 128, 1, 22050, false) },
                cooldownSeconds = 0.5f
            });
            catalog.Cues.Add(new AudioCueDefinition
            {
                id = "ui.click",
                bus = AudioBus.Ui,
                clips = new AudioClip[] { null }
            });
            catalog.MarkLookupDirty();
            return catalog;
        }

        private static AudioSettingsData CreateSettings()
        {
            var settings = ScriptableObject.CreateInstance<AudioSettingsData>();
            settings.preferredBackendId = "unity";
            settings.oneShotPoolSize = 2;
            settings.defaultSfxVolume = 0.8f;
            return settings;
        }
    }
}
