using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GameFramework;

public class GamePanel : UIPanel
{
    private const string PlayerTankCardPrefabPath = "Assets/Prefabs/UI/PlayerTankCard.prefab";
    private const string PlayerTank1PrefabPath = "Assets/Prefabs/Maps/PlayerTank1.prefab";
    private const string PlayerTank2PrefabPath = "Assets/Prefabs/Maps/PlayerTank2.prefab";

    [Header("Game Panel")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Button pauseButton;

    private static readonly FieldInfo PowerLevelSpritesField =
        typeof(PlayerTank).GetField("powerLevelSprites", BindingFlags.Instance | BindingFlags.NonPublic);

    private GameCoreManager gameCoreManager;
    private RectTransform cardPanel;
    private RectTransform cardStrip;
    private TMP_Text energyValueText;
    private readonly List<Image> energyPips = new List<Image>();
    private readonly Dictionary<PlayerTankType, PlayerTankCardItem> cardItems = new Dictionary<PlayerTankType, PlayerTankCardItem>();
    private readonly Dictionary<PlayerTankType, Sprite> tankCardSprites = new Dictionary<PlayerTankType, Sprite>();
    private GameObject playerTankCardPrefab;
    private bool cardContentBuilt;
    private bool loadingCardContent;

    protected GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;

    public override void Initialize()
    {
        base.Initialize();

        scoreText = transform.Find("scoreText")?.GetComponent<Text>();
        pauseButton = transform.Find("pauseButton")?.GetComponent<Button>();

        gameObject.TryGetComponent(out gameCoreManager);
        gameCoreManager = gameCoreManager == null ? gameObject.AddComponent<GameCoreManager>() : gameCoreManager;
        Framework.RegisterFeature(gameCoreManager);

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseGame);
        }

