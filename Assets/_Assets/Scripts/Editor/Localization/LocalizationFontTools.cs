using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BaoZuPo.Card;
using BaoZuPo.Localization;
using Martian.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BaoZuPo.Editor.Localization
{
    public static class LocalizationFontTools
    {
        private const string SourceFontAssetPath = "Assets/Resources/Fonts/SourceHanSansSC-Regular.otf";
        private const string GeneratedFontAssetPath = "Assets/Resources/Fonts/BaoZuPoChineseDynamic.asset";
        private const string GeneratedFontName = "BaoZuPoChineseDynamic";
        private static readonly Regex TmpTextRegex = new(@"m_text:\s*(.*)", RegexOptions.Compiled);

        [MenuItem("Tools/BaoZuPo/Fonts/Scan And Update Localization Font Atlas")]
        public static void ScanAndUpdateLocalizationFontAtlas()
        {
            TMP_FontAsset fontAsset = EnsureFontAsset();
            if (fontAsset == null)
            {
                return;
            }

            string characters = CollectCharacters();
            if (string.IsNullOrEmpty(characters))
            {
                Debug.LogWarning("[LocalizationFontTools] No characters collected.");
                return;
            }

            if (!fontAsset.TryAddCharacters(characters, out string missingCharacters))
            {
                Debug.LogWarning($"[LocalizationFontTools] Some characters could not be added: {missingCharacters}");
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LocalizationFontTools] Updated font atlas with {characters.Length} unique characters.");
        }

        private static TMP_FontAsset EnsureFontAsset()
        {
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontAssetPath);
            if (sourceFont == null)
            {
                Debug.LogError($"[LocalizationFontTools] Missing source font: {SourceFontAssetPath}");
                return null;
            }

            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GeneratedFontAssetPath);
            if (IsFontAssetUsable(fontAsset))
            {
                return fontAsset;
            }

            if (fontAsset != null)
            {
                AssetDatabase.DeleteAsset(GeneratedFontAssetPath);
                AssetDatabase.Refresh();
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
                Debug.LogError("[LocalizationFontTools] Failed to create TMP font asset.");
                return null;
            }

            fontAsset.name = GeneratedFontName;
            AssetDatabase.CreateAsset(fontAsset, GeneratedFontAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return fontAsset;
        }

        private static bool IsFontAssetUsable(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null || fontAsset.material == null)
            {
                return false;
            }

            var atlasTextures = fontAsset.atlasTextures;
            if (atlasTextures == null || atlasTextures.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < atlasTextures.Length; i++)
            {
                if (atlasTextures[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static string CollectCharacters()
        {
            var characters = new SortedSet<char>();

            AddString(characters, GameText.FontSeedCommonCharacters);
            foreach (string text in GameText.GetFontSeedTexts())
            {
                AddString(characters, text);
            }

            CollectCardCharacters(characters);
            CollectExcelCharacters(characters);
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

                AddString(characters, cardData.DefaultName);
                AddString(characters, cardData.DefaultDescription);
                AddString(characters, cardData.ResolveNameTextKey());
                AddString(characters, cardData.ResolveDescriptionTextKey());
            }
        }

        private static void CollectExcelCharacters(ISet<char> characters)
        {
            const string excelPath = "Assets/_Assets/Data/Excel/CardData.xlsx";
            if (!File.Exists(excelPath))
            {
                return;
            }

            try
            {
                using var stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook(stream);
                for (int sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
                {
                    var sheet = workbook.GetSheetAt(sheetIndex);
                    if (sheet == null)
                    {
                        continue;
                    }

                    for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                    {
                        var row = sheet.GetRow(rowIndex);
                        if (row == null)
                        {
                            continue;
                        }

                        for (int col = row.FirstCellNum; col < row.LastCellNum; col++)
                        {
                            var cell = row.GetCell(col);
                            if (cell != null)
                            {
                                AddString(characters, cell.ToString());
                            }
                        }
                    }
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"[LocalizationFontTools] Failed to scan Excel font seed text: {exception.Message}");
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
