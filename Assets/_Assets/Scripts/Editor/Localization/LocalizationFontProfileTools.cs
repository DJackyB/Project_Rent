using Martian.Localization;
using UnityEditor;
using UnityEngine;

namespace BaoZuPo.Editor.Localization
{
    public static class LocalizationFontProfileTools
    {
        private const string ResourcesFolderPath = "Assets/Resources";
        private const string LocalizationFolderPath = "Assets/Resources/Localization";
        private const string FontProfileAssetPath = LocalizationFolderPath + "/LocalizationFontProfile.asset";

        [MenuItem("Tools/BaoZuPo/Localization/Create Or Select Font Profile")]
        public static void CreateOrSelectFontProfile()
        {
            EnsureFolder(ResourcesFolderPath);
            EnsureFolder(LocalizationFolderPath);

            LocalizationFontProfile profile = AssetDatabase.LoadAssetAtPath<LocalizationFontProfile>(FontProfileAssetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<LocalizationFontProfile>();
                AssetDatabase.CreateAsset(profile, FontProfileAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }

                currentPath = nextPath;
            }
        }
    }
}
