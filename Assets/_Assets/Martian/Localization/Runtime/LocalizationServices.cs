using System;
using System.Collections.Generic;
using System.Globalization;

namespace Martian.Localization
{
    /// <summary>
    /// 本地化服务定位器。提供全局的语言、文本、字体服务访问入口。
    ///
    /// 设计：
    /// - 静态类，不依赖 MonoBehaviour
    /// - 默认使用 Null 实现（返回英文或参数不变）
    /// - 运行时通过 Set* 方法注册真实实现
    ///
    /// 通常的初始化流程：
    /// 1. 项目启动时，某个 Bootstrap 组件调用 SetLanguageService() 等
    /// 2. 其他代码通过 LocalizationServices.Language.SetLanguage() 切换语言
    /// 3. LocalizationServices.Text.Resolve() 获取本地化文本
    /// 4. LocalizationFontUtility.ApplyToText() 应用语言特定的字体
    ///
    /// 当前状态（包租婆项目）：
    /// - ILanguageService：未接线（使用 Null 实现）
    /// - ILocalizedTextService：未接线（使用 Null 实现，回落到 GameText）
    /// - FontProfile：已接线（应用 SourceHanSansSC 中文字体）
    /// </summary>
    public static class LocalizationServices
    {
        /// <summary>语言服务（默认 Null 实现）。</summary>
        private static ILanguageService _language = new NullLanguageService("zh-Hans");

        /// <summary>本地化文本服务（默认 Null 实现）。</summary>
        private static ILocalizedTextService _text = NullLocalizedTextService.Instance;

        /// <summary>本地化启动引导组件。</summary>
        private static ILocalizationBootstrap _bootstrap;

        /// <summary>字体配置文件（为每种语言指定不同的字体）。</summary>
        private static LocalizationFontProfile _fontProfile;

        /// <summary>语言服务访问。</summary>
        public static ILanguageService Language => _language;

        /// <summary>本地化文本服务访问。</summary>
        public static ILocalizedTextService Text => _text;

        /// <summary>启动引导组件访问。</summary>
        public static ILocalizationBootstrap Bootstrap => _bootstrap;

        /// <summary>字体配置文件访问。</summary>
        public static LocalizationFontProfile FontProfile => _fontProfile;

        /// <summary>
        /// 注册语言服务实现。
        /// 语言服务负责管理当前语言和支持的语言列表。
        /// 如果传入 null，使用 Null 实现（始终返回默认语言）。
        /// </summary>
        public static void SetLanguageService(ILanguageService languageService)
        {
            _language = languageService ?? new NullLanguageService("zh-Hans");
            LocalizationFontUtility.InvalidateCache();
        }

        /// <summary>
        /// 注册本地化文本服务实现。
        /// 文本服务负责根据语言和键返回本地化的文本。
        /// 如果传入 null，使用 Null 实现（返回备选文本）。
        /// </summary>
        public static void SetTextService(ILocalizedTextService textService)
        {
            _text = textService ?? NullLocalizedTextService.Instance;
        }

        /// <summary>
        /// 注册本地化启动引导组件。
        /// 通常在 Bootstrap 场景初始化时调用。
        /// </summary>
        public static void SetBootstrap(ILocalizationBootstrap bootstrap)
        {
            _bootstrap = bootstrap;
        }

        /// <summary>
        /// 注册字体配置文件。
        /// 字体会根据当前语言改变。
        /// 调用此方法会清空字体缓存，强制重新加载。
        /// </summary>
        public static void SetFontProfile(LocalizationFontProfile fontProfile)
        {
            _fontProfile = fontProfile;
            LocalizationFontUtility.InvalidateCache();
        }

        /// <summary>
        /// 便捷方法：解析本地化文本。
        /// 等同于 LocalizationServices.Text.Resolve()。
        /// </summary>
        public static string Resolve(LocalizationTextRef textRef, params object[] arguments)
        {
            return _text.Resolve(textRef, arguments);
        }

        /// <summary>
        /// 重置所有服务为默认状态。
        /// 用于卸载或重新初始化本地化系统。
        /// </summary>
        public static void Reset()
        {
            _bootstrap = null;
            _fontProfile = null;
            _language = new NullLanguageService("zh-Hans");
            _text = NullLocalizedTextService.Instance;
            LocalizationFontUtility.InvalidateCache();
        }
    }

    public sealed class NullLanguageService : ILanguageService
    {
        private readonly List<string> _supportedLanguageCodes = new();
        private string _currentLanguageCode;

        public NullLanguageService(string defaultLanguageCode, IEnumerable<string> supportedLanguageCodes = null)
        {
            _currentLanguageCode = string.IsNullOrWhiteSpace(defaultLanguageCode) ? "zh-Hans" : defaultLanguageCode;
            if (supportedLanguageCodes != null)
            {
                _supportedLanguageCodes.AddRange(supportedLanguageCodes);
            }

            if (_supportedLanguageCodes.Count == 0)
            {
                _supportedLanguageCodes.Add(_currentLanguageCode);
            }

            LastSelection = new LanguageSelectionResult(_currentLanguageCode, LanguageSelectionReason.FallbackService);
        }

        public event Action<string> LanguageChanged;

        public bool IsAvailable => false;
        public string CurrentLanguageCode => _currentLanguageCode;
        public IReadOnlyList<string> SupportedLanguageCodes => _supportedLanguageCodes;
        public LanguageSelectionResult LastSelection { get; private set; }

        public bool SetLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return false;
            }

            if (string.Equals(_currentLanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            _currentLanguageCode = languageCode;
            if (!_supportedLanguageCodes.Contains(languageCode))
            {
                _supportedLanguageCodes.Add(languageCode);
            }

            LastSelection = new LanguageSelectionResult(_currentLanguageCode, LanguageSelectionReason.ExplicitChange);
            LanguageChanged?.Invoke(_currentLanguageCode);
            return true;
        }
    }

    public sealed class NullLocalizedTextService : ILocalizedTextService
    {
        public static readonly NullLocalizedTextService Instance = new();

        private NullLocalizedTextService()
        {
        }

        public bool IsAvailable => false;

        public string Resolve(LocalizationTextRef textRef, params object[] arguments)
        {
            string text = textRef.FallbackPolicy switch
            {
                LocalizationFallbackPolicy.ReturnKey => !string.IsNullOrWhiteSpace(textRef.Entry) ? textRef.Entry : textRef.Fallback,
                LocalizationFallbackPolicy.ReturnEmpty => string.Empty,
                _ => textRef.Fallback
            };

            if (string.IsNullOrEmpty(text) || arguments == null || arguments.Length == 0)
            {
                return text ?? string.Empty;
            }

            try
            {
                return string.Format(CultureInfo.InvariantCulture, text, arguments);
            }
            catch (FormatException)
            {
                return text;
            }
        }
    }
}
