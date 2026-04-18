using TMPro;
using UnityEngine;

namespace Martian.Localization
{
    /// <summary>
    /// 本地化 TextMeshPro 标签。为 TMP_Text 提供自动本地化支持。
    ///
    /// 职责：
    /// - 在 Inspector 中配置文本表、键和备选值
    /// - 缓存目标 TMP_Text 组件
    /// - 监听语言改变事件，自动更新文本
    /// - 自动应用语言特定的字体
    ///
    /// 使用流程：
    /// 1. 将此组件添加到含有 TMP_Text 的 GameObject
    ///    - 若该 GameObject 上有 TMP_Text，自动关联
    ///    - 否则需在 Inspector 手动设置 targetText
    /// 2. 设置 table（文本表名，默认 "UI"）和 entry（键）
    /// 3. 设置 fallback（备选文本）和 fallbackPolicy（备选策略）
    /// 4. 运行时自动显示本地化文本
    /// 5. 语言改变时自动刷新
    /// </summary>
    [DisallowMultipleComponent]
    public class LocalizedTMPLabel : MonoBehaviour
    {
        /// <summary>目标 TMP_Text 组件。若为 null，会尝试从同 GameObject 上获取。</summary>
        [SerializeField] private TMP_Text targetText;

        /// <summary>文本所属的表名称（如 "UI", "Card" 等）。</summary>
        [SerializeField] private string table = "UI";

        /// <summary>文本条目的键。用于从文本表中查询本地化文本。</summary>
        [SerializeField] private string entry;

        /// <summary>备选文本。当本地化文本不可用时使用此备选值。</summary>
        [TextArea(1, 3)]
        [SerializeField] private string fallback;

        /// <summary>备选策略。决定备选文本的处理方式。</summary>
        [SerializeField] private LocalizationFallbackPolicy fallbackPolicy = LocalizationFallbackPolicy.UseFallback;

        /// <summary>
        /// Awake：缓存目标 Text 组件。
        /// 若 Inspector 中未设置，尝试从同 GameObject 上获取。
        /// </summary>
        private void Awake()
        {
            if (targetText == null)
            {
                targetText = GetComponent<TMP_Text>();
            }
        }

        /// <summary>
        /// OnEnable：订阅语言改变事件，并刷新文本。
        /// </summary>
        private void OnEnable()
        {
            LocalizationServices.Language.LanguageChanged += OnLanguageChanged;
            Refresh();
        }

        /// <summary>
        /// OnDisable：取消订阅语言改变事件。
        /// </summary>
        private void OnDisable()
        {
            LocalizationServices.Language.LanguageChanged -= OnLanguageChanged;
        }

        /// <summary>
        /// 刷新文本。
        /// 流程：
        /// 1. 调用 LocalizationServices.Resolve() 获取本地化文本
        /// 2. 设置目标 Text 的文本内容
        /// 3. 应用语言特定的字体
        /// </summary>
        public void Refresh()
        {
            if (targetText == null)
            {
                return;
            }

            // 通过 LocalizationServices 解析本地化文本
            targetText.text = LocalizationServices.Resolve(new LocalizationTextRef(table, entry, fallback, fallbackPolicy));

            // 应用当前语言的字体
            LocalizationFontUtility.ApplyToText(targetText);
        }

        /// <summary>
        /// 语言改变事件处理。自动调用 Refresh()。
        /// </summary>
        private void OnLanguageChanged(string _)
        {
            Refresh();
        }
    }
}
