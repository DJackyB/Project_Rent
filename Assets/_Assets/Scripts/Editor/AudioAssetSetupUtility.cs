using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaoZuPo.Integration;
using Martian.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BaoZuPo.Editor
{
    /// <summary>
    /// Creates the baseline Martian audio assets in a target project folder.
    /// Uses the selected Project folder when possible, otherwise falls back to the default audio data path.
    /// </summary>
    public static class AudioAssetSetupUtility
    {
        private const string DefaultFolder = "Assets/_Assets/Data/Audio";
        private const string DefaultAudioRoot = "Assets/_Assets/Audio";
        private const string CatalogAssetName = "AudioCatalog.asset";
        private const string SettingsAssetName = "AudioSettings.asset";

        [MenuItem("Tools/BaoZuPo/Audio/Generate Audio Assets")]
        public static void GenerateAudioAssets()
        {
            string targetFolder = ResolveTargetFolder();
            EnsureAudioAssets(targetFolder);
        }

        [MenuItem("Tools/BaoZuPo/Audio/Setup Active Scene Audio")]
        public static void SetupActiveSceneAudio()
        {
            string targetFolder = ResolveTargetFolder();
            SetupActiveSceneAudio(targetFolder);
        }

        [MenuItem("Tools/BaoZuPo/Audio/Assign Clips By Cue Id")]
        public static void AssignClipsByCueId()
        {
            string targetFolder = ResolveTargetFolder();
            AssignClipsByCueId(targetFolder, DefaultAudioRoot);
        }

        internal static void EnsureAudioAssets(string folderPath)
        {
            string normalizedFolder = NormalizeFolderPath(folderPath);
            CreateFolderRecursive(normalizedFolder);

            string catalogPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(normalizedFolder, CatalogAssetName).Replace("\\", "/"));
            string settingsPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(normalizedFolder, SettingsAssetName).Replace("\\", "/"));

            var catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            catalog.Cues.Clear();
            foreach (var cue in CreateDefaultCueDefinitions())
            {
                catalog.Cues.Add(cue);
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.CreateAsset(catalog, catalogPath);

            var settings = ScriptableObject.CreateInstance<AudioSettingsData>();
            settings.preferredBackendId = "unity";
            settings.musicFadeSeconds = 0.5f;
            settings.musicSourceCount = 2;
            settings.oneShotPoolSize = 16;
            settings.defaultMasterVolume = 1f;
            settings.defaultMusicVolume = 0.85f;
            settings.defaultSfxVolume = 1f;
            settings.defaultUiVolume = 1f;
            EditorUtility.SetDirty(settings);
            AssetDatabase.CreateAsset(settings, settingsPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AudioAssetSetupUtility] Created audio assets:\n- {catalogPath}\n- {settingsPath}");
            EditorUtility.FocusProjectWindow();
            Selection.objects = new Object[]
            {
                AssetDatabase.LoadAssetAtPath<Object>(catalogPath),
                AssetDatabase.LoadAssetAtPath<Object>(settingsPath)
            };
        }

        internal static void SetupActiveSceneAudio(string folderPath)
        {
            string normalizedFolder = ResolveAssetFolder(folderPath);
            string catalogPath = Path.Combine(normalizedFolder, CatalogAssetName).Replace("\\", "/");
            string settingsPath = Path.Combine(normalizedFolder, SettingsAssetName).Replace("\\", "/");

            var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(catalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<AudioSettingsData>(settingsPath);
            if (catalog == null || settings == null)
            {
                Debug.LogError($"[AudioAssetSetupUtility] Missing audio assets in '{normalizedFolder}'. Generate them first.");
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                Debug.LogError("[AudioAssetSetupUtility] No active loaded scene to configure.");
                return;
            }

            GameObject audioRoot = GameObject.Find("[Audio]");
            if (audioRoot == null)
            {
                audioRoot = new GameObject("[Audio]");
                SceneManager.MoveGameObjectToScene(audioRoot, activeScene);
            }

            var bootstrap = audioRoot.GetComponent<AudioBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = audioRoot.AddComponent<AudioBootstrap>();
            }

            var bridge = audioRoot.GetComponent<AudioEventBridge>();
            if (bridge == null)
            {
                bridge = audioRoot.AddComponent<AudioEventBridge>();
            }

            var bootstrapSerialized = new SerializedObject(bootstrap);
            bootstrapSerialized.FindProperty("_catalog").objectReferenceValue = catalog;
            bootstrapSerialized.FindProperty("_settings").objectReferenceValue = settings;
            bootstrapSerialized.FindProperty("_installOnAwake").boolValue = true;
            bootstrapSerialized.FindProperty("_uninstallOnDestroy").boolValue = false;
            bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(bootstrap);
            EditorUtility.SetDirty(bridge);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log($"[AudioAssetSetupUtility] Scene '{activeScene.name}' is configured with [Audio] bootstrap.");
            Selection.activeGameObject = audioRoot;
        }

        internal static void AssignClipsByCueId(string folderPath, string audioRootFolder)
        {
            string normalizedFolder = ResolveAssetFolder(folderPath);
            string catalogPath = Path.Combine(normalizedFolder, CatalogAssetName).Replace("\\", "/");
            var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(catalogPath);
            if (catalog == null)
            {
                Debug.LogError($"[AudioAssetSetupUtility] Missing catalog asset at '{catalogPath}'.");
                return;
            }

            string normalizedAudioRoot = NormalizeAudioRoot(audioRootFolder);
            string[] clipGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { normalizedAudioRoot });
            var clipsByBaseName = clipGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => new
                {
                    Path = path,
                    BaseName = Path.GetFileNameWithoutExtension(path),
                    Clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path)
                })
                .Where(item => item.Clip != null)
                .GroupBy(item => item.BaseName)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Clip).ToArray());

            int assignedCount = 0;
            foreach (var cue in catalog.Cues)
            {
                if (cue == null || string.IsNullOrWhiteSpace(cue.id))
                {
                    continue;
                }

                if (clipsByBaseName.TryGetValue(cue.id, out AudioClip[] matchedClips))
                {
                    cue.clips = matchedClips;
                    assignedCount++;
                }
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AudioAssetSetupUtility] Assigned clips for {assignedCount} cue(s) from '{normalizedAudioRoot}'.");
            Selection.activeObject = catalog;
        }

        private static string ResolveTargetFolder()
        {
            Object activeObject = Selection.activeObject;
            if (activeObject == null)
            {
                return DefaultFolder;
            }

            string selectedPath = AssetDatabase.GetAssetPath(activeObject);
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return DefaultFolder;
            }

            if (AssetDatabase.IsValidFolder(selectedPath))
            {
                return selectedPath;
            }

            string folder = Path.GetDirectoryName(selectedPath)?.Replace("\\", "/");
            return string.IsNullOrWhiteSpace(folder) ? DefaultFolder : folder;
        }

        private static string NormalizeFolderPath(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return DefaultFolder;
            }

            string normalized = folderPath.Replace("\\", "/").TrimEnd('/');
            return normalized.StartsWith("Assets") ? normalized : DefaultFolder;
        }

        private static string NormalizeAudioRoot(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return DefaultAudioRoot;
            }

            string normalized = folderPath.Replace("\\", "/").TrimEnd('/');
            return normalized.StartsWith("Assets") ? normalized : DefaultAudioRoot;
        }

        private static string ResolveAssetFolder(string folderPath)
        {
            string normalized = NormalizeFolderPath(folderPath);
            string catalogPath = Path.Combine(normalized, CatalogAssetName).Replace("\\", "/");
            string settingsPath = Path.Combine(normalized, SettingsAssetName).Replace("\\", "/");
            bool hasCatalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(catalogPath) != null;
            bool hasSettings = AssetDatabase.LoadAssetAtPath<AudioSettingsData>(settingsPath) != null;

            return hasCatalog && hasSettings ? normalized : DefaultFolder;
        }

        private static void CreateFolderRecursive(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }

        private static List<AudioCueDefinition> CreateDefaultCueDefinitions()
        {
            return new List<AudioCueDefinition>
            {
                new() { id = "bgm.main", bus = AudioBus.Music, baseVolume = 1f, pitchMin = 1f, pitchMax = 1f, cooldownSeconds = 0f, loop = true },
                new() { id = "bgm.result", bus = AudioBus.Music, baseVolume = 1f, pitchMin = 1f, pitchMax = 1f, cooldownSeconds = 0f, loop = true },
                new() { id = "sfx.card.play", bus = AudioBus.Sfx, baseVolume = 1f, pitchMin = 0.95f, pitchMax = 1.05f, cooldownSeconds = 0f, loop = false },
                new() { id = "sfx.coin.gain", bus = AudioBus.Sfx, baseVolume = 1f, pitchMin = 1f, pitchMax = 1f, cooldownSeconds = 0.05f, loop = false },
                new() { id = "sfx.coin.lose", bus = AudioBus.Sfx, baseVolume = 1f, pitchMin = 1f, pitchMax = 1f, cooldownSeconds = 0.05f, loop = false },
                new() { id = "sfx.turn.start", bus = AudioBus.Sfx, baseVolume = 1f, pitchMin = 1f, pitchMax = 1f, cooldownSeconds = 0f, loop = false },
                new() { id = "sfx.reward.show", bus = AudioBus.Sfx, baseVolume = 1f, pitchMin = 1f, pitchMax = 1f, cooldownSeconds = 0f, loop = false },
                new() { id = "sfx.reward.pick", bus = AudioBus.Sfx, baseVolume = 1f, pitchMin = 1f, pitchMax = 1f, cooldownSeconds = 0f, loop = false },
                new() { id = "ui.button", bus = AudioBus.Ui, baseVolume = 1f, pitchMin = 1f, pitchMax = 1f, cooldownSeconds = 0f, loop = false }
            };
        }
    }
}
