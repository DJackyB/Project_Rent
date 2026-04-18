using System.IO;
using UnityEditor;
using UnityEngine;

namespace Martian.Localization.Editor
{
    public sealed class LocalizationProjectInstallerWindow : EditorWindow
    {
        private UnityEditor.Editor _configEditor;
        private LocalizationProjectSetupConfig _config;

        [MenuItem("Tools/Martian/Localization/Installer")]
        public static void Open()
        {
            GetWindow<LocalizationProjectInstallerWindow>("Localization Installer").Show();
        }

        private void OnEnable()
        {
            _config = LocalizationProjectSetupConfig.LoadFirstOrCreateTransient();
        }

        private void OnDisable()
        {
            if (_configEditor != null)
            {
                DestroyImmediate(_configEditor);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Martian Localization Installer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use a saved config asset for project-specific defaults, then run one-click setup to create locales, tables, settings, and bundled TMP font wiring.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _config = (LocalizationProjectSetupConfig)EditorGUILayout.ObjectField("Config Asset", _config, typeof(LocalizationProjectSetupConfig), false);
            if (EditorGUI.EndChangeCheck())
            {
                RebuildConfigEditor();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Config Asset"))
            {
                CreateConfigAsset();
            }

            if (GUILayout.Button("Use Default Config"))
            {
                _config = LocalizationProjectSetupConfig.CreateTransientDefault();
                RebuildConfigEditor();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);

            if (_config == null)
            {
                EditorGUILayout.HelpBox("Create or assign a setup config before running the installer.", MessageType.Warning);
                return;
            }

            _config.EnsureDefaults();
            UnityEditor.Editor.CreateCachedEditor(_config, null, ref _configEditor);
            _configEditor?.OnInspectorGUI();

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Run One-Click Setup", GUILayout.Height(32f)))
            {
                LocalizationFontAssetGenerator.SetupProjectLocalization(_config);
            }
        }

        private void RebuildConfigEditor()
        {
            if (_configEditor != null)
            {
                DestroyImmediate(_configEditor);
                _configEditor = null;
            }
        }

        private void CreateConfigAsset()
        {
            string assetPath = LocalizationProjectSetupConfig.DefaultAssetPath;
            string folder = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            EnsureFolder(folder);

            var config = CreateInstance<LocalizationProjectSetupConfig>();
            config.EnsureDefaults();

            AssetDatabase.CreateAsset(config, AssetDatabase.GenerateUniqueAssetPath(assetPath));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _config = config;
            RebuildConfigEditor();
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string parentFolder = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            string folderName = Path.GetFileName(assetPath);
            EnsureFolder(parentFolder);
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }
    }
}
