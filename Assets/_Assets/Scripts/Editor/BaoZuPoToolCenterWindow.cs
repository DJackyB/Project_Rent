using BaoZuPo.Editor.Localization;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BaoZuPo.Editor
{
    /// <summary>
    /// BaoZuPo 项目工具中心（Odin 驱动）。
    /// 汇聚所有项目侧编辑器工具的快捷入口，不包含 Martian 插件菜单。
    /// Martian 插件工具请走 Tools/Martian/ 菜单。
    /// </summary>
    public sealed class BaoZuPoToolCenterWindow : OdinEditorWindow
    {
        [MenuItem("Tools/BaoZuPo/工具中心")]
        public static void Open()
        {
            var window = GetWindow<BaoZuPoToolCenterWindow>();
            window.titleContent = new GUIContent("BaoZuPo 工具中心");
            window.minSize = new Vector2(680f, 640f);
            window.Show();
        }

        // ── 说明 ──────────────────────────────────────────────────

        [Title("BaoZuPo 工具中心")]
        [InfoBox(
            "项目工具统一入口。卡牌、文本、音频、数值模拟都在这里。\n" +
            "Martian 插件工具请走 Tools/Martian/ 菜单，不在此处列出。",
            InfoMessageType.None)]
        [ShowInInspector, ReadOnly, HideLabel, PropertyOrder(-100)]
        public string Note => $"当前卡牌：{CountCards()} 张";

        // ── 快捷路径 ─────────────────────────────────────────────

        [TitleGroup("快捷路径")]
        [HorizontalGroup("快捷路径/行")]
        [Button("显示卡牌 Excel", ButtonSizes.Medium), GUIColor(0.85f, 0.85f, 0.85f)]
        [PropertyOrder(-90)]
        public void RevealCardExcel()
        {
            string fullPath = Path.GetFullPath("Assets/_Assets/Data/Excel/CardData.xlsx");
            if (File.Exists(fullPath))
                EditorUtility.RevealInFinder(fullPath);
            else
                Debug.LogWarning($"[工具中心] Excel 文件不存在：{fullPath}");
        }

        [TitleGroup("快捷路径")]
        [HorizontalGroup("快捷路径/行")]
        [Button("显示本地化文件夹", ButtonSizes.Medium), GUIColor(0.85f, 0.85f, 0.85f)]
        [PropertyOrder(-89)]
        public void RevealLocalizationFolder()
        {
            EditorUtility.RevealInFinder(Path.GetFullPath("Assets/_Assets/Data/Localization"));
        }

        [TitleGroup("快捷路径")]
        [HorizontalGroup("快捷路径/行")]
        [Button("显示卡牌资源文件夹", ButtonSizes.Medium), GUIColor(0.85f, 0.85f, 0.85f)]
        [PropertyOrder(-88)]
        public void RevealCardsFolder()
        {
            EditorUtility.RevealInFinder(Path.GetFullPath("Assets/Resources/Cards"));
        }

        // ── 卡牌 ─────────────────────────────────────────────────

        [TitleGroup("卡牌")]
        [HorizontalGroup("卡牌/行")]
        [Button("导入卡牌数据", ButtonSizes.Large), GUIColor(0.25f, 0.55f, 0.95f)]
        [PropertyOrder(-80)]
        public void ImportCardData() => CardDataImporter.Import();

        [TitleGroup("卡牌")]
        [HorizontalGroup("卡牌/行")]
        [Button("同步卡库", ButtonSizes.Large)]
        [PropertyOrder(-79)]
        public void SyncCardLibraries() => CardDataImporter.SyncCardLibraries();

        [ShowInInspector, TitleGroup("卡牌"), PropertyOrder(-78), HideLabel, ReadOnly, MultiLineProperty(2)]
        public string CardHelp =>
            "「导入卡牌数据」会读取 Excel → 更新 CardData → 自动同步本地化 → 重建卡库，是完整流程。\n" +
            "「同步卡库」仅在卡牌数量/分组改动但 Excel 未变时单独使用。";

        // ── 文本 ─────────────────────────────────────────────────

        [TitleGroup("文本")]
        [HorizontalGroup("文本/行")]
        [Button("打开文本工具", ButtonSizes.Large), GUIColor(0.30f, 0.70f, 0.45f)]
        [PropertyOrder(-70)]
        public void OpenTextTools() => GameTextLocalizationMigrator.Open();

        [TitleGroup("文本")]
        [HorizontalGroup("文本/行")]
        [Button("同步 GameText", ButtonSizes.Large)]
        [PropertyOrder(-69)]
        public void SyncGameText() => GameTextLocalizationMigrator.SyncGameTextDirect();

        [ShowInInspector, TitleGroup("文本"), PropertyOrder(-68), HideLabel, ReadOnly, MultiLineProperty(2)]
        public string TextHelp =>
            "「打开文本工具」：预览所有文案条目、管理 GameText 和卡牌双语 CSV，多步骤操作在窗口里做。\n" +
            "「同步 GameText」：静默同步，不打开窗口，适合改完 GameText.cs 后快速推送。";

        // ── 音频 ─────────────────────────────────────────────────

        [TitleGroup("音频")]
        [HorizontalGroup("音频/行")]
        [Button("① 生成音频资产", ButtonSizes.Medium), GUIColor(0.95f, 0.55f, 0.25f)]
        [PropertyOrder(-60)]
        public void GenerateAudioAssets() => AudioAssetSetupUtility.GenerateAudioAssets();

        [TitleGroup("音频")]
        [HorizontalGroup("音频/行")]
        [Button("② 配置当前场景音频", ButtonSizes.Medium)]
        [PropertyOrder(-59)]
        public void SetupSceneAudio() => AudioAssetSetupUtility.SetupActiveSceneAudio();

        [TitleGroup("音频")]
        [HorizontalGroup("音频/行")]
        [Button("③ 自动绑定 Clip", ButtonSizes.Medium)]
        [PropertyOrder(-58)]
        public void AssignAudioClips() => AudioAssetSetupUtility.AssignClipsByCueId();

        [ShowInInspector, TitleGroup("音频"), PropertyOrder(-57), HideLabel, ReadOnly, MultiLineProperty(2)]
        public string AudioHelp =>
            "初次接入按 ①②③ 顺序执行。后续只改 Clip 文件时只需执行 ③。\n" +
            "这三个工具是 BaoZuPo 对 Martian.Audio 的项目适配，不是插件通用菜单。";

        // ── 数值模拟 ──────────────────────────────────────────────

        [TitleGroup("数值模拟")]
        [Button("平衡模拟器", ButtonSizes.Large), GUIColor(0.75f, 0.45f, 0.95f)]
        [PropertyOrder(-50)]
        public void OpenBalanceSimulator() => BalanceSimulatorWindow.Open();

        [ShowInInspector, TitleGroup("数值模拟"), PropertyOrder(-49), HideLabel, ReadOnly]
        public string SimulatorHelp =>
            "实时查看卡牌净收益、回本周期、ROI，以及贷款压力曲线。可导出分析 CSV。";

        // ── 内部 ─────────────────────────────────────────────────

        private static int CountCards()
        {
            return AssetDatabase.FindAssets("t:CardData").Length;
        }
    }
}
