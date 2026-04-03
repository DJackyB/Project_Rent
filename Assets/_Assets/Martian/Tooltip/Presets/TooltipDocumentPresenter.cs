using System.Linq;
using Martian.Tooltip.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Martian.Tooltip.Presets
{
    public sealed class TooltipDocumentPresenter : MonoBehaviour, ITooltipPresenter
    {
        private static readonly Color PanelColor = new(0.08f, 0.09f, 0.12f, 0.94f);
        private static readonly Color AccentColor = new(0.78f, 0.88f, 1f, 1f);
        private static readonly Color BodyColor = new(0.93f, 0.95f, 0.98f, 1f);
        private static readonly Color TagColor = new(0.40f, 0.72f, 1f, 1f);
        private static readonly Color SectionColor = new(0.65f, 0.78f, 0.92f, 1f);

        private RectTransform _root;
        private GameObject _panel;

        public RectTransform Root => _root;

        public void Show(TooltipRequest request)
        {
            Hide();

            if (request == null || request.Content == null || request.Content.ContentId != TooltipDocumentContentIds.Document)
            {
                return;
            }

            if (request.Content.Payload is not TooltipDocument document)
            {
                return;
            }

            BuildView(document);
        }

        public void Hide()
        {
            if (_panel == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_panel);
            }
            else
            {
                DestroyImmediate(_panel);
            }

            _panel = null;
            _root = null;
        }

        private void BuildView(TooltipDocument document)
        {
            _panel = new GameObject("TooltipDocumentPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(VerticalLayoutGroup));
            _panel.transform.SetParent(transform, false);
            _root = _panel.GetComponent<RectTransform>();

            _root.anchorMin = new Vector2(0f, 0f);
            _root.anchorMax = new Vector2(0f, 0f);
            _root.pivot = new Vector2(0f, 0f);
            _root.sizeDelta = new Vector2(340f, 220f);

            var canvasGroup = _panel.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            var image = _panel.GetComponent<Image>();
            image.sprite = TooltipSpriteUtility.WhiteSprite;
            image.type = Image.Type.Sliced;
            image.color = PanelColor;
            image.raycastTarget = false;

            var layout = _panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            AddText(document.Title, 24f, FontStyles.Bold, AccentColor);

            if (!string.IsNullOrWhiteSpace(document.Subtitle))
            {
                AddText(document.Subtitle, 16f, FontStyles.Italic, BodyColor);
            }

            if (!string.IsNullOrWhiteSpace(document.Summary))
            {
                AddText(document.Summary, 15f, FontStyles.Normal, BodyColor);
            }

            if (document.Tags != null && document.Tags.Count > 0)
            {
                AddText(string.Join("  ", document.Tags.Select(tag => $"#{tag}")), 13f, FontStyles.Normal, TagColor);
            }

            if (document.Sections != null)
            {
                foreach (var section in document.Sections)
                {
                    if (section == null)
                    {
                        continue;
                    }

                    AddText(section.Header, 15f, FontStyles.Bold, SectionColor);

                    if (section.Rows == null)
                    {
                        continue;
                    }

                    foreach (var row in section.Rows)
                    {
                        if (row == null)
                        {
                            continue;
                        }

                        string rowText = string.IsNullOrWhiteSpace(row.Label)
                            ? row.Value ?? string.Empty
                            : $"{row.Label}: {row.Value}";
                        AddText(rowText, 14f, row.Highlight ? FontStyles.Bold : FontStyles.Normal, BodyColor);
                    }
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
        }

        private void AddText(string value, float fontSize, FontStyles style, Color color)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(_panel.transform, false);

            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = value ?? string.Empty;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.Left;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            label.font = TMP_Settings.defaultFontAsset;

            var layoutElement = textObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = fontSize + 4f;
        }
    }
}

internal static class TooltipSpriteUtility
{
    private static Sprite _whiteSprite;

    public static Sprite WhiteSprite
    {
        get
        {
            if (_whiteSprite != null)
            {
                return _whiteSprite;
            }

            var texture = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            _whiteSprite.name = "TooltipWhiteSprite";
            return _whiteSprite;
        }
    }
}
