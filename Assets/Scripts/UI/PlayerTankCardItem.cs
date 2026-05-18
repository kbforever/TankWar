using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerTankCardItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private Image frameImage;
    private Image accentImage;
    private Image iconImage;
    private Image energyBadgeImage;
    private GameObject lockedOverlay;
    private TMP_Text titleText;
    private TMP_Text costText;
    private TMP_Text descriptionText;
    private GamePanel ownerPanel;
    private Transform originalParent;
    private int originalSiblingIndex;
    private int energyCost;
    private bool isAffordable;

    public PlayerTankType TankType { get; private set; }

    public void Initialize(GamePanel panel, PlayerTankType tankType, Sprite iconSprite, string description)
    {
        ownerPanel = panel;
        TankType = tankType;
        energyCost = PlayerTank.GetEnergyCost(tankType);
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        rootCanvas = parentCanvas != null ? parentCanvas.rootCanvas : null;
        canvasGroup = GetComponent<CanvasGroup>();
        frameImage = GetComponent<Image>();
        accentImage = transform.Find("Accent")?.GetComponent<Image>();
        iconImage = transform.Find("Icon")?.GetComponent<Image>();
        energyBadgeImage = transform.Find("CostBadge")?.GetComponent<Image>();
        lockedOverlay = transform.Find("LockedOverlay")?.gameObject;
        titleText = transform.Find("Title")?.GetComponent<TMP_Text>();
        costText = transform.Find("CostBadge/CostText")?.GetComponent<TMP_Text>();
        descriptionText = transform.Find("Description")?.GetComponent<TMP_Text>();

        if (accentImage != null)
        {
            accentImage.color = PlayerTank.GetTypeColor(tankType);
        }

        if (iconImage != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;
            iconImage.color = iconSprite != null ? Color.white : PlayerTank.GetTypeColor(tankType);
        }

        if (titleText != null)
        {
            titleText.text = PlayerTank.GetShortName(tankType);
        }

        if (costText != null)
        {
            costText.text = energyCost.ToString();
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }

        UpdateAvailability(true);
    }

    public void UpdateAvailability(bool affordable)
    {
        isAffordable = affordable;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = affordable ? 1f : 0.68f;
            canvasGroup.blocksRaycasts = true;
        }

        if (frameImage != null)
        {
            frameImage.color = affordable
                ? new Color(0.16f, 0.18f, 0.24f, 0.98f)
                : new Color(0.1f, 0.11f, 0.14f, 0.98f);
        }

        if (energyBadgeImage != null)
        {
            energyBadgeImage.color = affordable
                ? new Color(0.7f, 0.18f, 0.95f, 1f)
                : new Color(0.35f, 0.2f, 0.42f, 1f);
        }

        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!affordable);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isAffordable || rectTransform == null || rootCanvas == null)
        {
            return;
        }

        originalParent = rectTransform.parent;
        originalSiblingIndex = rectTransform.GetSiblingIndex();
        rectTransform.SetParent(rootCanvas.transform,true);
        rectTransform.SetAsLastSibling();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.86f;
            canvasGroup.blocksRaycasts = false;
        }

        UpdateDraggedCardPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isAffordable || rectTransform == null || rectTransform.parent != rootCanvas.transform)
        {
            return;
        }

        UpdateDraggedCardPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isAffordable)
        {
            return;
        }

        bool spawned = ownerPanel != null && ownerPanel.TryPlaceTankCard(TankType, eventData.position);
        Debug.Log($"Try place {TankType} at {eventData.position}, success: {spawned}");
        RestoreCardToStrip();

        if (!spawned)
        {
            UpdateAvailability(ownerPanel != null && ownerPanel.CanAffordTank(TankType));
        }
    }

    private void UpdateDraggedCardPosition(PointerEventData eventData)
    {
        if (rootCanvas == null || rectTransform == null)
        {
            return;
        }

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvasRect,
            eventData.position,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
            out Vector3 worldPoint))
        {
            rectTransform.position = worldPoint;
        }
    }

    private void RestoreCardToStrip()
    {
        if (rectTransform != null && originalParent != null)
        {
            rectTransform.SetParent(originalParent, false);
            rectTransform.SetSiblingIndex(originalSiblingIndex);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = isAffordable ? 1f : 0.68f;
            canvasGroup.blocksRaycasts = true;
        }
    }
}
