using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Martian.Audio
{
    public sealed class UnityAudioBackend : IAudioBackend
    {
        public const string BackendIdValue = "unity";

        private const string MasterVolumeKey = "audio.master";
        private const string MusicVolumeKey = "audio.music";
        private const string SfxVolumeKey = "audio.sfx";
        private const string UiVolumeKey = "audio.ui";

        private readonly GameObject _host;
        private readonly AudioCatalog _catalog;
        private readonly AudioSettingsData _settings;
        private readonly Dictionary<AudioBus, float> _busVolumes = new();
        private readonly Dictionary<string, float> _lastCuePlayTimes = new(StringComparer.Ordinal);
        private readonly List<VoiceState> _activeVoices = new();
        private readonly List<AudioSource> _pooledSources = new();
        private readonly HashSet<string> _loggedWarnings = new(StringComparer.Ordinal);

        private Driver _driver;
        private AudioSource _musicSourceA;
        private AudioSource _musicSourceB;
        private AudioSource _activeMusicSource;
        private AudioSource _inactiveMusicSource;
        private VoiceState _activeMusicVoice;
        private Coroutine _musicRoutine;
        private bool _paused;

        public UnityAudioBackend(GameObject host, AudioCatalog catalog, AudioSettingsData settings)
        {
            _host = host;
            _catalog = catalog;
            _settings = settings;

            Initialize();
        }

        public string BackendId => BackendIdValue;
        public bool IsAvailable => true;

        public AudioPlaybackHandle Play(AudioPlayRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CueId))
            {
                return AudioPlaybackHandle.Invalid;
            }

            if (!_catalog.TryGetCue(request.CueId, out var cue))
            {
                LogOnce($"missing-cue:{request.CueId}", $"[Martian.Audio] Cue '{request.CueId}' was not found in the catalog.");
                return AudioPlaybackHandle.Invalid;
            }

            if (!cue.TryPickClip(out var clip))
            {
                LogOnce($"missing-clip:{request.CueId}", $"[Martian.Audio] Cue '{request.CueId}' has no valid AudioClip.");
                return AudioPlaybackHandle.Invalid;
            }

            float now = Time.realtimeSinceStartup;
            if (cue.cooldownSeconds > 0f &&
                _lastCuePlayTimes.TryGetValue(request.CueId, out float lastPlayTime) &&
                now - lastPlayTime < cue.cooldownSeconds)
            {
                return AudioPlaybackHandle.Invalid;
            }

            _lastCuePlayTimes[request.CueId] = now;

            var bus = request.OverrideBus ?? cue.bus;
            var source = AcquireOneShotSource();
            ConfigureSource(source, clip, cue, request, bus);

            var voice = new VoiceState(source, bus, cue.baseVolume, ResolveRequestVolume(request), request.Loop || cue.loop);
            voice.Handle = new AudioPlaybackHandle(fadeSeconds => StopVoice(voice, fadeSeconds));
            _activeVoices.Add(voice);

            if (request.FollowTarget != null)
            {
                LogOnce("follow-target-ignored", "[Martian.Audio] FollowTarget is reserved for future 3D audio and is currently ignored.");
            }

            if (request.Position != Vector3.zero)
            {
                LogOnce("position-ignored", "[Martian.Audio] Position is reserved for future 3D audio and is currently ignored.");
            }

            PlayWithOptionalFadeIn(voice, ResolveRequestFade(request.FadeInSeconds));
            return voice.Handle;
        }

        public AudioPlaybackHandle PlayMusic(string cueId, float fadeSeconds = 0.25f)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                return AudioPlaybackHandle.Invalid;
            }

            if (!_catalog.TryGetCue(cueId, out var cue))
            {
                LogOnce($"missing-music-cue:{cueId}", $"[Martian.Audio] Music cue '{cueId}' was not found in the catalog.");
                return AudioPlaybackHandle.Invalid;
            }

            if (!cue.TryPickClip(out var clip))
            {
                LogOnce($"missing-music-clip:{cueId}", $"[Martian.Audio] Music cue '{cueId}' has no valid AudioClip.");
                return AudioPlaybackHandle.Invalid;
            }

            float resolvedFade = ResolveMusicFade(fadeSeconds);
            _inactiveMusicSource.clip = clip;
            _inactiveMusicSource.loop = cue.loop;
            _inactiveMusicSource.pitch = cue.ResolvePitch(1f);
            _inactiveMusicSource.volume = 0f;
            _inactiveMusicSource.spatialBlend = 0f;
            _inactiveMusicSource.Play();

            var nextVoice = new VoiceState(_inactiveMusicSource, AudioBus.Music, cue.baseVolume, 1f, true)
            {
                Handle = new AudioPlaybackHandle(requestFade => StopMusic(requestFade))
            };

            if (_musicRoutine != null)
            {
                _driver.StopManagedCoroutine(_musicRoutine);
                _musicRoutine = null;
            }

            if (_activeMusicSource == null || !_activeMusicSource.isPlaying || resolvedFade <= 0f)
            {
                if (_activeMusicVoice != null)
                {
                    _activeMusicVoice.Handle?.Invalidate();
                }

                if (_activeMusicSource != null)
                {
                    _activeMusicSource.Stop();
                    _activeMusicSource.volume = 0f;
                }

                _activeMusicVoice = nextVoice;
                SwapMusicSources();
                ApplyMusicVolume();
                return nextVoice.Handle;
            }

            _musicRoutine = _driver.RunManagedCoroutine(CrossfadeMusic(_activeMusicVoice, nextVoice, resolvedFade));
            return nextVoice.Handle;
        }

        public void StopMusic(float fadeSeconds = 0.25f)
        {
            if (_activeMusicSource == null)
            {
                return;
            }

            float resolvedFade = ResolveMusicFade(fadeSeconds);
            if (_musicRoutine != null)
            {
                _driver.StopManagedCoroutine(_musicRoutine);
                _musicRoutine = null;
            }

            if (resolvedFade <= 0f)
            {
                _activeMusicSource.Stop();
                _activeMusicSource.volume = 0f;
                _activeMusicVoice?.Handle?.Invalidate();
                _activeMusicVoice = null;
                return;
            }

            _musicRoutine = _driver.RunManagedCoroutine(FadeOutMusic(resolvedFade));
        }

        public void SetBusVolume(AudioBus bus, float normalized)
        {
            _busVolumes[bus] = Mathf.Clamp01(normalized);
            PlayerPrefs.SetFloat(GetVolumeKey(bus), _busVolumes[bus]);
            PlayerPrefs.Save();
            ApplyAllVolumes();
        }

        public float GetBusVolume(AudioBus bus)
        {
            return _busVolumes.TryGetValue(bus, out float value) ? value : 1f;
        }

        public void PauseAll(bool paused)
        {
            _paused = paused;

            foreach (var voice in _activeVoices)
            {
                if (voice.Source == null)
                {
                    continue;
                }

                if (paused)
                {
                    voice.Source.Pause();
                }
                else
                {
                    voice.Source.UnPause();
                }
            }

            if (_activeMusicSource != null)
            {
                if (paused)
                {
                    _activeMusicSource.Pause();
                }
                else
                {
                    _activeMusicSource.UnPause();
                }
            }

            if (_inactiveMusicSource != null && _inactiveMusicSource.isPlaying)
            {
                if (paused)
                {
                    _inactiveMusicSource.Pause();
                }
                else
                {
                    _inactiveMusicSource.UnPause();
                }
            }
        }

        public void Dispose()
        {
            if (_musicRoutine != null && _driver != null)
            {
                _driver.StopManagedCoroutine(_musicRoutine);
                _musicRoutine = null;
            }

            foreach (var voice in _activeVoices)
            {
                voice.Handle?.Invalidate();
                if (voice.Source != null)
                {
                    voice.Source.Stop();
                }
            }

            _activeVoices.Clear();
            _activeMusicVoice?.Handle?.Invalidate();
            _activeMusicVoice = null;
        }

        internal static string GetPlayerPrefsKey(AudioBus bus)
        {
            return GetVolumeKey(bus);
        }

        private void Initialize()
        {
            _driver = _host.GetComponent<Driver>();
            if (_driver == null)
            {
                _driver = _host.AddComponent<Driver>();
            }

            _driver.SetTickHandler(Tick);

            _busVolumes[AudioBus.Master] = PlayerPrefs.GetFloat(MasterVolumeKey, _settings.GetDefaultVolume(AudioBus.Master));
            _busVolumes[AudioBus.Music] = PlayerPrefs.GetFloat(MusicVolumeKey, _settings.GetDefaultVolume(AudioBus.Music));
            _busVolumes[AudioBus.Sfx] = PlayerPrefs.GetFloat(SfxVolumeKey, _settings.GetDefaultVolume(AudioBus.Sfx));
            _busVolumes[AudioBus.Ui] = PlayerPrefs.GetFloat(UiVolumeKey, _settings.GetDefaultVolume(AudioBus.Ui));

            _musicSourceA = CreateManagedSource("MusicA");
            _musicSourceB = CreateManagedSource("MusicB");
            _activeMusicSource = _musicSourceA;
            _inactiveMusicSource = _musicSourceB;

            int poolSize = Mathf.Max(1, _settings.oneShotPoolSize);
            for (int i = 0; i < poolSize; i++)
            {
                _pooledSources.Add(CreateManagedSource($"OneShot_{i:D2}"));
            }
        }

        private void Tick()
        {
            if (_paused)
            {
                return;
            }

            for (int i = _activeVoices.Count - 1; i >= 0; i--)
            {
                var voice = _activeVoices[i];
                if (voice.Source == null)
                {
                    _activeVoices.RemoveAt(i);
                    continue;
                }

                if (voice.IsLooping || voice.Source.isPlaying)
                {
                    continue;
                }

                ReleaseVoice(voice);
            }

            if (_activeMusicVoice != null &&
                _activeMusicVoice.Source != null &&
                !_activeMusicVoice.Source.loop &&
                !_activeMusicVoice.Source.isPlaying &&
                _musicRoutine == null)
            {
                _activeMusicVoice.Handle?.Invalidate();
                _activeMusicVoice = null;
            }
        }

        private AudioSource AcquireOneShotSource()
        {
            foreach (var source in _pooledSources)
            {
                if (source == null || source.isPlaying)
                {
                    continue;
                }

                return source;
            }

            var created = CreateManagedSource($"OneShot_{_pooledSources.Count:D2}");
            _pooledSources.Add(created);
            return created;
        }

        private AudioSource CreateManagedSource(string childName)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(_host.transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }

        private void ConfigureSource(AudioSource source, AudioClip clip, AudioCueDefinition cue, AudioPlayRequest request, AudioBus bus)
        {
            source.clip = clip;
            source.loop = request.Loop || cue.loop;
            source.pitch = cue.ResolvePitch(request.PitchScale);
            source.spatialBlend = 0f;
            source.volume = CalculateSourceVolume(bus, cue.baseVolume, ResolveRequestVolume(request));
        }

        private void PlayWithOptionalFadeIn(VoiceState voice, float fadeInSeconds)
        {
            if (voice == null || voice.Source == null)
            {
                return;
            }

            voice.TargetVolume = CalculateSourceVolume(voice.Bus, voice.CueVolume, voice.RequestVolume);

            if (fadeInSeconds <= 0f)
            {
                voice.Source.volume = voice.TargetVolume;
                voice.Source.Play();
                return;
            }

            voice.Source.volume = 0f;
            voice.Source.Play();
            voice.FadeRoutine = _driver.RunManagedCoroutine(FadeVoice(voice, voice.TargetVolume, fadeInSeconds));
        }

        private IEnumerator FadeVoice(VoiceState voice, float targetVolume, float duration)
        {
            float safeDuration = Mathf.Max(0.0001f, duration);
            float elapsed = 0f;
            float startVolume = voice.Source != null ? voice.Source.volume : 0f;

            while (elapsed < safeDuration && voice.Source != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                voice.Source.volume = Mathf.Lerp(startVolume, targetVolume, t);
                yield return null;
            }

            if (voice.Source != null)
            {
                voice.Source.volume = targetVolume;
            }

            voice.FadeRoutine = null;
        }

        private IEnumerator CrossfadeMusic(VoiceState currentVoice, VoiceState nextVoice, float duration)
        {
            float safeDuration = Mathf.Max(0.0001f, duration);
            float elapsed = 0f;
            float fromStart = currentVoice?.Source != null ? currentVoice.Source.volume : 0f;
            float toTarget = CalculateSourceVolume(AudioBus.Music, nextVoice.CueVolume, nextVoice.RequestVolume);

            _activeMusicVoice = nextVoice;
            SwapMusicSources();

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);

                if (currentVoice?.Source != null)
                {
                    currentVoice.Source.volume = Mathf.Lerp(fromStart, 0f, t);
                }

                if (nextVoice.Source != null)
                {
                    nextVoice.Source.volume = Mathf.Lerp(0f, toTarget, t);
                }

                yield return null;
            }

            if (currentVoice?.Source != null)
            {
                currentVoice.Source.Stop();
                currentVoice.Source.volume = 0f;
            }

            if (currentVoice != null)
            {
                currentVoice.Handle?.Invalidate();
            }

            if (nextVoice.Source != null)
            {
                nextVoice.Source.volume = toTarget;
            }

            _musicRoutine = null;
        }

        private IEnumerator FadeOutMusic(float duration)
        {
            float safeDuration = Mathf.Max(0.0001f, duration);
            float elapsed = 0f;
            float startVolume = _activeMusicSource != null ? _activeMusicSource.volume : 0f;

            while (elapsed < safeDuration && _activeMusicSource != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                _activeMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            if (_activeMusicSource != null)
            {
                _activeMusicSource.Stop();
                _activeMusicSource.volume = 0f;
            }

            _activeMusicVoice?.Handle?.Invalidate();
            _activeMusicVoice = null;
            _musicRoutine = null;
        }

        private void ApplyAllVolumes()
        {
            foreach (var voice in _activeVoices)
            {
                if (voice.Source == null)
                {
                    continue;
                }

                voice.TargetVolume = CalculateSourceVolume(voice.Bus, voice.CueVolume, voice.RequestVolume);
                voice.Source.volume = voice.TargetVolume;
            }

            ApplyMusicVolume();
        }

        private void ApplyMusicVolume()
        {
            if (_activeMusicVoice?.Source != null)
            {
                _activeMusicVoice.TargetVolume = CalculateSourceVolume(AudioBus.Music, _activeMusicVoice.CueVolume, _activeMusicVoice.RequestVolume);
                _activeMusicVoice.Source.volume = _activeMusicVoice.TargetVolume;
            }
        }

        private void StopVoice(VoiceState voice, float fadeSeconds)
        {
            if (voice == null || !_activeVoices.Contains(voice))
            {
                return;
            }

            float resolvedFade = ResolveRequestFade(fadeSeconds);
            if (resolvedFade <= 0f)
            {
                ReleaseVoice(voice);
                return;
            }

            if (voice.FadeRoutine != null)
            {
                _driver.StopManagedCoroutine(voice.FadeRoutine);
            }

            voice.FadeRoutine = _driver.RunManagedCoroutine(FadeOutAndRelease(voice, resolvedFade));
        }

        private IEnumerator FadeOutAndRelease(VoiceState voice, float duration)
        {
            float safeDuration = Mathf.Max(0.0001f, duration);
            float elapsed = 0f;
            float startVolume = voice.Source != null ? voice.Source.volume : 0f;

            while (elapsed < safeDuration && voice.Source != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                voice.Source.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            ReleaseVoice(voice);
        }

        private void ReleaseVoice(VoiceState voice)
        {
            if (voice == null)
            {
                return;
            }

            if (voice.FadeRoutine != null)
            {
                _driver.StopManagedCoroutine(voice.FadeRoutine);
                voice.FadeRoutine = null;
            }

            if (voice.Source != null)
            {
                voice.Source.Stop();
                voice.Source.clip = null;
                voice.Source.volume = 0f;
                voice.Source.loop = false;
                voice.Source.pitch = 1f;
            }

            voice.Handle?.Invalidate();
            _activeVoices.Remove(voice);
        }

        private void SwapMusicSources()
        {
            var previousActive = _activeMusicSource;
            _activeMusicSource = _inactiveMusicSource;
            _inactiveMusicSource = previousActive;
        }

        private float CalculateSourceVolume(AudioBus bus, float cueVolume, float requestVolume)
        {
            float master = GetBusVolume(AudioBus.Master);
            float busVolume = bus == AudioBus.Master ? 1f : GetBusVolume(bus);
            return Mathf.Clamp01(master * busVolume * Mathf.Clamp01(cueVolume) * Mathf.Max(0f, requestVolume));
        }

        private static float ResolveRequestVolume(AudioPlayRequest request)
        {
            return request == null ? 1f : Mathf.Max(0f, request.VolumeScale);
        }

        private float ResolveMusicFade(float fadeSeconds)
        {
            if (fadeSeconds > 0f)
            {
                return fadeSeconds;
            }

            return Mathf.Max(0f, _settings.musicFadeSeconds);
        }

        private static float ResolveRequestFade(float fadeSeconds)
        {
            return Mathf.Max(0f, fadeSeconds);
        }

        private void LogOnce(string key, string message)
        {
            if (!_loggedWarnings.Add(key))
            {
                return;
            }

            Debug.LogWarning(message);
        }

        private static string GetVolumeKey(AudioBus bus)
        {
            return bus switch
            {
                AudioBus.Master => MasterVolumeKey,
                AudioBus.Music => MusicVolumeKey,
                AudioBus.Sfx => SfxVolumeKey,
                AudioBus.Ui => UiVolumeKey,
                _ => MasterVolumeKey
            };
        }

        private sealed class VoiceState
        {
            public VoiceState(AudioSource source, AudioBus bus, float cueVolume, float requestVolume, bool isLooping)
            {
                Source = source;
                Bus = bus;
                CueVolume = cueVolume;
                RequestVolume = requestVolume;
                IsLooping = isLooping;
            }

            public AudioSource Source { get; }
            public AudioBus Bus { get; }
            public float CueVolume { get; }
            public float RequestVolume { get; }
            public bool IsLooping { get; }
            public float TargetVolume { get; set; }
            public Coroutine FadeRoutine { get; set; }
            public AudioPlaybackHandle Handle { get; set; }
        }

        private sealed class Driver : MonoBehaviour
        {
            private Action _tickHandler;

            public void SetTickHandler(Action tickHandler)
            {
                _tickHandler = tickHandler;
            }

            public Coroutine RunManagedCoroutine(IEnumerator routine)
            {
                return StartCoroutine(routine);
            }

            public void StopManagedCoroutine(Coroutine routine)
            {
                if (routine != null)
                {
                    StopCoroutine(routine);
                }
            }

            private void Update()
            {
                _tickHandler?.Invoke();
            }
        }
    }
}
