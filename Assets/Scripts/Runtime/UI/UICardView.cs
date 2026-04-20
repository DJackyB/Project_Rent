using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.GameFlow;
using BaoZuPo.Integration.Martian.Tooltip;
using BaoZuPo.UI.Common.Drag;
using DG.Tweening;
using Martian.Localization;
using Martian.Tooltip;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 卡牌视图，负责渲染单张卡牌的所有视觉元素和信息。
    /// 从 CardInstance 获取数据并在不同上下文（手牌、房间租户、装备、合约、奖励、预览）下显示。
    /// 管理卡牌皮肤、徽章（类型、耐久度、等待）、文本内容和视觉效果。
    /// 实现拖拽源接口，在手牌上下文时允许被拖动。
    /// 实现提示内容提供者接口，支持悬停预览。
    /// </summary>
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class UICardView : MonoBehaviour, ITooltipContentProvider
    {
        [Header("Main Text References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private GameObject costBadgeRoot;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private GameObject rentBadgeRoot;
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

        [Header("Affix Tags")]
        [SerializeField] private Transform _affixTagContainer;
        [SerializeField] private GameObject _affixTagPrefab;

        [Header("Skin Configuration")]
        [SerializeField] private CardSkinDatabase skinDatabase;

        [Header("Interaction Visuals")]
        [SerializeField] private Color hoverFaceTint = new Color(1f, 0.97f, 0.9f, 1f);
        [SerializeField] private Color hoverFrameTint = new Color(1f, 0.93f, 0.68f, 1f);
        [SerializeField] private Color dragFaceTint = new Color(0.9f, 0.98f, 1f, 1f);
        [SerializeField] private Color dragFrameTint = new Color(0.42f, 0.92f, 0.9f, 1f);
        [SerializeField] private Color invalidFlashTint = new Color(1f, 0.56f, 0.5f, 1f);
        [SerializeField] private Color hoverNameTint = new Color(1f, 0.98f, 0.84f, 1f);
        [SerializeField] private Color dragNameTint = new Color(0.88f, 1f, 0.98f, 1f);
        [SerializeField] private float hoverArtScale = 1.025f;
        [SerializeField] private float dragArtScale = 1.07f;
        [SerializeField] private float visualTransitionSeconds = 0.14f;
        [SerializeField] private float invalidFlashSeconds = 0.08f;
        [SerializeField] private float invalidPunchScale = 0.08f;

        private TextMeshProUGUI typeText;
        private TextMeshProUGUI rentText;
        private TextMeshProUGUI durabilityText;
        private TextMeshProUGUI waitText;
        private TooltipTrigger _tooltipTrigger;
        private UICardDragHandler _dragHandler;
        private bool _selected;
        private bool _loggedMissingVisualReferences;
        private bool _loggedMissingTooltipTrigger;
        private bool _loggedMissingDragHandler;
        private bool _initialVisualStateCached;
        private bool _dragging;
        private ILanguageService _subscribedLanguageService;
        private Sprite _initialBackgroundSprite;
        private Image.Type _initialBackgroundType;
        private Color _initialBackgroundColor;
        private Sprite _initialFrameSprite;
        private Image.Type _initialFrameType;
        private Color _initialFrameColor;
        private Sprite _initialArtSprite;
        private Color _initialArtColor;
        private bool _initialArtEnabled;
        private Vector3 _initialArtScale;
        private Color _initialNameColor;
        private Color _initialCostColor;
        private Color _initialDescColor;
        private Tween _backgroundColorTween;
        private Tween _frameColorTween;
        private Tween _artScaleTween;
        private Tween _nameColorTween;
        private Tween _costColorTween;
        private Tween _descColorTween;
        private Tween _invalidFramePunchTween;
        private Tween _invalidBackgroundPunchTween;
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
            SubscribeToLanguageChanges();
            CacheReferences();
            ValidateVisualConfiguration();
            RefreshPresentation();
            UpdateTooltipTrigger();
            UpdateButtonState();
            UpdateDragHandler();
        }

        private void OnDisable()
        {
            UnsubscribeFromLanguageChanges();
            DisableTooltipTrigger();
            _dragHandler?.Unbind();
            StopInteractionTweens();
            KillAllVisualTweenTargets();
            ResetInteractionVisualState();
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
            RefreshViewState();
        }

        public void RefreshViewState()
        {
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
                    CardText.Name(Card.Data)));
            return true;
        }

        public void SetSelected(bool selected, bool immediate = false)
        {
            _selected = selected;
            ApplyInteractionVisualState(immediate);
        }

        public void SetDragging(bool dragging, bool immediate = false)
        {
            _dragging = dragging;
            ApplyInteractionVisualState(immediate);
        }

        public void PlayRejectedInteractionFeedback()
        {
            StopInteractionTweens();
            if (frameImage != null)
            {
                frameImage.color = invalidFlashTint;
                _frameColorTween = frameImage.DOColor(ResolveFrameTint(), invalidFlashSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
                _invalidFramePunchTween = frameImage.rectTransform
                    .DOPunchScale(Vector3.one * invalidPunchScale, invalidFlashSeconds * 2f, 8, 0.7f)
                    .SetUpdate(true).SetLink(gameObject);
            }

            if (background != null)
            {
                background.color = Color.Lerp(ResolveFaceTint(), invalidFlashTint, 0.5f);
                _backgroundColorTween = background.DOColor(ResolveFaceTint(), invalidFlashSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
                _invalidBackgroundPunchTween = background.rectTransform
                    .DOPunchScale(Vector3.one * (invalidPunchScale * 0.45f), invalidFlashSeconds * 2f, 8, 0.7f)
                    .SetUpdate(true).SetLink(gameObject);
            }
        }

        public void PlayCommitInteractionFeedback()
        {
            if (frameImage != null)
            {
                frameImage.rectTransform.DOKill(false);
                frameImage.rectTransform.DOPunchScale(Vector3.one * 0.1f, 0.18f, 8, 0.65f).SetUpdate(true).SetLink(gameObject);
            }

            if (artImage != null)
            {
                artImage.rectTransform.DOKill(false);
                artImage.rectTransform.DOPunchScale(Vector3.one * 0.06f, 0.18f, 8, 0.55f).SetUpdate(true).SetLink(gameObject);
            }

        }

        public void RefreshDragLayoutBaseline(bool snapToIdle = true, bool preserveIdleMotion = false)
        {
            _dragHandler?.RefreshLayoutBaseline(snapToIdle, preserveIdleMotion);
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

            rentText = ResolveBadgeText(rentBadgeRoot, rentText);
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

            if (rentText != null)
            {
                rentText.raycastTarget = false;
            }

            if (descText != null)
            {
                descText.raycastTarget = false;
            }

            if (typeBadgeRoot != null)
            {
                DisableBadgeRaycast(typeBadgeRoot);
            }

            if (costBadgeRoot != null)
            {
                DisableBadgeRaycast(costBadgeRoot);
            }

            if (rentBadgeRoot != null)
            {
                DisableBadgeRaycast(rentBadgeRoot);
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

            CacheInitialVisualState();
        }

        private void ValidateVisualConfiguration()
        {
            if (_loggedMissingVisualReferences)
            {
                return;
            }

            var missing = new List<string>(13);
            if (nameText == null) missing.Add(nameof(nameText));
            if (costBadgeRoot == null) missing.Add(nameof(costBadgeRoot));
            if (costText == null) missing.Add(nameof(costText));
            if (rentBadgeRoot == null) missing.Add(nameof(rentBadgeRoot));
            if (rentBadgeRoot != null && rentText == null) missing.Add($"{nameof(rentBadgeRoot)} child TMP");
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
            if (skinDatabase == null) missing.Add(nameof(skinDatabase));

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
            if (Card == null || Card.Data == null)
            {
                ClearPresentation();
                return;
            }

            UpdateBackground();
            UpdateArt();
            UpdateMainText();
            UpdateAffixTags();
            UpdateContextualText();
            UpdateButtonState();
            ApplyInteractionVisualState(true);
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
                background.sprite = faceSprite != null ? faceSprite : skins != null ? skins.DefaultFaceSprite : _initialBackgroundSprite;
                background.type = _initialBackgroundType;
                background.color = _initialBackgroundColor;
            }

            if (frameImage != null)
            {
                frameImage.sprite = frameSprite != null ? frameSprite : skins != null ? skins.DefaultFrameSprite : _initialFrameSprite;
                frameImage.type = _initialFrameType;
                frameImage.color = _initialFrameColor;
            }
        }

        private void UpdateArt()
        {
            if (artImage == null || Card == null || Card.Data == null)
            {
                return;
            }

            if (Card.Data.cardArt != null)
            {
                artImage.sprite = Card.Data.cardArt;
                artImage.enabled = true;
                artImage.color = Color.white;
                return;
            }

            artImage.sprite = _initialArtSprite;
            artImage.enabled = _initialArtEnabled;
            artImage.color = _initialArtColor;
        }

        private void UpdateMainText()
        {
            if (Card == null || Card.Data == null)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = CardText.Name(Card.Data);
                nameText.gameObject.SetActive(true);
            }

            bool showCost = ShouldShowCost();
            SetBadge(costBadgeRoot, costText, showCost, showCost ? GameText.Cost(Card.Data.cost) : string.Empty);

            bool showRent = ShouldShowRent();
            SetBadge(rentBadgeRoot, rentText, showRent, showRent ? BuildRentText() : string.Empty);

            bool showDescription = CurrentContext == CardViewContext.Hand
                || CurrentContext == CardViewContext.TooltipPreview
                || CurrentContext == CardViewContext.Contract
                || CurrentContext == CardViewContext.RoomTenant
                || CurrentContext == CardViewContext.RoomEquipment
                || CurrentContext == CardViewContext.RewardPick;

            if (descText != null)
            {
                descText.gameObject.SetActive(showDescription);
                if (showDescription)
                {
                    descText.text = CardText.Description(Card.Data);
                }
            }
        }

        private void UpdateAffixTags()
        {
            if (_affixTagContainer == null || _affixTagPrefab == null)
            {
                return;
            }

            foreach (Transform child in _affixTagContainer)
            {
                Destroy(child.gameObject);
            }

            if (Card == null || Card.Data == null || Card.Data.tags == null)
            {
                return;
            }

            foreach (var affix in Card.Data.tags)
            {
                var tag = Instantiate(_affixTagPrefab, _affixTagContainer);
                var label = tag.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = affix.ToString();
                }
            }
        }

        private bool ShouldShowCost()
        {
            return CurrentContext == CardViewContext.Hand
                || CurrentContext == CardViewContext.TooltipPreview
                || CurrentContext == CardViewContext.RewardPick;
        }

        private bool ShouldShowRent()
        {
            if (Card == null || Card.Data == null || Card.Data.cardType != CardType.Tenant || Card.Data.baseRent <= 0)
            {
                return false;
            }

            return CurrentContext == CardViewContext.Hand
                || CurrentContext == CardViewContext.RoomTenant
                || CurrentContext == CardViewContext.TooltipPreview;
        }

        private string BuildRentText()
        {
            if (Card == null || Card.Data == null || Card.Data.cardType != CardType.Tenant || Card.Data.baseRent <= 0)
            {
                return string.Empty;
            }

            return Card.Data.baseRent.ToString();
        }

        private void SubscribeToLanguageChanges()
        {
            ILanguageService languageService = LocalizationServices.Language;
            if (ReferenceEquals(_subscribedLanguageService, languageService) || languageService == null)
            {
                return;
            }

            UnsubscribeFromLanguageChanges();
            _subscribedLanguageService = languageService;
            _subscribedLanguageService.LanguageChanged += HandleLanguageChanged;
        }

        private void UnsubscribeFromLanguageChanges()
        {
            if (_subscribedLanguageService == null)
            {
                return;
            }

            _subscribedLanguageService.LanguageChanged -= HandleLanguageChanged;
            _subscribedLanguageService = null;
        }

        private void HandleLanguageChanged(string _)
        {
            RefreshPresentation();
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
            if (Card == null || Card.Data == null || Card.CurrentDurability <= 1)
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

            return GameText.Wait(Card.CurrentWait);
        }

        private string ResolveDurabilityLabel()
        {
            if (Card != null
                && Card.Data != null
                && Card.Data.cardType == CardType.Tenant
                && (CurrentContext == CardViewContext.RoomTenant || CurrentContext == CardViewContext.TooltipPreview))
            {
                return GameText.Lease;
            }

            return GameText.Durability;
        }

        private void UpdateTooltipTrigger()
        {
            bool allowTooltip = Card != null
                && CurrentContext != CardViewContext.TooltipPreview
                && CurrentContext != CardViewContext.RewardPick;
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
            }
            HideBadge(costBadgeRoot, costText);

            if (rentText != null)
            {
                rentText.text = string.Empty;
            }
            HideBadge(rentBadgeRoot, rentText);

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
                artImage.sprite = _initialArtSprite;
                artImage.enabled = _initialArtEnabled;
                artImage.color = _initialArtColor;
                artImage.rectTransform.localScale = _initialArtScale == default ? Vector3.one : _initialArtScale;
            }

            if (background != null)
            {
                background.sprite = _initialBackgroundSprite;
                background.type = _initialBackgroundType;
                background.color = _initialBackgroundColor;
            }

            if (frameImage != null)
            {
                frameImage.sprite = _initialFrameSprite;
                frameImage.type = _initialFrameType;
                frameImage.color = _initialFrameColor;
            }

            if (nameText != null)
            {
                nameText.color = _initialNameColor;
            }

            if (costText != null)
            {
                costText.color = _initialCostColor;
            }

            if (descText != null)
            {
                descText.color = _initialDescColor;
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
            return skinDatabase;
        }

        private static string GetTypeLabel(CardType cardType)
        {
            return GameText.TypeLabel(cardType);
        }

        private void CacheInitialVisualState()
        {
            if (_initialVisualStateCached)
            {
                return;
            }

            if (background != null)
            {
                _initialBackgroundSprite = background.sprite;
                _initialBackgroundType = background.type;
                _initialBackgroundColor = background.color;
            }

            if (frameImage != null)
            {
                _initialFrameSprite = frameImage.sprite;
                _initialFrameType = frameImage.type;
                _initialFrameColor = frameImage.color;
            }

            if (artImage != null)
            {
                _initialArtSprite = artImage.sprite;
                _initialArtColor = artImage.color;
                _initialArtEnabled = artImage.enabled;
                _initialArtScale = artImage.rectTransform.localScale;
            }

            if (nameText != null)
            {
                _initialNameColor = nameText.color;
            }

            if (costText != null)
            {
                _initialCostColor = costText.color;
            }

            if (descText != null)
            {
                _initialDescColor = descText.color;
            }

            _initialVisualStateCached = true;
        }

        private void ApplyInteractionVisualState(bool immediate)
        {
            if (!_initialVisualStateCached)
            {
                CacheInitialVisualState();
            }

            Color targetFaceTint = ResolveFaceTint();
            Color targetFrameTint = ResolveFrameTint();
            Color targetNameTint = ResolveNameTint();
            Vector3 targetArtScale = ResolveArtScale();

            if (immediate)
            {
                if (background != null)
                {
                    background.color = targetFaceTint;
                }

                if (frameImage != null)
                {
                    frameImage.color = targetFrameTint;
                }

                if (artImage != null)
                {
                    artImage.rectTransform.localScale = targetArtScale;
                }

                if (nameText != null)
                {
                    nameText.color = targetNameTint;
                }

                if (costText != null)
                {
                    costText.color = _initialCostColor;
                }

                if (descText != null)
                {
                    descText.color = _initialDescColor;
                }

                return;
            }

            StopInteractionTweens();

            if (background != null)
            {
                _backgroundColorTween = background.DOColor(targetFaceTint, visualTransitionSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            }

            if (frameImage != null)
            {
                _frameColorTween = frameImage.DOColor(targetFrameTint, visualTransitionSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            }

            if (artImage != null)
            {
                _artScaleTween = artImage.rectTransform.DOScale(targetArtScale, visualTransitionSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            }

            if (nameText != null)
            {
                _nameColorTween = nameText.DOColor(targetNameTint, visualTransitionSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            }

            if (costText != null)
            {
                _costColorTween = costText.DOColor(_initialCostColor, visualTransitionSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            }

            if (descText != null)
            {
                _descColorTween = descText.DOColor(_initialDescColor, visualTransitionSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            }
        }

        private Color ResolveFaceTint()
        {
            if (_dragging)
            {
                return Color.Lerp(_initialBackgroundColor, dragFaceTint, 0.85f);
            }

            if (_selected)
            {
                return Color.Lerp(_initialBackgroundColor, hoverFaceTint, 0.72f);
            }

            return _initialBackgroundColor;
        }

        private Color ResolveFrameTint()
        {
            if (_dragging)
            {
                return Color.Lerp(_initialFrameColor, dragFrameTint, 0.96f);
            }

            if (_selected)
            {
                return Color.Lerp(_initialFrameColor, hoverFrameTint, 0.92f);
            }

            return _initialFrameColor;
        }

        private Color ResolveNameTint()
        {
            if (_dragging)
            {
                return Color.Lerp(_initialNameColor, dragNameTint, 0.95f);
            }

            if (_selected)
            {
                return Color.Lerp(_initialNameColor, hoverNameTint, 0.85f);
            }

            return _initialNameColor;
        }

        private Vector3 ResolveArtScale()
        {
            Vector3 baseScale = _initialArtScale == default ? Vector3.one : _initialArtScale;
            if (_dragging)
            {
                return baseScale * dragArtScale;
            }

            if (_selected)
            {
                return baseScale * hoverArtScale;
            }

            return baseScale;
        }

        private void ResetInteractionVisualState()
        {
            _selected = false;
            _dragging = false;

            if (background != null)
            {
                background.color = _initialBackgroundColor;
            }

            if (frameImage != null)
            {
                frameImage.color = _initialFrameColor;
            }

            if (artImage != null)
            {
                artImage.rectTransform.localScale = _initialArtScale == default ? Vector3.one : _initialArtScale;
            }

            if (nameText != null)
            {
                nameText.color = _initialNameColor;
            }

            if (costText != null)
            {
                costText.color = _initialCostColor;
            }

            if (descText != null)
            {
                descText.color = _initialDescColor;
            }
        }

        private void StopInteractionTweens()
        {
            _backgroundColorTween?.Kill(false);
            _frameColorTween?.Kill(false);
            _artScaleTween?.Kill(false);
            _nameColorTween?.Kill(false);
            _costColorTween?.Kill(false);
            _descColorTween?.Kill(false);
            _invalidFramePunchTween?.Kill(false);
            _invalidBackgroundPunchTween?.Kill(false);

            _backgroundColorTween = null;
            _frameColorTween = null;
            _artScaleTween = null;
            _nameColorTween = null;
            _costColorTween = null;
            _descColorTween = null;
            _invalidFramePunchTween = null;
            _invalidBackgroundPunchTween = null;
        }

        private void KillAllVisualTweenTargets()
        {
            (transform as RectTransform)?.DOKill(false);

            if (background != null)
            {
                background.DOKill(false);
                background.rectTransform.DOKill(false);
            }

            if (frameImage != null)
            {
                frameImage.DOKill(false);
                frameImage.rectTransform.DOKill(false);
            }

            if (artImage != null)
            {
                artImage.DOKill(false);
                artImage.rectTransform.DOKill(false);
            }

            nameText?.DOKill(false);
            costText?.DOKill(false);
            descText?.DOKill(false);
        }
    }
}
