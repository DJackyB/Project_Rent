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
    /// GameText 本地化迁移工具。
    ///
    /// 功能：
    /// 1. 解析 GameText.cs，自动提取所有文本条目（属性、格式化方法、switch 枚举）
    /// 2. 在编辑器窗口预览提取结果
    /// 3. 导出 TSV 文件供翻译填写
    /// 4. 导入翻译后的 TSV，将中文值写入窗口预览
    /// 5. 生成 Unity Localization StringTableCollection（en + zh-Hans）
    ///
    /// 使用流程：
    ///   Tools → BaoZuPo → GameText 本地化迁移
    ///   → 解析 → 导出 TSV → 填写 zh-Hans 列 → 导入 TSV → 生成 StringTable
    /// </summary>
    public sealed class GameTextLocalizationMigrator : EditorWindow
    {
        private const string GameTextPath = "Assets/_Assets/Scripts/UI/GameText.cs";
        private const string TsvOutputPath = "Assets/_Assets/Data/Localization/GameText.csv";
        private const string LocalizationAssetDir = "Assets/_Assets/Data/Localization";
        private const string CollectionName = "GameText";
        private const string EnLocaleCode = "en";
        private const string ZhLocaleCode = "zh-Hans";

        private readonly List<TextEntry> _entries = new();
        private Vector2 _scroll;
        private string _statusMessage;

        [MenuItem("Tools/BaoZuPo/GameText 本地化迁移")]
        public static void Open()
        {
            GetWindow<GameTextLocalizationMigrator>("GameText 本地化迁移").Show();
        }

        private void OnEnable()
        {
            ParseGameText();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("GameText 本地化迁移工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "流程：解析 → 导出 CSV → 在 Excel 填写 zh-Hans 列 → 导入 CSV → 生成 StringTable",
                MessageType.Info);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("1. 重新解析")) ParseGameText();
            if (GUILayout.Button("2. 导出 CSV")) ExportTsv();
            if (GUILayout.Button("3. 导入 CSV")) ImportTsv();
            if (GUILayout.Button("4. 生成 StringTable")) GenerateStringTable();
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, MessageType.None);
            }

            EditorGUILayout.Space(4);

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Key", EditorStyles.boldLabel, GUILayout.Width(270));
            EditorGUILayout.LabelField("en", EditorStyles.boldLabel, GUILayout.Width(230));
            EditorGUILayout.LabelField("zh-Hans", EditorStyles.boldLabel, GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var entry in _entries)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.SelectableLabel(entry.Key, GUILayout.Width(270), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.SelectableLabel(entry.EnValue, GUILayout.Width(230), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                var zhStyle = string.IsNullOrWhiteSpace(entry.ZhValue) ? EditorStyles.label : EditorStyles.label;
                EditorGUILayout.LabelField(
                    string.IsNullOrWhiteSpace(entry.ZhValue) ? "—" : entry.ZhValue,
                    GUILayout.Width(200));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField($"共 {_entries.Count} 条", EditorStyles.miniLabel);
        }

        // ── 解析 ─────────────────────────────────────────────────────────────

        private void ParseGameText()
        {
            _entries.Clear();

            if (!File.Exists(GameTextPath))
            {
                SetStatus($"找不到 {GameTextPath}");
                return;
            }

            var src = File.ReadAllText(GameTextPath, Encoding.UTF8);

            ParseSimpleProperties(src);
            ParseSingleLineMethods(src);
            ParseSwitchMethod(src, "PhaseName");
            ParseSwitchMethod(src, "TypeLabel");

            SetStatus($"解析完成：{_entries.Count} 条");
            Repaint();
        }

        /// <summary>
        /// 提取单行属性：public static string Xxx => "value";
        /// </summary>
        private void ParseSimpleProperties(string src)
        {
            var re = new Regex(@"public static string (\w+) => ""([^""]+)"";");
            foreach (Match m in re.Matches(src))
            {
                _entries.Add(new TextEntry
                {
                    Key = $"GameText.{m.Groups[1].Value}",
                    EnValue = m.Groups[2].Value
                });
            }
        }

        /// <summary>
        /// 提取单行格式化方法（支持任意参数数量）：
        ///   public static string Xxx(T a, T b, ...) => $"... {a} ... {b} ...";
        /// 参数按声明顺序替换为 {0}, {1}, ...
        /// 支持声明与 lambda 体跨行。
        /// </summary>
        private void ParseSingleLineMethods(string src)
        {
            // 匹配方法签名 + => $"..." 的单行 lambda（允许换行）
            var re = new Regex(
                @"public static string (\w+)\(([^)]+)\)\s*=>\s*\$""([^""]+)"";",
                RegexOptions.Singleline);

            foreach (Match m in re.Matches(src))
            {
                var methodName = m.Groups[1].Value;
                var paramDecls = m.Groups[2].Value;
                var template = m.Groups[3].Value;

                // 提取参数名（按顺序）
                var paramNames = ExtractParamNames(paramDecls);

                // 将 {paramName} 替换为 {0}, {1}, ...
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

        /// <summary>
        /// 提取 switch 方法的 case：
        ///   case Xxx.Yyy => "value",  或  case "Yyy" => "value",
        /// 生成 key: GameText.MethodName.CaseName
        /// </summary>
        private void ParseSwitchMethod(string src, string methodName)
        {
            // 找到方法体的 switch 块
            var methodStart = src.IndexOf($"public static string {methodName}(");
            if (methodStart < 0) return;

            var switchStart = src.IndexOf("switch", methodStart);
            if (switchStart < 0) return;

            var braceOpen = src.IndexOf('{', switchStart);
            if (braceOpen < 0) return;

            // 找到匹配的闭括号
            int depth = 0;
            int braceClose = -1;
            for (int i = braceOpen; i < src.Length; i++)
            {
                if (src[i] == '{') depth++;
                else if (src[i] == '}')
                {
                    depth--;
                    if (depth == 0) { braceClose = i; break; }
                }
            }

            if (braceClose < 0) return;

            var block = src.Substring(braceOpen + 1, braceClose - braceOpen - 1);

            // 匹配 case Xxx.CaseName => "value", 或 case "CaseName" => "value",
            var caseRe = new Regex(@"(?:[\w]+\.)?(\w+)\s*=>\s*""([^""]+)""");
            foreach (Match m in caseRe.Matches(block))
            {
                var caseName = m.Groups[1].Value;
                var caseValue = m.Groups[2].Value;
                _entries.Add(new TextEntry
                {
                    Key = $"GameText.{methodName}.{caseName}",
                    EnValue = caseValue
                });
            }
        }

        private static List<string> ExtractParamNames(string paramDecls)
        {
            var names = new List<string>();
            // 参数格式: "Type name, Type name, ..."
            foreach (var param in paramDecls.Split(','))
            {
                var parts = param.Trim().Split(' ');
                if (parts.Length >= 2)
                {
                    names.Add(parts[parts.Length - 1].Trim());
                }
            }
            return names;
        }

        // ── 导出 TSV ──────────────────────────────────────────────────────────

        private void ExportTsv()
        {
            EnsureLocalizationDir();

            static string Escape(string v)
            {
                if (string.IsNullOrEmpty(v)) return string.Empty;
                if (v.IndexOf(',') >= 0 || v.IndexOf('"') >= 0 || v.IndexOf('\n') >= 0)
                    return "\"" + v.Replace("\"", "\"\"") + "\"";
                return v;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Key,en,zh-Hans");
            foreach (var entry in _entries)
            {
                sb.AppendLine(Escape(entry.Key) + "," + Escape(entry.EnValue) + "," + Escape(entry.ZhValue ?? string.Empty));
            }

            File.WriteAllText(TsvOutputPath, sb.ToString(), new UTF8Encoding(true));
            AssetDatabase.Refresh();
            SetStatus($"CSV 已导出：{TsvOutputPath}（{_entries.Count} 条）");
            EditorUtility.RevealInFinder(TsvOutputPath);
        }

        // ── 导入 TSV ──────────────────────────────────────────────────────────

        private void ImportTsv()
        {
            if (!File.Exists(TsvOutputPath))
            {
                SetStatus($"找不到 TSV：{TsvOutputPath}，请先导出");
                return;
            }

            var lines = File.ReadAllLines(TsvOutputPath, Encoding.UTF8);
            var zhMap = new Dictionary<string, string>();

            static string[] Split(string ln)
            {
                var fields = new List<string>();
                int j = 0;
                while (j <= ln.Length)
                {
                    if (j == ln.Length) { fields.Add(string.Empty); break; }
                    if (ln[j] == '"')
                    {
                        var sb2 = new StringBuilder();
                        j++;
                        while (j < ln.Length)
                        {
                            if (ln[j] == '"')
                            {
                                if (j + 1 < ln.Length && ln[j + 1] == '"') { sb2.Append('"'); j += 2; }
                                else { j++; break; }
                            }
                            else sb2.Append(ln[j++]);
                        }
                        fields.Add(sb2.ToString());
                        if (j < ln.Length && ln[j] == ',') j++;
                    }
                    else
                    {
                        int start = j;
                        while (j < ln.Length && ln[j] != ',') j++;
                        fields.Add(ln.Substring(start, j - start));
                        if (j < ln.Length) j++;
                    }
                }
                return fields.ToArray();
            }

            for (int i = 1; i < lines.Length; i++)
            {
                var cols = Split(lines[i]);
                if (cols.Length >= 3 && !string.IsNullOrWhiteSpace(cols[2]))
                {
                    zhMap[cols[0]] = cols[2];
                }
            }

            int filled = 0;
            foreach (var entry in _entries)
            {
                if (zhMap.TryGetValue(entry.Key, out var zh))
                {
                    entry.ZhValue = zh;
                    filled++;
                }
            }

            SetStatus($"已导入 {filled} 条中文翻译");
            Repaint();
        }

        // ── 生成 StringTable ──────────────────────────────────────────────────

        private void GenerateStringTable()
        {
            EnsureLocalizationDir();

            // 确保 Locale 资产存在
            var enLocale = GetOrCreateLocale(EnLocaleCode);
            var zhLocale = GetOrCreateLocale(ZhLocaleCode);

            // 获取或创建 StringTableCollection
            var collection = LocalizationEditorSettings.GetStringTableCollection(CollectionName);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(
                    CollectionName,
                    LocalizationAssetDir,
                    new List<Locale> { enLocale, zhLocale });
            }

            var enTable = collection.GetTable(new LocaleIdentifier(EnLocaleCode)) as StringTable;
            var zhTable = collection.GetTable(new LocaleIdentifier(ZhLocaleCode)) as StringTable;

            if (enTable == null || zhTable == null)
            {
                SetStatus("无法获取 StringTable，请检查 Locale 是否已添加到 Localization Settings");
                return;
            }

            foreach (var entry in _entries)
            {
                enTable.AddEntry(entry.Key, entry.EnValue);
                if (!string.IsNullOrWhiteSpace(entry.ZhValue))
                {
                    zhTable.AddEntry(entry.Key, entry.ZhValue);
                }
            }

            EditorUtility.SetDirty(enTable);
            EditorUtility.SetDirty(zhTable);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SetStatus($"StringTable 已生成（{_entries.Count} 条）→ {LocalizationAssetDir}");
        }

        // ── 辅助 ──────────────────────────────────────────────────────────────

        private static Locale GetOrCreateLocale(string localeCode)
        {
            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                if (locale.Identifier.Code == localeCode)
                {
                    return locale;
                }
            }

            var newLocale = Locale.CreateLocale(new LocaleIdentifier(localeCode));
            var path = $"{LocalizationAssetDir}/Locale-{localeCode}.asset";
            AssetDatabase.CreateAsset(newLocale, path);
            LocalizationEditorSettings.AddLocale(newLocale);
            return newLocale;
        }

        private static void EnsureLocalizationDir()
        {
            if (!Directory.Exists(LocalizationAssetDir))
            {
                Directory.CreateDirectory(LocalizationAssetDir);
            }
        }

        private void SetStatus(string msg)
        {
            _statusMessage = msg;
            Debug.Log($"[GameTextLocalizationMigrator] {msg}");
        }

        // ── 数据结构 ──────────────────────────────────────────────────────────

        private sealed class TextEntry
        {
            public string Key;
            public string EnValue;
            public string ZhValue;
        }
    }
}
