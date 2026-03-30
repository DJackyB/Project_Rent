using UnityEditor;
using UnityEngine;

namespace BaoZuPo.Editor
{
    public static class ChineseFontTools
    {
        [MenuItem("Tools/BaoZuPo/Fonts/Scan And Update Chinese Font Atlas")]
        public static void ScanAndUpdateChineseFontAtlas()
        {
            BaoZuPo.Editor.Localization.LocalizationFontTools.ScanAndUpdateLocalizationFontAtlas();
            Debug.Log("[ChineseFontTools] Redirected to localization font atlas tool.");
        }
    }
}
