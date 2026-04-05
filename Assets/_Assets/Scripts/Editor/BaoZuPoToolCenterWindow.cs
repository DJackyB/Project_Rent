using BaoZuPo.Editor.Localization;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace BaoZuPo.Editor
{
    /// <summary>
    /// Odin-powered unified launcher for common project and plugin tools.
    /// Direct menu items still exist, but this window is the recommended entry.
    /// </summary>
    public sealed class BaoZuPoToolCenterWindow : OdinEditorWindow
    {
        [MenuItem("Tools/BaoZuPo/工具中心")]
        public static void Open()
        {
            BaoZuPoToolCenterWindow window = GetWindow<BaoZuPoToolCenterWindow>();
            window.titleContent = new GUIContent("工具中心");
            window.minSize = new Vector2(760f, 720f);
            window.Show();
        }

        [ShowInInspector]
        [PropertyOrder(-100)]
        [Title("BaoZuPo 工具中心")]
        [InfoBox("这是项目常用工具的统一入口。上半部分是 BaoZuPo 项目工具，下半部分是 Martian 插件工具。建议日常从这里进入，原有菜单保留为快捷方式。", InfoMessageType.None)]
        [LabelText("当前说明")]
        [ReadOnly]
        [PropertySpace(0, 10)]
        public string Summary => "卡牌导入、本地化、音频初始化、数值模拟都可以在这里直达。";

        [TitleGroup("项目工具/卡牌")]
        [Button("导入卡牌数据", ButtonSizes.Large)]
        [GUIColor(0.25f, 0.55f, 0.95f)]
        [PropertyOrder(-90)]
        public void ImportCardData()
        {
            CardDataImporter.Import();
        }

        [TitleGroup("项目工具/卡牌")]
        [Button("同步卡库", ButtonSizes.Medium)]
        [PropertyOrder(-89)]
        public void SyncCardLibraries()
        {
            CardDataImporter.SyncCardLibraries();
        }

        [ShowInInspector]
        [TitleGroup("项目工具/卡牌")]
        [PropertyOrder(-88)]
        [HideLabel]
        [ReadOnly]
        [MultiLineProperty(2)]
        public string CardHelp => "导入卡牌数据会读取 Excel，更新 CardData，并自动同步卡牌本地化和卡库。只想重建卡库时再单独点“同步卡库”。";

        [TitleGroup("项目工具/本地化")]
        [Button("打开文本工具", ButtonSizes.Large)]
        [GUIColor(0.30f, 0.70f, 0.45f)]
        [PropertyOrder(-80)]
        public void OpenTextTools()
        {
            GameTextLocalizationMigrator.Open();
        }

        [TitleGroup("项目工具/本地化")]
        [HorizontalGroup("项目工具/本地化/卡牌按钮")]
        [Button("同步卡牌双语表", ButtonSizes.Medium)]
        [PropertyOrder(-79)]
        public void SyncCardTables()
        {
            CardLocalizationSyncUtility.SyncCardTablesMenu();
        }

        [TitleGroup("项目工具/本地化")]
        [HorizontalGroup("项目工具/本地化/卡牌按钮")]
        [Button("导出卡牌翻译 CSV", ButtonSizes.Medium)]
        [PropertyOrder(-78)]
        public void ExportCardCsv()
        {
            CardLocalizationSyncUtility.ExportCardCsvMenu();
        }

        [TitleGroup("项目工具/本地化")]
        [HorizontalGroup("项目工具/本地化/卡牌按钮")]
        [Button("导入卡牌翻译 CSV", ButtonSizes.Medium)]
        [PropertyOrder(-77)]
        public void ImportCardCsv()
        {
            CardLocalizationSyncUtility.ImportCardCsvMenu();
        }

        [ShowInInspector]
        [TitleGroup("项目工具/本地化")]
        [PropertyOrder(-76)]
        [HideLabel]
        [ReadOnly]
        [MultiLineProperty(2)]
        public string LocalizationHelp => "“文本工具”里已经整合了 GameText 和 Card Text 的流程。这里额外保留卡牌双语按钮，方便你直接操作 CSV。";

        [TitleGroup("项目工具/音频")]
        [HorizontalGroup("项目工具/音频/按钮")]
        [Button("生成音频资源", ButtonSizes.Medium)]
        [GUIColor(0.95f, 0.55f, 0.25f)]
        [PropertyOrder(-70)]
        public void GenerateAudioAssets()
        {
            AudioAssetSetupUtility.GenerateAudioAssets();
        }

        [TitleGroup("项目工具/音频")]
        [HorizontalGroup("项目工具/音频/按钮")]
        [Button("配置当前场景音频", ButtonSizes.Medium)]
        [PropertyOrder(-69)]
        public void SetupSceneAudio()
        {
            AudioAssetSetupUtility.SetupActiveSceneAudio();
        }

        [TitleGroup("项目工具/音频")]
        [HorizontalGroup("项目工具/音频/按钮")]
        [Button("按 CueId 自动绑音频", ButtonSizes.Medium)]
        [PropertyOrder(-68)]
        public void AssignAudioClips()
        {
            AudioAssetSetupUtility.AssignClipsByCueId();
        }

        [ShowInInspector]
        [TitleGroup("项目工具/音频")]
        [PropertyOrder(-67)]
        [HideLabel]
        [ReadOnly]
        [MultiLineProperty(2)]
        public string AudioHelp => "这一组是 BaoZuPo 项目对 Martian.Audio 的适配工具，不是插件通用菜单。它们会按你项目的 cue 和场景约定来接线。";

        [TitleGroup("项目工具/数值模拟")]
        [Button("打开平衡模拟器", ButtonSizes.Large)]
        [GUIColor(0.75f, 0.45f, 0.95f)]
        [PropertyOrder(-60)]
        public void OpenBalanceSimulator()
        {
            BalanceSimulatorWindow.Open();
        }

        [ShowInInspector]
        [TitleGroup("Martian 插件/本地化")]
        [PropertyOrder(-50)]
        [HideLabel]
        [ReadOnly]
        [MultiLineProperty(2)]
        public string PluginHelp => "这一组是插件侧入口。凡是明显属于 Martian 通用能力的，统一放在这里，通过菜单转发，不直接耦合插件 Editor 程序集。";

        [TitleGroup("Martian 插件/本地化")]
        [HorizontalGroup("Martian 插件/本地化/按钮")]
        [Button("打开本地化安装器", ButtonSizes.Medium)]
        [GUIColor(0.40f, 0.80f, 0.90f)]
        [PropertyOrder(-49)]
        public void OpenMartianLocalizationInstaller()
        {
            EditorApplication.ExecuteMenuItem("Tools/Martian/Localization/Installer");
        }

        [TitleGroup("Martian 插件/本地化")]
        [HorizontalGroup("Martian 插件/本地化/按钮")]
        [Button("运行高级 Setup", ButtonSizes.Medium)]
        [PropertyOrder(-48)]
        public void RunMartianLocalizationSetup()
        {
            EditorApplication.ExecuteMenuItem("Tools/Martian/Localization/Advanced/Setup Project Localization");
        }
    }
}