        EnsureEventSystem();
        BuildPlayerTankCardPanel();
        RefreshEnergyUi();
        _ = EnsureCardContentAsync();
    }

    private void Update()
    {
        RefreshEnergyUi();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(OnPauseGame);
        }
    }

    public bool TryPlaceTankCard(PlayerTankType tankType, Vector2 screenPosition)
    {
        bool spawned = gameCoreManager != null && gameCoreManager.TrySpawnPlayerTankFromCard(tankType, screenPosition);
        RefreshEnergyUi();
        return spawned;
    }

    public bool CanAffordTank(PlayerTankType tankType)
    {
        return gameCoreManager != null && gameCoreManager.CanAffordPlayerTank(tankType);
    }

    private void OnPauseGame()
    {
        if (Framework != null)
        {
            Framework.ChangeState(GameState.Paused);
        }
    }

    private void BuildPlayerTankCardPanel()
    {
        Transform existing = transform.Find("PlayerTankCardPanel");
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        cardItems.Clear();
        energyPips.Clear();
        cardContentBuilt = false;
        loadingCardContent = false;

        GameObject panelObject = CreateUiObject("PlayerTankCardPanel", transform);
        cardPanel = panelObject.GetComponent<RectTransform>();
        cardPanel.anchorMin = new Vector2(0.5f, 0f);
        cardPanel.anchorMax = new Vector2(0.5f, 0f);
        cardPanel.pivot = new Vector2(0.5f, 0f);
        cardPanel.anchoredPosition = new Vector2(0f, 18f);
        cardPanel.sizeDelta = new Vector2(860f, 208f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.07f, 0.1f, 0.95f);

        Outline panelOutline = panelObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0f, 0f, 0f, 0.45f);
        panelOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject topBand = CreateUiObject("TopBand", panelObject.transform);
        RectTransform topBandRect = topBand.GetComponent<RectTransform>();
        topBandRect.anchorMin = new Vector2(0f, 1f);
        topBandRect.anchorMax = new Vector2(1f, 1f);
        topBandRect.pivot = new Vector2(0.5f, 1f);
        topBandRect.anchoredPosition = Vector2.zero;
        topBandRect.sizeDelta = new Vector2(0f, 6f);
        Image bandImage = topBand.AddComponent<Image>();
        bandImage.color = new Color(0.74f, 0.28f, 0.98f, 0.95f);

        CreateEnergyBar(panelObject.transform);
        CreateCardStrip(panelObject.transform);
    }

    private async Task EnsureCardContentAsync()
    {
        if (cardContentBuilt || loadingCardContent || cardStrip == null)
        {
            return;
        }

        loadingCardContent = true;

        playerTankCardPrefab ??= await ResourceManager.AsycnLoadAddressable<GameObject>(PlayerTankCardPrefabPath);
        await EnsureTankCardSpritesAsync();

        if (this == null || cardStrip == null || playerTankCardPrefab == null)
        {
            loadingCardContent = false;
            return;
        }

        CreateTankCard(PlayerTankType.Standard);
        CreateTankCard(PlayerTankType.Rapid);
        CreateTankCard(PlayerTankType.Assault);

        cardContentBuilt = true;
        loadingCardContent = false;
        RefreshEnergyUi();
    }

    private async Task EnsureTankCardSpritesAsync()
    {
        if (tankCardSprites.Count >= 3)
        {
            return;
        }

        GameObject playerTank1Prefab = await ResourceManager.AsycnLoadAddressable<GameObject>(PlayerTank1PrefabPath);
        GameObject playerTank2Prefab = await ResourceManager.AsycnLoadAddressable<GameObject>(PlayerTank2PrefabPath);

        Sprite[] player1Sprites = GetPowerLevelSprites(playerTank1Prefab);
        Sprite[] player2Sprites = GetPowerLevelSprites(playerTank2Prefab);

        tankCardSprites[PlayerTankType.Standard] = GetSpriteOrFallback(player1Sprites, 0, playerTank1Prefab);
        tankCardSprites[PlayerTankType.Rapid] = GetSpriteOrFallback(player2Sprites, 0, playerTank2Prefab);
        tankCardSprites[PlayerTankType.Assault] =
            GetSpriteOrFallback(player1Sprites, 2, playerTank1Prefab)
            ?? GetSpriteOrFallback(player2Sprites, 2, playerTank2Prefab)
            ?? GetSpriteOrFallback(player1Sprites, 0, playerTank1Prefab);
    }

    private void CreateEnergyBar(Transform parent)
    {
        GameObject energyRoot = CreateUiObject("EnergyRoot", parent);
        RectTransform energyRect = energyRoot.GetComponent<RectTransform>();
        energyRect.anchorMin = new Vector2(0f, 1f);
        energyRect.anchorMax = new Vector2(1f, 1f);
        energyRect.pivot = new Vector2(0.5f, 1f);
        energyRect.anchoredPosition = new Vector2(0f, -10f);
        energyRect.sizeDelta = new Vector2(-30f, 42f);

        // GameObject labelObject = CreateUiObject("EnergyLabel", energyRoot.transform);
        // RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        // labelRect.anchorMin = new Vector2(0f, 0.5f);
        // labelRect.anchorMax = new Vector2(0f, 0.5f);
        // labelRect.pivot = new Vector2(0f, 0.5f);
        // labelRect.anchoredPosition = new Vector2(0f, 0f);
        // labelRect.sizeDelta = new Vector2(88f, 30f);
        // TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
        // label.text = "\u80fd\u91cf";
        // label.fontSize = 24f;
        // label.fontStyle = FontStyles.Bold;
        // label.color = new Color(0.96f, 0.95f, 1f, 1f);
        // label.alignment = TextAlignmentOptions.Left;

        GameObject valueObject = CreateUiObject("EnergyValue", energyRoot.transform);
        RectTransform valueRect = valueObject.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(1f, 0.5f);
        valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.anchoredPosition = new Vector2(0f, 0f);
        valueRect.sizeDelta = new Vector2(80f, 30f);
        GameObject valueBgObject = CreateUiObject("ValueBackground", valueObject.transform);
        Image valueBg = valueBgObject.AddComponent<Image>();
        valueBg.color = new Color(0.18f, 0.11f, 0.26f, 1f);
        GameObject valueOutlineObject = CreateUiObject("ValueOutline", valueObject.transform);
        Outline valueOutline = valueOutlineObject.AddComponent<Outline>();
        valueOutline.effectColor = new Color(0f, 0f, 0f, 0.4f);
        valueOutline.effectDistance = new Vector2(1f, -1f);
        energyValueText = valueOutlineObject.AddComponent<TextMeshProUGUI>();
        energyValueText.fontSize = 22f;
        energyValueText.fontStyle = FontStyles.Bold;
        energyValueText.color = new Color(0.98f, 0.93f, 1f, 1f);
        energyValueText.alignment = TextAlignmentOptions.Center;

        GameObject trackObject = CreateUiObject("EnergyTrack", energyRoot.transform);
        RectTransform trackRect = trackObject.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0f);
        trackRect.anchorMax = new Vector2(1f, 0f);
        trackRect.pivot = new Vector2(0.5f, 0f);
        trackRect.anchoredPosition = new Vector2(0f, -2f);
        trackRect.sizeDelta = new Vector2(-112f, 12f);
        Image trackImage = trackObject.AddComponent<Image>();
        trackImage.color = new Color(0.14f, 0.15f, 0.2f, 1f);

        HorizontalLayoutGroup layout = trackObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(4, 4, 2, 2);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        for (int i = 0; i < 10; i++)
        {
            GameObject pipObject = CreateUiObject($"EnergyPip_{i}", trackObject.transform);
            Image pipImage = pipObject.AddComponent<Image>();
            pipImage.color = new Color(0.28f, 0.17f, 0.39f, 1f);
            LayoutElement pipLayout = pipObject.AddComponent<LayoutElement>();
            pipLayout.preferredHeight = 8f;
            pipLayout.minHeight = 8f;
            pipLayout.preferredWidth = 1f;
            energyPips.Add(pipImage);
        }
    }

    private void CreateCardStrip(Transform parent)
    {
        GameObject stripObject = CreateUiObject("CardStrip", parent);
        cardStrip = stripObject.GetComponent<RectTransform>();
        cardStrip.anchorMin = new Vector2(0f, 0f);
        cardStrip.anchorMax = new Vector2(1f, 0f);
        cardStrip.pivot = new Vector2(0.5f, 0f);
        cardStrip.anchoredPosition = new Vector2(0f, 12f);
        cardStrip.sizeDelta = new Vector2(-28f, 126f);

        HorizontalLayoutGroup layout = stripObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.padding = new RectOffset(10, 10, 0, 0);
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private void CreateTankCard(PlayerTankType tankType)
    {
        if (playerTankCardPrefab == null || cardStrip == null)
        {
            return;
        }

        GameObject cardObject = Object.Instantiate(playerTankCardPrefab, cardStrip);
        cardObject.name = $"{tankType}Card";

        RectTransform rectTransform = cardObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(196f, 126f);
        }

        LayoutElement layoutElement = cardObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = cardObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = 196f;
        layoutElement.preferredHeight = 126f;

        PlayerTankCardItem cardItem = cardObject.GetComponent<PlayerTankCardItem>();
        if (cardItem == null)
        {
            cardItem = cardObject.AddComponent<PlayerTankCardItem>();
        }

        tankCardSprites.TryGetValue(tankType, out Sprite iconSprite);
        cardItem.Initialize(this, tankType, iconSprite, PlayerTank.GetTypeDescription(tankType));
        cardItems[tankType] = cardItem;
    }

    private void RefreshEnergyUi()
    {
        if (gameCoreManager == null)
        {
            return;
        }

        float currentEnergy = gameCoreManager.PlayerEnergy;
        float maxEnergy = Mathf.Max(1f, gameCoreManager.MaxEnergy);

        if (energyValueText != null)
        {
            energyValueText.text = $"{currentEnergy:0.0}/{maxEnergy:0}";
        }

        for (int i = 0; i < energyPips.Count; i++)
        {
            float filled = Mathf.Clamp01(currentEnergy - i);
            Color onColor = new Color(0.78f, 0.28f, 0.98f, 1f);
            Color offColor = new Color(0.28f, 0.17f, 0.39f, 1f);
            energyPips[i].color = Color.Lerp(offColor, onColor, filled);
        }

        foreach (KeyValuePair<PlayerTankType, PlayerTankCardItem> pair in cardItems)
        {
            pair.Value.UpdateAvailability(gameCoreManager.CanAffordPlayerTank(pair.Key));
        }
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(eventSystemObject);
    }

    private static Sprite[] GetPowerLevelSprites(GameObject tankPrefab)
    {
        if (tankPrefab == null || PowerLevelSpritesField == null)
        {
            return null;
        }

        PlayerTank playerTank = tankPrefab.GetComponent<PlayerTank>();
        if (playerTank == null)
        {
            return null;
        }

        return PowerLevelSpritesField.GetValue(playerTank) as Sprite[];
    }

    private static Sprite GetSpriteOrFallback(Sprite[] sprites, int index, GameObject tankPrefab)
    {
        if (sprites != null && sprites.Length > 0)
        {
            int safeIndex = Mathf.Clamp(index, 0, sprites.Length - 1);
            if (sprites[safeIndex] != null)
            {
                return sprites[safeIndex];
            }
        }

        Image image = tankPrefab != null ? tankPrefab.GetComponent<Image>() : null;
        return image != null ? image.sprite : null;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }
}
