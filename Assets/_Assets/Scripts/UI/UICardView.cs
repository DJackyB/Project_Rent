using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.GameFlow;
using BaoZuPo.UI.Common.Drag;
using BaoZuPo.UI.Common.Hover;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class UICardView : MonoBehaviour, IHoverPreviewSource
    {
        [Header("\u53ef\u9009\u573a\u666f\u5f15\u7528")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI typeText;
        public TextMeshProUGUI descText;
        public TextMeshProUGUI statsText;
        public Button cardButton;
        public Image background;

        [Header("\u76ae\u80a4\u914d\u7f6e")]
        [SerializeField] private CardSkinDatabase skinDatabase;

        [Header("\u7f3a\u7701\u989c\u8272")]
        [SerializeField] private Color normalTint = new Color(0.22f, 0.22f, 0.24f, 0.96f);
        [SerializeField] private Color selectedTint = new Color(0.38f, 0.58f, 0.88f, 1f);
        [SerializeField] private Color tenantTint = new Color(0.24f, 0.56f, 0.33f, 0.96f);
        [SerializeField] private Color equipmentTint = new Color(0.59f, 0.43f, 0.20f, 0.96f);
        [SerializeField] private Color eventTint = new Color(0.55f, 0.25f, 0.65f, 0.96f);
        [SerializeField] private Color contractTint = new Color(0.25f, 0.44f, 0.68f, 0.96f);

        private static CardSkinDatabase _cachedSkinDatabase;
        private static bool _skinLookupAttempted;

        private UIHandPanel _handPanel;
        private HoverPreviewTrigger _hoverTrigger;
        private UICardDragHandler _dragHandler;
        private CanvasGroup _canvasGroup;
        private LayoutElement _layoutElement;
        private Image _frameImage;
        private Image _artImage;
        private TextMeshProUGUI _runtimeTypeText;
        private TextMeshProUGUI _runtimeStatsText;
        private bool _selected;

        public CardInstance Card { get; private set; }
        public CardViewContext CurrentContext { get; private set; } = CardViewContext.Hand;

        public GameObject HoverSourceObject => gameObject;
        public RectTransform HoverAnchor => transform as RectTransform;
        public object HoverPayload => Card;

        private void Awake()
        {
            CacheReferences();
            EnsureRuntimeVisuals();
            RefreshPresentation();
        }

        private void OnEnable()
        {
            CacheReferences();
            EnsureRuntimeVisuals();
            RefreshPresentation();
            UpdateHoverTrigger();
            UpdateButtonState();
            UpdateDragHandler();
        }

        private void OnDisable()
        {
            if (_hoverTrigger != null)
            {
                _hoverTrigger.Unbind(this);
            }

            if (_dragHandler != null)
            {
                _dragHandler.Unbind();
            }
        }

        public void Setup(CardInstance card, UIHandPanel panel)
        {
            Setup(card, CardViewContext.Hand, panel);
        }

        public void Setup(CardInstance card, CardViewContext context, UIHandPanel panel = null)
        {
            Card = card;
            CurrentContext = context;
            _handPanel = panel;

            CacheReferences();
            EnsureRuntimeVisuals();
            RefreshPresentation();
            UpdateHoverTrigger();
            UpdateButtonState();
            UpdateDragHandler();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            UpdateBackground();
        }

        private void CacheReferences()
        {
            if (background == null)
            {
                background = GetComponent<Image>();
            }

            if (cardButton == null)
            {
                cardButton = GetComponent<Button>();
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_layoutElement == null)
            {
                _layoutElement = GetComponent<LayoutElement>();
            }
        }

        private void EnsureRuntimeVisuals()
        {
            if (background != null)
            {
                background.raycastTarget = true;
            }

            if (_frameImage == null)
            {
                _frameImage = CreateRuntimeImage("Frame", new Vector2(6f, 6f), new Vector2(-6f, -6f), 0);
            }

            if (_artImage == null)
            {
                _artImage = CreateRuntimeImage("Illustration", new Vector2(14f, 62f), new Vector2(-14f, -82f), 1);
                _artImage.preserveAspect = true;
            }

            if (_runtimeTypeText == null)
            {
                _runtimeTypeText = CreateRuntimeLabel(
                    "TypeLabel",
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(-12f, -12f),
                    new Vector2(118f, 26f),
                    TextAlignmentOptions.TopRight);
            }

            if (_runtimeStatsText == null)
            {
                _runtimeStatsText = CreateRuntimeLabel(
                    "StatsLabel",
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(12f, 12f),
                    new Vector2(180f, 72f),
                    TextAlignmentOptions.BottomLeft);
            }
        }

        private void RefreshPresentation()
        {
            UIFontCatalog.ApplyToChildren(transform);

            if (Card == null || Card.Data == null)
            {
                ClearPresentation();
                return;
            }

            ApplyContextOffsets();
            UpdateBackground();
            UpdateArt();
            UpdateMainText();
            UpdateContextualText();
            UpdateButtonState();
        }

        private void ApplyContextOffsets()
        {
            if (_artImage == null)
            {
                return;
            }

            if (CurrentContext == CardViewContext.RoomEquipment)
            {
                _artImage.rectTransform.offsetMin = new Vector2(10f, 38f);
                _artImage.rectTransform.offsetMax = new Vector2(-10f, -56f);
            }
            else
            {
                _artImage.rectTransform.offsetMin = new Vector2(14f, 62f);
                _artImage.rectTransform.offsetMax = new Vector2(-14f, -82f);
            }
        }

        private void UpdateBackground()
        {
            var skins = ResolveSkinDatabase();
            Sprite faceSprite = skins != null ? skins.GetFaceSprite(Card.Data.cardType) : null;
            Sprite frameSprite = skins != null ? skins.GetFrameSprite(Card.Data.rarity) : null;

            if (background != null)
            {
                background.sprite = faceSprite != null ? faceSprite : GetBuiltinSprite();
                background.type = faceSprite != null ? Image.Type.Simple : Image.Type.Sliced;
                background.color = ResolveFaceTint(faceSprite != null);
            }

            if (_frameImage != null)
            {
                _frameImage.sprite = frameSprite != null ? frameSprite : GetBuiltinSprite();
                _frameImage.type = frameSprite != null ? Image.Type.Simple : Image.Type.Sliced;
                _frameImage.color = frameSprite != null ? Color.white : ResolveRarityTint(Card.Data.rarity);
            }
        }

        private void UpdateArt()
        {
            if (_artImage == null)
            {
                return;
            }

            _artImage.sprite = Card.Data.cardArt;
            _artImage.enabled = Card.Data.cardArt != null;
            _artImage.color = Card.Data.cardArt != null ? Color.white : Color.clear;
        }

        private void UpdateMainText()
        {
            if (nameText != null)
            {
                nameText.text = Card.Data.cardName ?? string.Empty;
                nameText.gameObject.SetActive(true);
            }

            bool showCost = CurrentContext == CardViewContext.Hand || CurrentContext == CardViewContext.HoverPreview;
            if (costText != null)
            {
                costText.gameObject.SetActive(showCost);
                if (showCost)
                {
                    costText.text = UIStrings.Cost(Card.Data.cost);
                }
            }

            bool showDescription = CurrentContext == CardViewContext.Hand || CurrentContext == CardViewContext.HoverPreview || CurrentContext == CardViewContext.Contract;
            if (CurrentContext == CardViewContext.RoomTenant || CurrentContext == CardViewContext.RoomEquipment)
            {
                showDescription = false;
            }

            if (descText != null)
            {
                descText.gameObject.SetActive(showDescription);
                if (showDescription)
                {
                    descText.text = Card.Data.description ?? string.Empty;
                }
            }
        }

        private void UpdateContextualText()
        {
            string typeLabel = GetTypeLabel(Card.Data.cardType);
            bool showType = CurrentContext != CardViewContext.RoomEquipment;

            if (typeText != null)
            {
                typeText.gameObject.SetActive(showType);
                if (showType)
                {
                    typeText.text = typeLabel;
                }
            }

            if (_runtimeTypeText != null)
            {
                bool useRuntimeType = typeText == null && showType;
                _runtimeTypeText.gameObject.SetActive(useRuntimeType);
                if (useRuntimeType)
                {
                    _runtimeTypeText.text = typeLabel;
                }
            }

            string stats = BuildStatsText();
            bool showStats = !string.IsNullOrEmpty(stats);

            if (statsText != null)
            {
                statsText.gameObject.SetActive(showStats);
                if (showStats)
                {
                    statsText.text = stats;
                }
            }

            if (_runtimeStatsText != null)
            {
                bool useRuntimeStats = statsText == null && showStats;
                _runtimeStatsText.gameObject.SetActive(useRuntimeStats);
                if (useRuntimeStats)
                {
                    _runtimeStatsText.text = stats;
                }
            }
        }

        private string BuildStatsText()
        {
            if (Card == null || Card.Data == null)
            {
                return string.Empty;
            }

            var lines = new List<string>(4);

            if (CurrentContext == CardViewContext.RoomTenant || CurrentContext == CardViewContext.HoverPreview)
            {
                if (Card.Data.cardType == CardType.Tenant && Card.Data.baseRent > 0)
                {
                    lines.Add(UIStrings.BaseRent(Card.Data.baseRent));
                }
            }

            if (Card.Data.durability > 0)
            {
                string durabilityLabel = ResolveDurabilityLabel();
                lines.Add($"{durabilityLabel} {Card.CurrentDurability}");
            }

            if (Card.Data.waitTurns > 0)
            {
                lines.Add(UIStrings.Wait(Card.CurrentWait));
            }

            if (CurrentContext == CardViewContext.RoomEquipment)
            {
                return lines.Count > 0 ? string.Join("\n", lines) : string.Empty;
            }

            return lines.Count > 0 ? string.Join("\n", lines) : string.Empty;
        }

        private string ResolveDurabilityLabel()
        {
            if (Card != null && Card.Data != null && Card.Data.cardType == CardType.Tenant)
            {
                if (CurrentContext == CardViewContext.RoomTenant || CurrentContext == CardViewContext.HoverPreview)
                {
                    return UIStrings.Lease;
                }
            }

            return UIStrings.Durability;
        }

        private void UpdateHoverTrigger()
        {
            if (CurrentContext == CardViewContext.RoomEquipment && GetComponent<UIEquipmentCardView>() != null)
            {
                if (_hoverTrigger != null)
                {
                    _hoverTrigger.Unbind(this);
                    _hoverTrigger.enabled = false;
                }

                return;
            }

            bool allowHover = Card != null
                && CurrentContext != CardViewContext.Hand
                && CurrentContext != CardViewContext.HoverPreview;

            if (!allowHover)
            {
                if (_hoverTrigger != null)
                {
                    _hoverTrigger.Unbind(this);
                    _hoverTrigger.enabled = false;
                }

                return;
            }

            if (_hoverTrigger == null)
            {
                _hoverTrigger = GetComponent<HoverPreviewTrigger>();
                if (_hoverTrigger == null)
                {
                    _hoverTrigger = gameObject.AddComponent<HoverPreviewTrigger>();
                }
            }

            _hoverTrigger.enabled = true;
            _hoverTrigger.Bind(this);
        }

        private void UpdateButtonState()
        {
            if (cardButton == null)
            {
                return;
            }

            cardButton.onClick.RemoveAllListeners();
            cardButton.interactable = false;
            cardButton.transition = Selectable.Transition.None;
        }

        private void UpdateDragHandler()
        {
            if (Card == null || CurrentContext != CardViewContext.Hand)
            {
                if (_dragHandler != null)
                {
                    _dragHandler.Unbind();
                }

                return;
            }

            if (_dragHandler == null)
            {
                _dragHandler = GetComponent<UICardDragHandler>();
                if (_dragHandler == null)
                {
                    _dragHandler = gameObject.AddComponent<UICardDragHandler>();
                }
            }

            _dragHandler.Bind(this);
        }

        private void ClearPresentation()
        {
            if (nameText != null) nameText.text = string.Empty;
            if (costText != null) costText.text = string.Empty;
            if (typeText != null) typeText.text = string.Empty;
            if (descText != null) descText.text = string.Empty;
            if (statsText != null) statsText.text = string.Empty;
            if (_runtimeTypeText != null) _runtimeTypeText.text = string.Empty;
            if (_runtimeStatsText != null) _runtimeStatsText.text = string.Empty;

            if (_artImage != null)
            {
                _artImage.sprite = null;
                _artImage.enabled = false;
            }

            if (background != null)
            {
                background.sprite = GetBuiltinSprite();
                background.type = Image.Type.Sliced;
                background.color = normalTint;
            }

            if (_frameImage != null)
            {
                _frameImage.sprite = GetBuiltinSprite();
                _frameImage.type = Image.Type.Sliced;
                _frameImage.color = ResolveRarityTint(CardRarity.Common);
            }
        }

        private Image CreateRuntimeImage(string name, Vector2 offsetMin, Vector2 offsetMax, int siblingIndex)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var image = go.GetComponent<Image>();
            image.sprite = GetBuiltinSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;

            go.transform.SetSiblingIndex(siblingIndex);
            return image;
        }

        private TextMeshProUGUI CreateRuntimeLabel(
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMax;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var label = go.GetComponent<TextMeshProUGUI>();
            label.font = ResolveFont();
            label.fontSize = 18f;
            label.color = Color.white;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        private Color ResolveFaceTint(bool hasSprite)
        {
            if (_selected)
            {
                return hasSprite ? Color.Lerp(Color.white, selectedTint, 0.35f) : selectedTint;
            }

            if (hasSprite)
            {
                return Color.white;
            }

            return Card != null && Card.Data != null ? ResolveTypeTint(Card.Data.cardType) : normalTint;
        }

        private Color ResolveTypeTint(CardType cardType)
        {
            return cardType switch
            {
                CardType.Tenant => tenantTint,
                CardType.Equipment => equipmentTint,
                CardType.Event => eventTint,
                CardType.Contract => contractTint,
                _ => normalTint
            };
        }

        private static Color ResolveRarityTint(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Rare => new Color(0.35f, 0.63f, 1f, 0.30f),
                CardRarity.Epic => new Color(0.70f, 0.35f, 1f, 0.32f),
                CardRarity.Legendary => new Color(1f, 0.82f, 0.28f, 0.34f),
                _ => new Color(0.85f, 0.85f, 0.88f, 0.18f)
            };
        }

        private CardSkinDatabase ResolveSkinDatabase()
        {
            if (skinDatabase != null)
            {
                return skinDatabase;
            }

            if (_skinLookupAttempted)
            {
                return _cachedSkinDatabase;
            }

            _cachedSkinDatabase = Resources.Load<CardSkinDatabase>("CardSkinDatabase");
            if (_cachedSkinDatabase == null)
            {
                var all = Resources.LoadAll<CardSkinDatabase>(string.Empty);
                if (all != null && all.Length > 0)
                {
                    _cachedSkinDatabase = all[0];
                }
            }

            _skinLookupAttempted = true;
            return _cachedSkinDatabase;
        }

        private static string GetTypeLabel(CardType cardType)
        {
            return UIStrings.TypeLabel(cardType);
        }

        private static Sprite GetBuiltinSprite()
        {
            return UIRuntimeSpriteUtility.GetWhiteSprite();
        }

        private static TMP_FontAsset ResolveFont()
        {
            return UIFontCatalog.GetPreferredFontAsset();
        }
    }
}
