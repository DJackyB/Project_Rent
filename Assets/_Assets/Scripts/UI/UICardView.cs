using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.GameFlow;
using BaoZuPo.Integration.Martian.Tooltip;
using BaoZuPo.UI.Common.Drag;
using Martian.Tooltip;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class UICardView : MonoBehaviour, ITooltipContentProvider
    {
        [Header("Main Text References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI descText;

        [Header("Badge References")]
        [SerializeField] private GameObject typeBadgeRoot;
        [SerializeField] private GameObject durabilityBadgeRoot;
        [SerializeField] private GameObject waitBadgeRoot;

        [Header("Component References")]
        [SerializeField] private Button cardButton;
        [SerializeField] private Image background;

        [Header("Prefab Visual References")]
        [SerializeField] private Image frameImage;
        [SerializeField] private Image artImage;

        [Header("Skin Configuration")]
        [SerializeField] private CardSkinDatabase skinDatabase;

        [Header("Fallback Colors")]
        [SerializeField] private Color normalTint = new Color(0.22f, 0.22f, 0.24f, 0.96f);
        [SerializeField] private Color selectedTint = new Color(0.38f, 0.58f, 0.88f, 1f);
        [SerializeField] private Color tenantTint = new Color(0.24f, 0.56f, 0.33f, 0.96f);
        [SerializeField] private Color equipmentTint = new Color(0.59f, 0.43f, 0.20f, 0.96f);
        [SerializeField] private Color eventTint = new Color(0.55f, 0.25f, 0.65f, 0.96f);
        [SerializeField] private Color contractTint = new Color(0.25f, 0.44f, 0.68f, 0.96f);

        private static CardSkinDatabase _cachedSkinDatabase;
        private static bool _skinLookupAttempted;

        private TextMeshProUGUI typeText;
        private TextMeshProUGUI durabilityText;
        private TextMeshProUGUI waitText;
        private TooltipTrigger _tooltipTrigger;
        private UICardDragHandler _dragHandler;
        private bool _selected;
        private bool _loggedMissingVisualReferences;
        private bool _loggedMissingTooltipTrigger;
        private bool _loggedMissingDragHandler;

        public CardInstance Card { get; private set; }
        public CardViewContext CurrentContext { get; private set; } = CardViewContext.Hand;
        public RectTransform HoverAnchor => transform as RectTransform;

        private void Awake()
        {
            CacheReferences();
            ValidateVisualConfiguration();
            RefreshPresentation();
        }

        private void OnEnable()
        {
            CacheReferences();
            ValidateVisualConfiguration();
            RefreshPresentation();
            UpdateTooltipTrigger();
            UpdateButtonState();
            UpdateDragHandler();
        }

        private void OnDisable()
        {
            DisableTooltipTrigger();
            _dragHandler?.Unbind();
        }

        public void Setup(CardInstance card, UIHandPanel panel)
        {
            Setup(card, CardViewContext.Hand, panel);
        }

        public void Setup(CardInstance card, CardViewContext context, UIHandPanel panel = null)
        {
            Card = card;
            CurrentContext = context;

            CacheReferences();
            ValidateVisualConfiguration();
            RefreshPresentation();
            UpdateTooltipTrigger();
            UpdateButtonState();
            UpdateDragHandler();
        }

        public bool TryBuildTooltipRequest(out TooltipRequest request)
        {
            request = null;
            if (!isActiveAndEnabled || Card == null || CurrentContext == CardViewContext.TooltipPreview)
            {
                return false;
            }

            var anchor = transform as RectTransform;
            if (anchor == null)
            {
                return false;
            }

            request = new TooltipRequest(
                this,
                anchor,
                new TooltipContent(
                    BaoZuPoTooltipContentIds.CardPreview,
                    Card,
                    Card.Data != null ? Card.Data.cardName : null));
            return true;
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

            if (_tooltipTrigger == null)
            {
                _tooltipTrigger = GetComponent<TooltipTrigger>();
            }

            if (_dragHandler == null)
            {
                _dragHandler = GetComponent<UICardDragHandler>();
            }

            if (background != null)
            {
                background.raycastTarget = true;
            }

            typeText = ResolveBadgeText(typeBadgeRoot, typeText);
            durabilityText = ResolveBadgeText(durabilityBadgeRoot, durabilityText);
            waitText = ResolveBadgeText(waitBadgeRoot, waitText);

            if (typeText != null)
            {
                typeText.raycastTarget = false;
            }

            if (durabilityText != null)
            {
                durabilityText.raycastTarget = false;
            }

            if (waitText != null)
            {
                waitText.raycastTarget = false;
            }

            if (nameText != null)
            {
                nameText.raycastTarget = false;
            }

            if (costText != null)
            {
                costText.raycastTarget = false;
            }

            if (descText != null)
            {
                descText.raycastTarget = false;
            }

            if (typeBadgeRoot != null)
            {
                DisableBadgeRaycast(typeBadgeRoot);
            }

            if (durabilityBadgeRoot != null)
            {
                DisableBadgeRaycast(durabilityBadgeRoot);
            }

            if (waitBadgeRoot != null)
            {
                DisableBadgeRaycast(waitBadgeRoot);
            }

            if (frameImage != null)
            {
                frameImage.raycastTarget = false;
            }

            if (artImage != null)
            {
                artImage.raycastTarget = false;
            }
        }

        private void ValidateVisualConfiguration()
        {
            if (_loggedMissingVisualReferences)
            {
                return;
            }

            var missing = new List<string>(13);
            if (nameText == null) missing.Add(nameof(nameText));
            if (costText == null) missing.Add(nameof(costText));
            if (typeBadgeRoot == null) missing.Add(nameof(typeBadgeRoot));
            if (typeBadgeRoot != null && typeText == null) missing.Add($"{nameof(typeBadgeRoot)} child TMP");
            if (descText == null) missing.Add(nameof(descText));
            if (durabilityBadgeRoot == null) missing.Add(nameof(durabilityBadgeRoot));
            if (durabilityBadgeRoot != null && durabilityText == null) missing.Add($"{nameof(durabilityBadgeRoot)} child TMP");
            if (waitBadgeRoot == null) missing.Add(nameof(waitBadgeRoot));
            if (waitBadgeRoot != null && waitText == null) missing.Add($"{nameof(waitBadgeRoot)} child TMP");
            if (cardButton == null) missing.Add(nameof(cardButton));
            if (background == null) missing.Add(nameof(background));
            if (frameImage == null) missing.Add(nameof(frameImage));
            if (artImage == null) missing.Add(nameof(artImage));

            if (missing.Count == 0)
            {
                return;
            }

            _loggedMissingVisualReferences = true;
            Debug.LogError(
                $"[UICardView] Card prefab is missing references: {string.Join(", ", missing)}. " +
                "Please configure Card.prefab instead of relying on runtime-generated UI.",
                this);
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
            if (artImage == null)
            {
                return;
            }

            if (CurrentContext == CardViewContext.RoomEquipment)
            {
                artImage.rectTransform.offsetMin = new Vector2(10f, 38f);
                artImage.rectTransform.offsetMax = new Vector2(-10f, -56f);
            }
            else
            {
                artImage.rectTransform.offsetMin = new Vector2(14f, 62f);
                artImage.rectTransform.offsetMax = new Vector2(-14f, -82f);
            }
        }

        private void UpdateBackground()
        {
            if (Card == null || Card.Data == null)
            {
                return;
            }

            var skins = ResolveSkinDatabase();
            Sprite faceSprite = skins != null ? skins.GetFaceSprite(Card.Data.cardType) : null;
            Sprite frameSprite = skins != null ? skins.GetFrameSprite(Card.Data.rarity) : null;

            if (background != null)
            {
                background.sprite = faceSprite != null ? faceSprite : GetBuiltinSprite();
                background.type = faceSprite != null ? Image.Type.Simple : Image.Type.Sliced;
                background.color = ResolveFaceTint(faceSprite != null);
            }

            if (frameImage != null)
            {
                frameImage.sprite = frameSprite != null ? frameSprite : GetBuiltinSprite();
                frameImage.type = frameSprite != null ? Image.Type.Simple : Image.Type.Sliced;
                frameImage.color = frameSprite != null ? Color.white : ResolveRarityTint(Card.Data.rarity);
            }
        }

        private void UpdateArt()
        {
            if (artImage == null || Card == null || Card.Data == null)
            {
                return;
            }

            artImage.sprite = Card.Data.cardArt;
            artImage.enabled = Card.Data.cardArt != null;
            artImage.color = Card.Data.cardArt != null ? Color.white : Color.clear;
        }

        private void UpdateMainText()
        {
            if (Card == null || Card.Data == null)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = Card.Data.cardName ?? string.Empty;
                nameText.gameObject.SetActive(true);
            }

            bool showCost = CurrentContext == CardViewContext.Hand || CurrentContext == CardViewContext.TooltipPreview;
            if (costText != null)
            {
                costText.gameObject.SetActive(showCost);
                if (showCost)
                {
                    costText.text = UIStrings.Cost(Card.Data.cost);
                }
            }

            bool showDescription = CurrentContext == CardViewContext.Hand
                || CurrentContext == CardViewContext.TooltipPreview
                || CurrentContext == CardViewContext.Contract;

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
            if (Card == null || Card.Data == null)
            {
                return;
            }

            string typeLabel = GetTypeLabel(Card.Data.cardType);
            bool showType = CurrentContext != CardViewContext.RoomEquipment;
            SetBadge(typeBadgeRoot, typeText, showType, typeLabel);

            string durability = BuildDurabilityText();
            bool showDurability = !string.IsNullOrEmpty(durability);
            SetBadge(durabilityBadgeRoot, durabilityText, showDurability, durability);

            string wait = BuildWaitText();
            bool showWait = !string.IsNullOrEmpty(wait);
            SetBadge(waitBadgeRoot, waitText, showWait, wait);
        }

        private string BuildDurabilityText()
        {
            if (Card == null || Card.Data == null || Card.Data.durability <= 0)
            {
                return string.Empty;
            }

            string durabilityLabel = ResolveDurabilityLabel();
            return $"{durabilityLabel} {Card.CurrentDurability}";
        }

        private string BuildWaitText()
        {
            if (Card == null || Card.Data == null || Card.Data.waitTurns <= 0)
            {
                return string.Empty;
            }

            return UIStrings.Wait(Card.CurrentWait);
        }

        private string ResolveDurabilityLabel()
        {
            if (Card != null
                && Card.Data != null
                && Card.Data.cardType == CardType.Tenant
                && (CurrentContext == CardViewContext.RoomTenant || CurrentContext == CardViewContext.TooltipPreview))
            {
                return UIStrings.Lease;
            }

            return UIStrings.Durability;
        }

        private void UpdateTooltipTrigger()
        {
            bool allowTooltip = Card != null && CurrentContext != CardViewContext.TooltipPreview;
            if (!allowTooltip)
            {
                DisableTooltipTrigger();
                return;
            }

            if (_tooltipTrigger == null)
            {
                LogMissingTooltipTriggerOnce();
                return;
            }

            _tooltipTrigger.enabled = true;
            _tooltipTrigger.Bind(this);
        }

        private void DisableTooltipTrigger()
        {
            if (_tooltipTrigger == null)
            {
                return;
            }

            _tooltipTrigger.Unbind(this);
            _tooltipTrigger.enabled = false;
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
                _dragHandler?.Unbind();
                return;
            }

            if (_dragHandler == null)
            {
                LogMissingDragHandlerOnce();
                return;
            }

            _dragHandler.Bind(this);
        }

        private void ClearPresentation()
        {
            if (nameText != null)
            {
                nameText.text = string.Empty;
            }

            if (costText != null)
            {
                costText.text = string.Empty;
                costText.gameObject.SetActive(false);
            }

            if (typeText != null)
            {
                typeText.text = string.Empty;
            }
            HideBadge(typeBadgeRoot, typeText);

            if (descText != null)
            {
                descText.text = string.Empty;
                descText.gameObject.SetActive(false);
            }

            if (durabilityText != null)
            {
                durabilityText.text = string.Empty;
            }
            HideBadge(durabilityBadgeRoot, durabilityText);

            if (waitText != null)
            {
                waitText.text = string.Empty;
            }
            HideBadge(waitBadgeRoot, waitText);

            if (artImage != null)
            {
                artImage.sprite = null;
                artImage.enabled = false;
            }

            if (background != null)
            {
                background.sprite = GetBuiltinSprite();
                background.type = Image.Type.Sliced;
                background.color = normalTint;
            }

            if (frameImage != null)
            {
                frameImage.sprite = GetBuiltinSprite();
                frameImage.type = Image.Type.Sliced;
                frameImage.color = ResolveRarityTint(CardRarity.Common);
            }
        }

        private void LogMissingTooltipTriggerOnce()
        {
            if (_loggedMissingTooltipTrigger)
            {
                return;
            }

            _loggedMissingTooltipTrigger = true;
            Debug.LogError(
                "[UICardView] Tooltip requires TooltipTrigger on Card.prefab. " +
                "Please configure the prefab instead of relying on AddComponent.",
                this);
        }

        private void LogMissingDragHandlerOnce()
        {
            if (_loggedMissingDragHandler)
            {
                return;
            }

            _loggedMissingDragHandler = true;
            Debug.LogError(
                "[UICardView] Hand drag requires UICardDragHandler on Card.prefab. " +
                "Please configure the prefab instead of relying on AddComponent.",
                this);
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

        private static void SetBadge(GameObject badgeRoot, TextMeshProUGUI badgeText, bool visible, string value)
        {
            GameObject target = badgeRoot != null ? badgeRoot : badgeText != null ? badgeText.gameObject : null;
            if (target != null)
            {
                target.SetActive(visible);
            }

            if (badgeText != null)
            {
                badgeText.text = visible ? value : string.Empty;
            }
        }

        private static void HideBadge(GameObject badgeRoot, TextMeshProUGUI badgeText)
        {
            SetBadge(badgeRoot, badgeText, false, string.Empty);
        }

        private static void DisableBadgeRaycast(GameObject badgeRoot)
        {
            Image badgeImage = badgeRoot.GetComponent<Image>();
            if (badgeImage != null)
            {
                badgeImage.raycastTarget = false;
            }
        }

        private TextMeshProUGUI ResolveBadgeText(GameObject badgeRoot, TextMeshProUGUI currentText)
        {
            if (badgeRoot == null)
            {
                return null;
            }

            if (currentText != null && currentText.transform.IsChildOf(badgeRoot.transform))
            {
                return currentText;
            }

            TextMeshProUGUI[] texts = badgeRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI resolved = null;
            int childTextCount = 0;

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null || texts[i].gameObject == badgeRoot)
                {
                    continue;
                }

                childTextCount++;
                if (resolved == null)
                {
                    resolved = texts[i];
                }
            }

            if (childTextCount == 1)
            {
                return resolved;
            }

            if (!_loggedMissingVisualReferences)
            {
                Debug.LogError(
                    $"[UICardView] '{badgeRoot.name}' must contain exactly one child TextMeshProUGUI, but found {childTextCount}.",
                    this);
            }

            return null;
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
    }
}
