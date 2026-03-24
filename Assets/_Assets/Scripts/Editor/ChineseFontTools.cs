using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BaoZuPo.Card;
using BaoZuPo.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace BaoZuPo.Editor
{
    public static class ChineseFontTools
    {
        private static readonly Regex TmpTextRegex = new(@"m_text:\s*(.*)", RegexOptions.Compiled);

        [MenuItem("Tools/BaoZuPo/Fonts/Scan And Update Chinese Font Atlas")]
        public static void ScanAndUpdateChineseFontAtlas()
        {
            TMP_FontAsset fontAsset = EnsureFontAsset();
            if (fontAsset == null)
            {
                return;
            }

            string characters = CollectCharacters();
            if (string.IsNullOrEmpty(characters))
            {
                Debug.LogWarning("[ChineseFontTools] No characters collected.");
                return;
            }

            if (!fontAsset.TryAddCharacters(characters, out string missingCharacters))
            {
                Debug.LogWarning($"[ChineseFontTools] Some characters could not be added: {missingCharacters}");
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ChineseFontTools] Updated font atlas with {characters.Length} unique characters.");
        }

        private static TMP_FontAsset EnsureFontAsset()
        {
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(UIFontCatalog.SourceFontAssetPath);
            if (sourceFont == null)
            {
                Debug.LogError($"[ChineseFontTools] Missing source font: {UIFontCatalog.SourceFontAssetPath}");
                return null;
            }

            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIFontCatalog.GeneratedFontAssetPath);
            if (fontAsset != null)
            {
                return fontAsset;
            }

            fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic);

            if (fontAsset == null)
            {
                Debug.LogError("[ChineseFontTools] Failed to create TMP font asset.");
                return null;
            }

            fontAsset.name = "BaoZuPoChineseDynamic";
            AssetDatabase.CreateAsset(fontAsset, UIFontCatalog.GeneratedFontAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return fontAsset;
        }

        private static string CollectCharacters()
        {
            var characters = new SortedSet<char>();

            AddString(characters, UIStrings.FontSeedCommonCharacters);
            foreach (string text in UIStrings.GetFontSeedTexts())
            {
                AddString(characters, text);
            }

            CollectCardCharacters(characters);
            CollectYamlTextCharacters(characters, "Assets/_Assets/Prefabs");
            CollectYamlTextCharacters(characters, "Assets/Scenes");

            return new string(characters.ToArray());
        }

        private static void CollectCardCharacters(ISet<char> characters)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:CardData", new[] { "Assets/Resources/Cards" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (cardData == null)
                {
                    continue;
                }

                AddString(characters, cardData.cardName);
                AddString(characters, cardData.description);
            }
        }

        private static void CollectYamlTextCharacters(ISet<char> characters, string rootPath)
        {
            if (!Directory.Exists(rootPath))
            {
                return;
            }

            foreach (string filePath in Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".prefab") || path.EndsWith(".unity")))
            {
                string yaml = File.ReadAllText(filePath);
                foreach (Match match in TmpTextRegex.Matches(yaml))
                {
                    if (match.Groups.Count < 2)
                    {
                        continue;
                    }

                    string rawText = match.Groups[1].Value.Trim();
                    if (string.IsNullOrEmpty(rawText))
                    {
                        continue;
                    }

                    AddString(characters, UnescapeYamlString(rawText));
                }
            }
        }

        private static void AddString(ISet<char> characters, string text)
        {
            if (characters == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            foreach (char character in text)
            {
                if (!char.IsControl(character))
                {
                    characters.Add(character);
                }
            }
        }

        private static string UnescapeYamlString(string value)
        {
            return value
                .Replace("\\n", "\n")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"");
        }
    }
}
