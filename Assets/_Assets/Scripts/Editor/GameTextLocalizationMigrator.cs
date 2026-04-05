using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace BaoZuPo.Editor.Localization
{
    /// <summary>
    /// Combined text tooling for project-owned gameplay copy.
    /// Handles both the legacy GameText workflow and the card localization CSV flow.
    /// </summary>
    public sealed class GameTextLocalizationMigrator : EditorWindow
    {
        private const string GameTextPath = "Assets/_Assets/Scripts/UI/GameText.cs";
        private const string TsvOutputPath = "Assets/_Assets/Data/Localization/GameText.csv";
        private const string CardCsvPath = "Assets/_Assets/Data/Localization/Card.csv";
        private const string LocalizationAssetDir = "Assets/_Assets/Data/Localization";
        private const string CollectionName = "GameText";
        private const string EnLocaleCode = "en";
        private const string ZhLocaleCode = "zh-Hans";

        private readonly List<TextEntry> _entries = new();
        private Vector2 _scroll;
        private string _statusMessage;

        [MenuItem("Tools/BaoZuPo/Localization/Text Tools")]
        public static void Open()
        {
            GetWindow<GameTextLocalizationMigrator>("Localization Text Tools").Show();
        }

        private void OnEnable()
        {
            ParseGameText();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Localization Text Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Project-owned text tools. Use GameText for shared UI copy, and Card Text for card name/description bilingual sync.",
                MessageType.Info);
            EditorGUILayout.Space(4f);

            DrawGameTextSection();
            EditorGUILayout.Space(8f);
            DrawCardTextSection();
            EditorGUILayout.Space(6f);

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, MessageType.None);
            }

            EditorGUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Key", EditorStyles.boldLabel, GUILayout.Width(270f));
            EditorGUILayout.LabelField("en", EditorStyles.boldLabel, GUILayout.Width(230f));
            EditorGUILayout.LabelField("zh-Hans", EditorStyles.boldLabel, GUILayout.Width(200f));
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (TextEntry entry in _entries)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.SelectableLabel(entry.Key, GUILayout.Width(270f), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.SelectableLabel(entry.EnValue, GUILayout.Width(230f), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(entry.ZhValue) ? "-" : entry.ZhValue, GUILayout.Width(200f));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField($"Entries: {_entries.Count}", EditorStyles.miniLabel);
        }

        private void DrawGameTextSection()
        {
            EditorGUILayout.LabelField("GameText", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Parses GameText.cs, exports/imports GameText.csv, then writes the GameText string table.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("1. Parse GameText"))
            {
                ParseGameText();
            }

            if (GUILayout.Button("2. Export GameText CSV"))
            {
                ExportTsv();
            }

            if (GUILayout.Button("3. Import GameText CSV"))
            {
                ImportTsv();
            }

            if (GUILayout.Button("4. Generate GameText Table"))
            {
                GenerateStringTable();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCardTextSection()
        {
            EditorGUILayout.LabelField("Card Text", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Syncs CardData into the Card localization table, exports Card.csv for translation, and imports translated English back into the table.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("1. Sync Card Tables"))
            {
                CardLocalizationSyncUtility.SyncCardTablesFromCardData();
                SetStatus("Card tables synced from CardData and Card.csv updated.");
            }

            if (GUILayout.Button("2. Export Card CSV"))
            {
                CardLocalizationSyncUtility.SyncCardTablesFromCardData();
                EditorUtility.RevealInFinder(CardCsvPath);
                SetStatus("Card.csv exported to Assets/_Assets/Data/Localization.");
            }

            if (GUILayout.Button("3. Import Card CSV"))
            {
                CardLocalizationSyncUtility.ImportCardCsv();
                SetStatus("Card.csv imported into the Card English table.");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ParseGameText()
        {
            _entries.Clear();

            if (!File.Exists(GameTextPath))
            {
                SetStatus($"Could not find {GameTextPath}");
                return;
            }

            string src = File.ReadAllText(GameTextPath, Encoding.UTF8);
            ParseSimpleProperties(src);
            ParseSingleLineMethods(src);
            ParseSwitchMethod(src, "PhaseName");
            ParseSwitchMethod(src, "TypeLabel");

            SetStatus($"Parsed {_entries.Count} GameText entries.");
            Repaint();
        }

        private void ParseSimpleProperties(string src)
        {
            Regex regex = new Regex(@"public static string (\w+) => Resolve\(""[^""]+"", ""([^""]+)""");
            foreach (Match match in regex.Matches(src))
            {
                _entries.Add(new TextEntry
                {
                    Key = $"GameText.{match.Groups[1].Value}",
                    EnValue = match.Groups[2].Value
                });
            }
        }

        private void ParseSingleLineMethods(string src)
        {
            Regex regex = new Regex(
                @"public static string (\w+)\(([^)]*)\)\s*=>\s*Resolve\(""[^""]+"",\s*\$""([^""]+)""",
                RegexOptions.Singleline);

            foreach (Match match in regex.Matches(src))
            {
                string methodName = match.Groups[1].Value;
                string paramDecls = match.Groups[2].Value;
                string template = match.Groups[3].Value;

                List<string> paramNames = ExtractParamNames(paramDecls);
                for (int i = 0; i < paramNames.Count; i++)
                {
                    template = template.Replace($"{{{paramNames[i]}}}", $"{{{i}}}");
                }

                _entries.Add(new TextEntry
                {
                    Key = $"GameText.{methodName}",
                    EnValue = template
                });
            }
        }

        private void ParseSwitchMethod(string src, string methodName)
        {
            int methodStart = src.IndexOf($"public static string {methodName}(", System.StringComparison.Ordinal);
            if (methodStart < 0)
            {
                return;
            }

            int switchStart = src.IndexOf("switch", methodStart, System.StringComparison.Ordinal);
            if (switchStart < 0)
            {
                return;
            }

            int braceOpen = src.IndexOf('{', switchStart);
            if (braceOpen < 0)
            {
                return;
            }

            int depth = 0;
            int braceClose = -1;
            for (int i = braceOpen; i < src.Length; i++)
            {
                if (src[i] == '{')
                {
                    depth++;
                }
                else if (src[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        braceClose = i;
                        break;
                    }
                }
            }

            if (braceClose < 0)
            {
                return;
            }

            string block = src.Substring(braceOpen + 1, braceClose - braceOpen - 1);
            Regex caseRegex = new Regex(@"Resolve\(""[^""]+\." + methodName + @"\.(\w+)"",\s*""([^""]+)""");
            foreach (Match match in caseRegex.Matches(block))
            {
                _entries.Add(new TextEntry
                {
                    Key = $"GameText.{methodName}.{match.Groups[1].Value}",
                    EnValue = match.Groups[2].Value
                });
            }
        }

        private static List<string> ExtractParamNames(string paramDecls)
        {
            List<string> names = new List<string>();
            foreach (string param in paramDecls.Split(','))
            {
                string trimmed = param.Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                string[] parts = trimmed.Split(' ');
                if (parts.Length >= 2)
                {
                    names.Add(parts[parts.Length - 1].Trim());
                }
            }

            return names;
        }

        private void ExportTsv()
        {
            EnsureLocalizationDir();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Key,en,zh-Hans");
            foreach (TextEntry entry in _entries)
            {
                sb.AppendLine(Escape(entry.Key) + "," + Escape(entry.EnValue) + "," + Escape(entry.ZhValue ?? string.Empty));
            }

            File.WriteAllText(TsvOutputPath, sb.ToString(), new UTF8Encoding(true));
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(TsvOutputPath);
            SetStatus($"Exported GameText CSV to {TsvOutputPath}.");
        }

        private void ImportTsv()
        {
            if (!File.Exists(TsvOutputPath))
            {
                SetStatus($"Could not find {TsvOutputPath}. Export it first.");
                return;
            }

            string[] lines = File.ReadAllLines(TsvOutputPath, Encoding.UTF8);
            Dictionary<string, string> zhMap = new Dictionary<string, string>();

            for (int i = 1; i < lines.Length; i++)
            {
                string[] cols = SplitCsvLine(lines[i]);
                if (cols.Length >= 3 && !string.IsNullOrWhiteSpace(cols[2]))
                {
                    zhMap[cols[0]] = cols[2];
                }
            }

            int filled = 0;
            foreach (TextEntry entry in _entries)
            {
                if (zhMap.TryGetValue(entry.Key, out string zh))
                {
                    entry.ZhValue = zh;
                    filled++;
                }
            }

            SetStatus($"Imported {filled} zh-Hans GameText entries from CSV.");
            Repaint();
        }

        private void GenerateStringTable()
        {
            EnsureLocalizationDir();

            Locale enLocale = GetOrCreateLocale(EnLocaleCode);
            Locale zhLocale = GetOrCreateLocale(ZhLocaleCode);

            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(CollectionName);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(
                    CollectionName,
                    LocalizationAssetDir,
                    new List<Locale> { enLocale, zhLocale });
            }

            StringTable enTable = collection.GetTable(new LocaleIdentifier(EnLocaleCode)) as StringTable;
            StringTable zhTable = collection.GetTable(new LocaleIdentifier(ZhLocaleCode)) as StringTable;
            if (enTable == null || zhTable == null)
            {
                SetStatus("Could not resolve the GameText string tables.");
                return;
            }

            foreach (TextEntry entry in _entries)
            {
                UpsertEntry(enTable, entry.Key, entry.EnValue);
                if (!string.IsNullOrWhiteSpace(entry.ZhValue))
                {
                    UpsertEntry(zhTable, entry.Key, entry.ZhValue);
                }
            }

            EditorUtility.SetDirty(enTable);
            EditorUtility.SetDirty(zhTable);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SetStatus($"Generated GameText string tables in {LocalizationAssetDir}.");
        }

        private static void UpsertEntry(StringTable table, string key, string value)
        {
            StringTableEntry entry = table.GetEntry(key) ?? table.AddEntry(key, string.Empty);
            string nextValue = value ?? string.Empty;
            if (entry.Value != nextValue)
            {
                entry.Value = nextValue;
            }
        }

        private static Locale GetOrCreateLocale(string localeCode)
        {
            foreach (Locale locale in LocalizationEditorSettings.GetLocales())
            {
                if (locale.Identifier.Code == localeCode)
                {
                    return locale;
                }
            }

            Locale newLocale = Locale.CreateLocale(new LocaleIdentifier(localeCode));
            string path = $"{LocalizationAssetDir}/Locale-{localeCode}.asset";
            AssetDatabase.CreateAsset(newLocale, path);
            LocalizationEditorSettings.AddLocale(newLocale);
            return newLocale;
        }

        private static void EnsureLocalizationDir()
        {
            if (Directory.Exists(LocalizationAssetDir))
            {
                return;
            }

            Directory.CreateDirectory(LocalizationAssetDir);
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.IndexOf(',') >= 0 || value.IndexOf('"') >= 0 || value.IndexOf('\n') >= 0)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        private static string[] SplitCsvLine(string line)
        {
            List<string> fields = new List<string>();
            int index = 0;

            while (index <= line.Length)
            {
                if (index == line.Length)
                {
                    fields.Add(string.Empty);
                    break;
                }

                if (line[index] == '"')
                {
                    StringBuilder sb = new StringBuilder();
                    index++;
                    while (index < line.Length)
                    {
                        if (line[index] == '"')
                        {
                            if (index + 1 < line.Length && line[index + 1] == '"')
                            {
                                sb.Append('"');
                                index += 2;
                            }
                            else
                            {
                                index++;
                                break;
                            }
                        }
                        else
                        {
                            sb.Append(line[index]);
                            index++;
                        }
                    }

                    fields.Add(sb.ToString());
                    if (index < line.Length && line[index] == ',')
                    {
                        index++;
                    }
                }
                else
                {
                    int start = index;
                    while (index < line.Length && line[index] != ',')
                    {
                        index++;
                    }

                    fields.Add(line.Substring(start, index - start));
                    if (index < line.Length)
                    {
                        index++;
                    }
                }
            }

            return fields.ToArray();
        }

        private void SetStatus(string message)
        {
            _statusMessage = message;
            Debug.Log($"[LocalizationTextTools] {message}");
        }

        private sealed class TextEntry
        {
            public string Key;
            public string EnValue;
            public string ZhValue;
        }
    }
}
