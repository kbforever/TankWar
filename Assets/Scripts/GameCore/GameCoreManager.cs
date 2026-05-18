using System.Collections;
using System.Collections.Generic;
using GameFramework;
using LevelGeneration;
using UnityEngine;
using UnityEngine.UI;

public class GameCoreManager : MonoBehaviour, IGameFeature
{
    private const float FreezeDuration = 5f;
    private const float RandomItemSpawnIntervalMin = 12f;
    private const float RandomItemSpawnIntervalMax = 20f;
    private const int MaxMapItems = 2;
    private const float MaxPlayerEnergy = 10f;
    private const float PlayerEnergyRegenPerSecond = 0.8f;

    private GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;
    private DataManager dataManager;
    private InputManager inputManager;
    private LevelManager levelManager;
    private RectTransform levelContainer;
    private PlayerTank playerTank1;
    private PlayerTank playerTank2;

    private int _CurEnemyCount;
    public int CurEnemyCount
    {
        get => _CurEnemyCount;
        set
        {
            if (_CurEnemyCount != value)
            {
                _CurEnemyCount = value;
                Framework?.PublishEvent(new EnemyCountChangedEvent(_CurEnemyCount, Player1Health, Player2Health));
            }
        }
    }

    private int Player1Health;
    private int Player2Health;
    private int Player1PowerLevel;
    private int Player2PowerLevel;
    private PlayerTankType Player1TankType;
    private PlayerTankType Player2TankType;
    private float playerEnergy;

    private float cellSize;
    private float frozenEnemyTimer;
    private float randomItemSpawnTimer;
    private List<Vector2Int> spawnEnemyPoints;
    private List<Vector2Int> spawnPlayerPoints;

    private GameMode currentGameMode;
    private GameData currentGameData => dataManager.GetGameData();
    private LevelData currentLevelData;
    private readonly List<GameObject> levelCells = new List<GameObject>();
    public List<EnemyTank> enemyTanks = new List<EnemyTank>();
    private readonly List<GameItem> activeItems = new List<GameItem>();
    private readonly List<GameObject> boundaryObjects = new List<GameObject>();
    private int spawnContextVersion;

    private enum TankSpawnKind
    {
        Player,
        Enemy
    }

    private struct TankSpawnRequest
    {
        public TankSpawnKind Kind;
        public GameObject Prefab;
        public LevelData LevelData;
        public Vector2Int SpawnGrid;
        public Vector2 AnchoredPosition;
        public bool UseAnchoredPosition;
        public Color Color;
        public int PlayerIndex;
        public int EnemyId;
        public EnemyTankType EnemyType;
        public int EnemyHealth;
        public bool EnemyHasBonusItem;
        public GameItemType EnemyBonusItemType;
        public int SpawnContextVersion;
    }

    #region Prefabs
    private GameObject PlayerPrefab;
    private readonly Dictionary<EnemyTankType, GameObject> enemyPrefabs = new Dictionary<EnemyTankType, GameObject>();
    private GameObject BulletPrefab;
    private GameObject ItemPrefab;
    private GameObject[] CellPrefabs;
    #endregion

    public bool IsActive { get; private set; }

    public void FeatureFixedUpdate()
    {
    }

    public void FeatureLateUpdate()
    {
    }

    public void FeatureUpdate()
    {
        if (frozenEnemyTimer > 0f)
        {
            frozenEnemyTimer -= Time.deltaTime;
        }

        if (Framework.CurrentState == GameState.Playing)
        {
            playerEnergy = Mathf.Min(MaxPlayerEnergy, playerEnergy + PlayerEnergyRegenPerSecond * Time.deltaTime);
        }

        if (Framework.CurrentState != GameState.Playing || currentLevelData == null)
        {
            return;
        }

        if (activeItems.Count >= MaxMapItems)
        {
            return;
        }

        // randomItemSpawnTimer -= Time.deltaTime;
        // if (randomItemSpawnTimer <= 0f)
        // {
        //     TrySpawnRandomMapItem();
        //     ResetRandomItemSpawnTimer();
        // }
    }

    public void Initialize()
    {
        IsActive = true;
        levelContainer = transform.Find("levelContainer") as RectTransform;
        dataManager = Framework.GetFeature<DataManager>();
        levelManager = Framework.GetFeature<LevelManager>();
        inputManager = Framework.GetFeature<InputManager>();
        currentLevelData = null;

        if (levelManager != null)
        {
            Framework?.SubscribeEvent<LevelManager.LevelLoadedEvent>(OnLevelLoaded);
        }

        Framework.SubscribeEvent<GameStateChangedEvent>(OnFrameworkGameStateChanged);
        Framework.SubscribeEvent<BulletEvent>(CreateBullet);
        Framework.SubscribeEvent<PlayerDieEvent>(PlayerDie);
        Framework.SubscribeEvent<EnemyDieEvent>(EnemyDie);
    }

    private bool isInit = false;
    private async void LoadPrefabsAndLevels()
    {
        Framework.ChangeState(GameState.Loading);
        if (!isInit)
        {
            string rootPrefabsPath = "Assets/Prefabs/Maps/";
            PlayerPrefab = await ResourceManager.AsycnLoadAddressable<GameObject>(rootPrefabsPath + "PlayerTank1.prefab");

            enemyPrefabs.Clear();
            enemyPrefabs[EnemyTankType.Basic] = await ResourceManager.AsycnLoadAddressable<GameObject>(rootPrefabsPath + "EnemyTankBasic.prefab");
            enemyPrefabs[EnemyTankType.Fast] = await ResourceManager.AsycnLoadAddressable<GameObject>(rootPrefabsPath + "EnemyTankFast.prefab");
            enemyPrefabs[EnemyTankType.Strong] = await ResourceManager.AsycnLoadAddressable<GameObject>(rootPrefabsPath + "EnemyTankStrong.prefab");
            enemyPrefabs[EnemyTankType.Heavy] = await ResourceManager.AsycnLoadAddressable<GameObject>(rootPrefabsPath + "EnemyTankHeavy.prefab");

            BulletPrefab = await ResourceManager.AsycnLoadAddressable<GameObject>(rootPrefabsPath + "Bullet.prefab");
            ItemPrefab = await ResourceManager.AsycnLoadAddressable<GameObject>(rootPrefabsPath + "Item.prefab");

            CellPrefabs = new GameObject[(int)LevelTileType.Base];
            for (int i = 0; i < (int)LevelTileType.Base; i++)
            {
                LevelTileType tile = (LevelTileType)i;
                if (tile == LevelTileType.EnemySpawn || tile == LevelTileType.PlayerSpawn)
                {
                    continue;
                }

                CellPrefabs[i] = await ResourceManager.AsycnLoadAddressable<GameObject>(rootPrefabsPath + tile + ".prefab");
            }
        }

        isInit = true;
        levelManager.LoadLevel(currentGameData.LevelIndex);
    }

    private void EnemyDie(EnemyDieEvent enemyDieEvent)
    {
        EnemyTank enemyTank = enemyDieEvent.enemyTank;
        enemyTanks.Remove(enemyTank);

        Destroy(enemyTank.gameObject);

        if (CurEnemyCount > 0)
        {
            SpawnEnemyTanks(currentLevelData, cellSize, true);
        }
        else if (enemyTanks.Count <= 0)
        {
            dataManager.SetGameData(new GameData { gameMode = currentGameMode });
            Framework.ChangeState(GameState.Loading);
            levelManager.LoadNextLevel();
        }
    }

    private void PlayerDie(PlayerDieEvent playerDieEvent)
    {
        PlayerTank playerTank = playerDieEvent.playerTank;
        if (playerTank == playerTank1)
        {
            Player1PowerLevel = playerTank.PowerLevel;
            Player1TankType = playerTank.TankType;
            Player1Health--;
            if (Player1Health > 0)
            {
                CreatePlayerTank(currentLevelData, cellSize, spawnPlayerPoints[0], Color.white, 1, Player1TankType, tank =>
                {
                    playerTank1 = tank;
                    playerTank1.RestoreState(Mathf.Max(1, Player1Health), Player1PowerLevel);
                });
            }
        }

        if (playerTank == playerTank2)
        {
            Player2PowerLevel = playerTank.PowerLevel;
            Player2TankType = playerTank.TankType;
            Player2Health--;
            if (Player2Health > 0)
            {
                CreatePlayerTank(currentLevelData, cellSize, spawnPlayerPoints[1], Color.blue, 2, Player2TankType, tank =>
                {
                    playerTank2 = tank;
                    playerTank2.RestoreState(Mathf.Max(1, Player2Health), Player2PowerLevel);
                });
            }
        }

        Destroy(playerTank.gameObject);
        if (Player1Health <= 0 && Player2Health <= 0)
        {
            Framework.ChangeState(GameState.GameOver);
        }

        Framework?.PublishEvent(new EnemyCountChangedEvent(CurEnemyCount, Player1Health, Player2Health));
    }

    private void CreateBullet(BulletEvent bulletEvent)
    {
        CreateBullet(bulletEvent.gameObject);
    }

    private void OnFrameworkGameStateChanged(GameStateChangedEvent changeevent)
    {
        OnGameStateChanged(changeevent.PreviousState, changeevent.NextState);
    }

    private void OnLevelLoaded(LevelManager.LevelLoadedEvent levelEvent)
    {
        RenderLevel(levelEvent.LevelData);
    }

    public void OnGameStateChanged(GameState previousState, GameState nextState)
    {
        if (nextState == GameState.Playing)
        {
            currentGameMode = currentGameData.gameMode;
            if (previousState == GameState.Paused)
            {
                inputManager.EnableMaps();
                Time.timeScale = 1f;
                return;
            }

            if (previousState == GameState.MainMenu && levelManager.CurrentLevelIndex != currentGameData.LevelIndex)
            {
                LoadPrefabsAndLevels();
                return;
            }

            if (levelManager.CurrentLevelData == null)
            {
                return;
            }

            currentLevelData = levelManager.CurrentLevelData;
            if (currentGameData.enmeyPositions != null && currentGameData.player1Health > 0)
            {
                RenderLevel(currentLevelData, false);

                Player1Health = currentGameData.player1Health;
                Player2Health = currentGameData.gameMode == GameMode.SinglePlayer ? 0 : currentGameData.player2Health;
                Player1PowerLevel = currentGameData.player1PowerLevel;
                Player2PowerLevel = currentGameData.gameMode == GameMode.SinglePlayer ? 0 : currentGameData.player2PowerLevel;
                Player1TankType = GetSavedPlayerTankType(currentGameData.player1TankType);
                Player2TankType = currentGameData.gameMode == GameMode.SinglePlayer ? PlayerTankType.Standard : GetSavedPlayerTankType(currentGameData.player2TankType);
                playerEnergy = Mathf.Clamp(currentGameData.playerEnergy, 0f, MaxPlayerEnergy);
                CurEnemyCount = currentGameData.maxEnemyCount + currentGameData.enmeyPositions.Length;

                RestorePlayersFromSave();
                RestoreEnemiesFromSave();
                RestoreItemsFromSave();
            }
            else
            {
                RenderLevel(currentLevelData);
                UpdateGameData();
            }

            Time.timeScale = 1f;
            inputManager.EnableMaps();
        }
        else if (nextState == GameState.Paused)
        {
            Time.timeScale = 0f;
            inputManager.DisableMaps();
            UpdateGameData();
        }
        else if (nextState == GameState.GameOver)
        {
            Time.timeScale = 0f;
            inputManager.DisableMaps();
            ClearPlayerTank();
            ClearEnemyTanks();
            ClearItems();
            UpdateGameData();
        }
    }

    private void OnApplicationQuit()
    {
        if (Framework != null)
        {
            Framework.UnsubscribeEvent<LevelManager.LevelLoadedEvent>(OnLevelLoaded);
            Framework.UnsubscribeEvent<GameStateChangedEvent>(OnFrameworkGameStateChanged);
            Framework.UnsubscribeEvent<BulletEvent>(CreateBullet);
        }

        if (dataManager != null)
        {
            UpdateGameData();
            dataManager.SaveGameData();
        }

        ClearLevelCells();
        ClearPlayerTank();
        ClearEnemyTanks();
        ClearItems();
        ClearBoundaryObjects();
    }

    public void Shutdown()
    {
    }

    private void RenderLevel(LevelData levelData, bool genEnemy = true)
    {
        if (levelContainer == null || levelData == null)
        {
            return;
        }

        spawnContextVersion++;
        ClearLevelCells();
        ClearPlayerTank();
        ClearItems();
        ClearBoundaryObjects();
        ClearEnemyTanks();

        Player1Health = levelData.player1Health;
        Player2Health = levelData.player2Health;
        Player1PowerLevel = 0;
        Player2PowerLevel = 0;
        Player1TankType = PlayerTankType.Standard;
        Player2TankType = PlayerTankType.Standard;
        playerEnergy = MaxPlayerEnergy;
        CurEnemyCount = levelData.maxEnemyCount;
        frozenEnemyTimer = 0f;
        ResetRandomItemSpawnTimer();

        float cellSpaceSize = 5f;
        Vector2 containerSize = levelContainer.rect.size;
        if (containerSize == Vector2.zero && levelContainer.parent is RectTransform parentRect)
        {
            containerSize = parentRect.rect.size;
        }

        if (containerSize == Vector2.zero)
        {
            containerSize = new Vector2(levelData.width * 24f, levelData.height * 24f);
        }

        float cellWidth = containerSize.x / levelData.width;
        float cellHeight = containerSize.y / levelData.height;
        cellSize = Mathf.Min(cellWidth, cellHeight);

        spawnEnemyPoints = new List<Vector2Int>();
        spawnPlayerPoints = new List<Vector2Int>();

        for (int y = 0; y < levelData.height; y++)
        {
            for (int x = 0; x < levelData.width; x++)
            {
                if (levelData.GetTile(x, y) == LevelTileType.EnemySpawn)
                {
                    spawnEnemyPoints.Add(new Vector2Int(x, y));
                }

                if (levelData.GetTile(x, y) == LevelTileType.PlayerSpawn)
                {
                    spawnPlayerPoints.Add(new Vector2Int(x, y));
                }

                LevelTileType tileType = levelData.GetTile(x, y);
                GameObject cell = CreateCell(tileType);
                cell.name = $"Tile_{x}_{y}";
                cell.transform.SetParent(levelContainer, false);

                RectTransform rectTransform = cell.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.zero;
                rectTransform.pivot = Vector2.zero;
                rectTransform.anchoredPosition = new Vector2(x * cellSize, y * cellSize);
                rectTransform.sizeDelta = new Vector2(cellSize, cellSize);

                BoxCollider2D obstacleCollider = cell.GetComponent<BoxCollider2D>();
                if (obstacleCollider != null)
                {
                    obstacleCollider.size = new Vector2(cellSize - cellSpaceSize, cellSize - cellSpaceSize);
                    obstacleCollider.offset = new Vector2(cellSize / 2f, cellSize / 2f);
                }

                levelCells.Add(cell);
            }
        }

        CreateBoundary(levelData, cellSize);
        // SpawnPlayerTank(levelData, cellSize);
        if (genEnemy)
        {
            SpawnEnemyTanks(levelData, cellSize);
        }
    }

    private void SpawnPlayerTank(LevelData levelData, float currentCellSize)
    {
        if (levelContainer == null || levelData == null || dataManager == null)
        {
            return;
        }

        if (spawnPlayerPoints.Count == 0)
        {
            spawnPlayerPoints.Add(new Vector2Int(levelData.width / 2, levelData.height / 2));
        }

        if (currentGameMode == GameMode.SinglePlayer && spawnPlayerPoints.Count >= 1)
        {
            Player2Health = 0;
            CreatePlayerTank(levelData, currentCellSize, spawnPlayerPoints[0], Color.white, 1, Player1TankType, tank => playerTank1 = tank);
        }
        else if (currentGameMode == GameMode.TwoPlayer)
        {
            CreatePlayerTank(levelData, currentCellSize, spawnPlayerPoints[0], Color.white, 1, Player1TankType, tank => playerTank1 = tank);
            if (spawnPlayerPoints.Count > 1)
            {
                CreatePlayerTank(levelData, currentCellSize, spawnPlayerPoints[1], Color.blue, 2, Player2TankType, tank => playerTank2 = tank);
            }
            else
            {
                Vector2Int pos2 = spawnPlayerPoints[0] + Vector2Int.right;
                if (levelData.IsPositionValid(pos2.x, pos2.y))
                {
                    CreatePlayerTank(levelData, currentCellSize, pos2, Color.blue, 2, Player2TankType, tank => playerTank2 = tank);
                }
            }
        }
    }

    private void CreatePlayerTank(LevelData levelData, float currentCellSize, Vector2Int spawnGrid, Color color, int playerIndex, PlayerTankType tankType, System.Action<PlayerTank> onSpawned = null)
    {
        StartCoroutine(SpawnTankRoutine<PlayerTank>(new TankSpawnRequest
        {
            Kind = TankSpawnKind.Player,
            Prefab = PlayerPrefab,
            LevelData = levelData,
            SpawnGrid = spawnGrid,
            Color = color,
            PlayerIndex = playerIndex,
            SpawnContextVersion = spawnContextVersion
        }, tank =>
        {
            if (tank != null)
            {
                tank.ConfigureType(tankType);
            }

            onSpawned?.Invoke(tank);
        }));
    }

    private void SpawnEnemyTanks(LevelData levelData, float currentCellSize, bool genOneEnemy = false)
    {
        if (levelContainer == null || levelData == null || spawnEnemyPoints == null || spawnEnemyPoints.Count == 0)
        {
            return;
        }

        if (genOneEnemy)
        {
            if (TryGetAvailableEnemySpawnPoint(out Vector2Int spawnPoint))
            {
                EnemyTankType enemyType = GetRandomEnemyTankType();
                CreateEnemyTank(currentCellSize, spawnPoint, Color.red, enemyType, tank => enemyTanks.Add(tank));
            }
        }
        else
        {
            foreach (Vector2Int spawnPoint in GetAvailableEnemySpawnPoints())
            {
                EnemyTankType enemyType = GetRandomEnemyTankType();
                CreateEnemyTank(currentCellSize, spawnPoint, Color.red, enemyType, tank => enemyTanks.Add(tank));
            }
        }
    }

    private void CreateEnemyTank(float currentCellSize, Vector2Int spawnGrid, Color color, EnemyTankType enemyType, System.Action<EnemyTank> onSpawned = null)
    {
        if (!enemyPrefabs.TryGetValue(enemyType, out GameObject enemyPrefab))
        {
            return;
        }

        StartCoroutine(SpawnTankRoutine(new TankSpawnRequest
        {
            Kind = TankSpawnKind.Enemy,
            Prefab = enemyPrefab,
            SpawnGrid = spawnGrid,
            Color = color,
            EnemyId = CurEnemyCount--,
            EnemyType = enemyType,
            EnemyHealth = -1,
            EnemyHasBonusItem = Random.value < 0.35f,
            EnemyBonusItemType = GetRandomItemType(),
            SpawnContextVersion = spawnContextVersion
        }, onSpawned));
    }

    private void CreateEnemyTank(float currentCellSize, Vector2 position, Color color, EnemyTankType enemyType, bool isRestore = false, int restoredHealth = -1, bool restoredHasItem = false, GameItemType restoredItemType = GameItemType.FreezeEnemies, System.Action<EnemyTank> onSpawned = null)
    {
        if (!enemyPrefabs.TryGetValue(enemyType, out GameObject enemyPrefab))
        {
            return;
        }

        StartCoroutine(SpawnTankRoutine(new TankSpawnRequest
        {
            Kind = TankSpawnKind.Enemy,
            Prefab = enemyPrefab,
            AnchoredPosition = position,
            UseAnchoredPosition = true,
            Color = color,
            EnemyId = CurEnemyCount--,
            EnemyType = enemyType,
            EnemyHealth = restoredHealth,
            EnemyHasBonusItem = restoredHasItem,
            EnemyBonusItemType = restoredItemType,
            SpawnContextVersion = spawnContextVersion
        }, onSpawned));
    }

    private IEnumerator SpawnTankRoutine<T>(TankSpawnRequest request, System.Action<T> onSpawned) where T : Component
    {
        if (request.Prefab == null || levelContainer == null || !IsSpawnRequestValid(request))
        {
            yield break;
        }

        T tank = SpawnTank<T>(request);
        if (tank == null)
        {
            yield break;
        }

        onSpawned?.Invoke(tank);
    }

    private T SpawnTank<T>(TankSpawnRequest request) where T : Component
    {
        if (request.Prefab == null || levelContainer == null)
        {
            return null;
        }

        GameObject tankObject = Instantiate(request.Prefab);
        PrepareSpawnedTankObject(tankObject);

        T tank = tankObject.GetComponent<T>();
        if (tank == null)
        {
            tank = tankObject.AddComponent<T>();
        }

        InitializeSpawnedTank(tank, request);
        return tank;
    }

    private void PrepareSpawnedTankObject(GameObject tankObject)
    {
        tankObject.transform.SetParent(levelContainer, false);
        tankObject.transform.SetAsFirstSibling();
    }

    private bool IsSpawnRequestValid(TankSpawnRequest request)
    {
        return levelContainer != null && request.SpawnContextVersion == spawnContextVersion;
    }

    private bool TryGetAvailableEnemySpawnPoint(out Vector2Int spawnPoint)
    {
        List<Vector2Int> availablePoints = GetAvailableEnemySpawnPoints();
        if (availablePoints.Count > 0)
        {
            spawnPoint = availablePoints[0];
            return true;
        }

        spawnPoint = default;
        return false;
    }

    private List<Vector2Int> GetAvailableEnemySpawnPoints()
    {
        List<Vector2Int> candidates = new List<Vector2Int>(spawnEnemyPoints);
        for (int i = 0; i < candidates.Count; i++)
        {
            int randomIndex = Random.Range(i, candidates.Count);
            Vector2Int temp = candidates[i];
            candidates[i] = candidates[randomIndex];
            candidates[randomIndex] = temp;
        }

        List<Vector2Int> availablePoints = new List<Vector2Int>();
        foreach (Vector2Int candidate in candidates)
        {
            if (!IsTankBlockingEnemySpawn(candidate))
            {
                availablePoints.Add(candidate);
            }
        }

        return availablePoints;
    }

    private bool IsTankBlockingEnemySpawn(Vector2Int spawnGrid)
    {
        Vector2 spawnPosition = new Vector2((spawnGrid.x + 0.5f) * cellSize, (spawnGrid.y + 0.5f) * cellSize);
        return HasNearbyTank(spawnPosition);
    }

    private void InitializeSpawnedTank<T>(T tank, TankSpawnRequest request) where T : Component
    {
        if (tank is PlayerTank playerTank && request.LevelData != null)
        {
            playerTank.Initialize(cellSize, request.SpawnGrid, new Vector2Int(request.LevelData.width, request.LevelData.height), request.Color, request.PlayerIndex, request.LevelData);
            return;
        }

        if (tank is EnemyTank enemyTank)
        {
            if (request.UseAnchoredPosition)
            {
                enemyTank.Initialize(cellSize, request.AnchoredPosition, request.Color, request.EnemyId, request.EnemyHealth, request.EnemyHasBonusItem, request.EnemyBonusItemType);
            }
            else
            {
                enemyTank.Initialize(cellSize, request.SpawnGrid, request.Color, request.EnemyId, request.EnemyHealth, request.EnemyHasBonusItem, request.EnemyBonusItemType);
            }
        }
    }

    private EnemyTankType GetRandomEnemyTankType()
    {
        EnemyTankType[] types =
        {
            EnemyTankType.Basic,
            EnemyTankType.Fast,
            EnemyTankType.Heavy
        };
        return types[Random.Range(0, types.Length)];
    }

    private GameItemType GetRandomItemType()
    {
        GameItemType[] types =
        {
            GameItemType.FreezeEnemies,
            GameItemType.PlayerShield,
            GameItemType.PlayerLife,
            GameItemType.PlayerPower,
            GameItemType.DestroyAllEnemies,
            GameItemType.BaseInvincible
        };
        return types[Random.Range(0, types.Length)];
    }

    private EnemyTankType GetSavedEnemyType(int index)
    {
        if (currentGameData.enemyTypes != null && index >= 0 && index < currentGameData.enemyTypes.Length)
        {
            int rawType = currentGameData.enemyTypes[index];
            if (System.Enum.IsDefined(typeof(EnemyTankType), rawType))
            {
                return (EnemyTankType)rawType;
            }
        }

        return EnemyTankType.Basic;
    }

    private int GetSavedEnemyHealth(int index)
    {
        if (currentGameData.enemyHealths != null && index >= 0 && index < currentGameData.enemyHealths.Length)
        {
            return currentGameData.enemyHealths[index];
        }

        return -1;
    }

    private bool GetSavedEnemyHasItem(int index)
    {
        if (currentGameData.enemyHasItems != null && index >= 0 && index < currentGameData.enemyHasItems.Length)
        {
            return currentGameData.enemyHasItems[index];
        }

        return false;
    }

    private GameItemType GetSavedEnemyItemType(int index)
    {
        if (currentGameData.enemyItemTypes != null && index >= 0 && index < currentGameData.enemyItemTypes.Length)
        {
            int rawType = currentGameData.enemyItemTypes[index];
            if (System.Enum.IsDefined(typeof(GameItemType), rawType))
            {
                return (GameItemType)rawType;
            }
        }

        return GameItemType.FreezeEnemies;
    }

    private void RestorePlayersFromSave()
    {
        if (playerTank1 != null)
        {
            playerTank1.GetComponent<RectTransform>().anchoredPosition = currentGameData.player1Position;
            playerTank1.ConfigureType(Player1TankType);
            playerTank1.RestoreState(Mathf.Max(1, Player1Health), Player1PowerLevel);
            if (Player1Health <= 0)
            {
                Destroy(playerTank1.gameObject);
                playerTank1 = null;
            }
        }

        if (playerTank2 != null)
        {
            playerTank2.GetComponent<RectTransform>().anchoredPosition = currentGameData.player2Position;
            playerTank2.ConfigureType(Player2TankType);
            playerTank2.RestoreState(Mathf.Max(1, Player2Health), Player2PowerLevel);
            if (Player2Health <= 0)
            {
                Destroy(playerTank2.gameObject);
                playerTank2 = null;
            }
        }
    }

    private void RestoreEnemiesFromSave()
    {
        enemyTanks.Clear();
        for (int i = 0; i < currentGameData.enmeyPositions.Length; i++)
        {
            EnemyTankType enemyType = GetSavedEnemyType(i);
            int enemyHealth = GetSavedEnemyHealth(i);
            bool hasBonusItem = GetSavedEnemyHasItem(i);
            GameItemType itemType = GetSavedEnemyItemType(i);

            CreateEnemyTank(cellSize, currentGameData.enmeyPositions[i], Color.white, enemyType, true, enemyHealth, hasBonusItem, itemType, tank => enemyTanks.Add(tank));
        }
    }

    private void RestoreItemsFromSave()
    {
        if (currentGameData.itemPositions == null || currentGameData.itemTypes == null)
        {
            return;
        }

        for (int i = 0; i < Mathf.Min(currentGameData.itemPositions.Length, currentGameData.itemTypes.Length); i++)
        {
            int rawType = currentGameData.itemTypes[i];
            if (!System.Enum.IsDefined(typeof(GameItemType), rawType))
            {
                continue;
            }

            SpawnItem(currentGameData.itemPositions[i], (GameItemType)rawType);
        }
    }

    private void ClearPlayerTank()
    {
        if (playerTank1 != null)
        {
            Destroy(playerTank1.gameObject);
            playerTank1 = null;
        }

        if (playerTank2 != null)
        {
            Destroy(playerTank2.gameObject);
            playerTank2 = null;
        }
    }

    private void ClearEnemyTanks()
    {
        foreach (EnemyTank enemyTank in enemyTanks)
        {
            if (enemyTank != null && enemyTank.gameObject != null)
            {
                Destroy(enemyTank.gameObject);
            }
        }

        enemyTanks.Clear();
    }

    private void ClearItems()
    {
        foreach (GameItem item in activeItems)
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }

        activeItems.Clear();
    }

    private GameObject CreateCell(LevelTileType tileType)
    {
        if (tileType == LevelTileType.PlayerSpawn || tileType == LevelTileType.EnemySpawn)
        {
            return CreateCell(LevelTileType.Empty);
        }

        GameObject cell = Instantiate(CellPrefabs[(int)tileType]);
        RuntimeLevelTile runtimeTile = cell.GetComponent<RuntimeLevelTile>();
        if (runtimeTile == null)
        {
            runtimeTile = cell.AddComponent<RuntimeLevelTile>();
        }

        runtimeTile.Initialize(tileType);

        Image image = cell.GetComponent<Image>();
        image.color = GetColorForTile(tileType);

        if (IsBlockingTile(tileType))
        {
            BoxCollider2D collider = cell.AddComponent<BoxCollider2D>();
            collider.offset = Vector2.zero;
            collider.size = Vector2.one;
            Rigidbody2D rigidbody = cell.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Static;
            rigidbody.simulated = true;
        }

        return cell;
    }

    private void UpdateGameData()
    {
        GameData gameData = new GameData
        {
            player1Position = playerTank1 != null ? playerTank1.GetComponent<RectTransform>().anchoredPosition : Vector2.zero,
            player2Position = playerTank2 != null ? playerTank2.GetComponent<RectTransform>().anchoredPosition : Vector2.zero,
            player1Health = Player1Health,
            player2Health = Player2Health,
            player1PowerLevel = playerTank1 != null ? playerTank1.PowerLevel : Player1PowerLevel,
            player2PowerLevel = playerTank2 != null ? playerTank2.PowerLevel : Player2PowerLevel,
            player1TankType = (int)(playerTank1 != null ? playerTank1.TankType : Player1TankType),
            player2TankType = (int)(playerTank2 != null ? playerTank2.TankType : Player2TankType),
            playerEnergy = playerEnergy,
            gameMode = dataManager.GetGameData().gameMode,
            LevelIndex = levelManager.CurrentLevelIndex,
            maxEnemyCount = CurEnemyCount
        };

        gameData.enmeyPositions = enemyTanks != null ? new Vector2[enemyTanks.Count] : null;
        gameData.enemyTypes = enemyTanks != null ? new int[enemyTanks.Count] : null;
        gameData.enemyHealths = enemyTanks != null ? new int[enemyTanks.Count] : null;
        gameData.enemyHasItems = enemyTanks != null ? new bool[enemyTanks.Count] : null;
        gameData.enemyItemTypes = enemyTanks != null ? new int[enemyTanks.Count] : null;

        for (int i = 0; i < enemyTanks.Count; i++)
        {
            EnemyTank enemyTank = enemyTanks[i];
            if (enemyTank == null)
            {
                continue;
            }

            gameData.enmeyPositions[i] = enemyTank.GetComponent<RectTransform>().anchoredPosition;
            gameData.enemyTypes[i] = (int)enemyTank.TankType;
            gameData.enemyHealths[i] = enemyTank.CurrentHealth;
            gameData.enemyHasItems[i] = enemyTank.HasBonusItem;
            gameData.enemyItemTypes[i] = (int)enemyTank.BonusItemType;
        }

        gameData.itemPositions = activeItems != null ? new Vector2[activeItems.Count] : null;
        gameData.itemTypes = activeItems != null ? new int[activeItems.Count] : null;
        for (int i = 0; i < activeItems.Count; i++)
        {
            GameItem item = activeItems[i];
            if (item == null)
            {
                continue;
            }

            gameData.itemPositions[i] = item.GetComponent<RectTransform>().anchoredPosition;
            gameData.itemTypes[i] = (int)item.ItemType;
        }

        dataManager.SetGameData(gameData);
    }

    public sealed class BulletEvent : GameEvent
    {
        public GameObject gameObject;

        public BulletEvent(GameObject gameObject)
        {
            this.gameObject = gameObject;
        }
    }

    private void CreateBullet(GameObject parent)
    {
        GameObject bullet = Instantiate(BulletPrefab, parent.transform.position, Quaternion.identity);
        bullet.transform.SetParent(levelContainer);
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent == null)
        {
            bulletComponent = bullet.AddComponent<Bullet>();
        }

        bulletComponent.movedir = parent.transform.up;
        bulletComponent.selfTag = parent.tag;
        bulletComponent.canBreakSteel = false;

        if (parent.CompareTag("Player"))
        {
            PlayerTank owner = parent.GetComponentInParent<PlayerTank>();
            bulletComponent.canBreakSteel = owner != null && owner.CanBreakSteel;
        }
    }

    private void SpawnItem(Vector2 anchoredPosition, GameItemType itemType)
    {
        if (ItemPrefab == null || levelContainer == null)
        {
            return;
        }

        GameObject itemObject = Instantiate(ItemPrefab);
        itemObject.transform.SetParent(levelContainer, false);
        itemObject.transform.SetAsLastSibling();

        GameItem item = itemObject.GetComponent<GameItem>();
        if (item == null)
        {
            item = itemObject.AddComponent<GameItem>();
        }

        item.Initialize(itemType, cellSize, anchoredPosition);
        activeItems.Add(item);
    }

    public void SpawnDroppedItem(Vector2 anchoredPosition, GameItemType itemType)
    {
        // SpawnItem(anchoredPosition, itemType);
        TrySpawnRandomMapItem();
    }

    public void ApplyItem(PlayerTank playerTank, GameItemType itemType)
    {
        if (playerTank == null)
        {
            return;
        }

        switch (itemType)
        {
            case GameItemType.FreezeEnemies:
                frozenEnemyTimer = FreezeDuration;
                break;
            case GameItemType.PlayerShield:
                playerTank.GrantShield();
                break;
            case GameItemType.PlayerLife:
                playerTank.GrantLife();
                if (playerTank == playerTank1)
                {
                    Player1Health++;
                }
                else if (playerTank == playerTank2)
                {
                    Player2Health++;
                }
                break;
            case GameItemType.PlayerPower:
                playerTank.UpgradePower();
                if (playerTank == playerTank1)
                {
                    Player1PowerLevel = playerTank.PowerLevel;
                }
                else if (playerTank == playerTank2)
                {
                    Player2PowerLevel = playerTank.PowerLevel;
                }
                break;
            case GameItemType.DestroyAllEnemies:
                DestroyAllEnemiesImmediately();
                break;
            case GameItemType.BaseInvincible:
                break;
        }

        Framework?.PublishEvent(new EnemyCountChangedEvent(CurEnemyCount, Player1Health, Player2Health));
    }

    public void OnItemPicked(GameItem item)
    {
        if (item == null)
        {
            return;
        }

        activeItems.Remove(item);
        Destroy(item.gameObject);
    }

    public bool AreEnemiesFrozen()
    {
        return frozenEnemyTimer > 0f;
    }

    private void ResetRandomItemSpawnTimer()
    {
        randomItemSpawnTimer = Random.Range(RandomItemSpawnIntervalMin, RandomItemSpawnIntervalMax);
    }

    private void TrySpawnRandomMapItem()
    {
        if (currentLevelData == null || levelContainer == null)
        {
            return;
        }

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int y = 0; y < currentLevelData.height; y++)
        {
            for (int x = 0; x < currentLevelData.width; x++)
            {
                LevelTileType tileType = currentLevelData.GetTile(x, y);
                if (tileType != LevelTileType.Empty)
                {
                    continue;
                }

                Vector2 anchoredPosition = new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);
                if (HasNearbyDynamicObject(anchoredPosition))
                {
                    continue;
                }

                candidates.Add(new Vector2Int(x, y));
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        Vector2Int selected = candidates[Random.Range(0, candidates.Count)];
        Vector2 spawnPosition = new Vector2((selected.x + 0.5f) * cellSize, (selected.y + 0.5f) * cellSize);
        SpawnItem(spawnPosition, GetRandomItemType());
    }

    private bool HasNearbyDynamicObject(Vector2 anchoredPosition)
    {
        float sqrDistance = cellSize * cellSize * 0.8f;

        if (HasNearbyTank(anchoredPosition))
        {
            return true;
        }

        foreach (GameItem item in activeItems)
        {
            if (item == null)
            {
                continue;
            }

            if ((item.GetComponent<RectTransform>().anchoredPosition - anchoredPosition).sqrMagnitude < sqrDistance)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasNearbyTank(Vector2 anchoredPosition)
    {
        const float minDistanceFactor = 0.9f;
        float sqrDistance = cellSize * cellSize * minDistanceFactor;

        if (playerTank1 != null && (playerTank1.GetComponent<RectTransform>().anchoredPosition - anchoredPosition).sqrMagnitude < sqrDistance)
        {
            return true;
        }

        if (playerTank2 != null && (playerTank2.GetComponent<RectTransform>().anchoredPosition - anchoredPosition).sqrMagnitude < sqrDistance)
        {
            return true;
        }

        foreach (EnemyTank enemyTank in enemyTanks)
        {
            if (enemyTank == null)
            {
                continue;
            }

            if ((enemyTank.GetComponent<RectTransform>().anchoredPosition - anchoredPosition).sqrMagnitude < sqrDistance)
            {
                return true;
            }
        }

        return false;
    }

    private void DestroyAllEnemiesImmediately()
    {
        if (enemyTanks.Count == 0)
        {
            return;
        }

        List<EnemyTank> enemiesToDestroy = new List<EnemyTank>(enemyTanks);
        foreach (EnemyTank enemyTank in enemiesToDestroy)
        {
            if (enemyTank == null)
            {
                continue;
            }

            enemyTank.ConfigureBonusItem(false, enemyTank.BonusItemType);
            enemyTank.TakeDamage(enemyTank.CurrentHealth);
        }
    }

    private PlayerTankType GetSavedPlayerTankType(int rawType)
    {
        if (System.Enum.IsDefined(typeof(PlayerTankType), rawType))
        {
            return (PlayerTankType)rawType;
        }

        return PlayerTankType.Standard;
    }

    public bool TrySpawnPlayerTankFromCard(PlayerTankType tankType, Vector2 screenPosition)
    {
        if (levelContainer == null || currentLevelData == null)
        {
            return false;
        }

        int energyCost = PlayerTank.GetEnergyCost(tankType);
        if (playerEnergy < energyCost)
        {
            return false;
        }

        Canvas canvas = levelContainer.GetComponentInParent<Canvas>();
        Camera eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = canvas.worldCamera;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(levelContainer, screenPosition, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        Vector2 containerSize = levelContainer.rect.size;
        Vector2 rectPoint = localPoint + Vector2.Scale(containerSize, levelContainer.pivot);
        if (rectPoint.x < 0f || rectPoint.y < 0f || rectPoint.x > containerSize.x || rectPoint.y > containerSize.y)
        {
            return false;
        }

        int gridX = Mathf.FloorToInt(rectPoint.x / cellSize);
        int gridY = Mathf.FloorToInt(rectPoint.y / cellSize);
        if (!currentLevelData.IsPositionValid(gridX, gridY))
        {
            return false;
        }

        if (currentLevelData.GetTile(gridX, gridY) != LevelTileType.Empty)
        {
            return false;
        }

        Vector2 anchoredPosition = new Vector2((gridX + 0.5f) * cellSize, (gridY + 0.5f) * cellSize);
        if (HasNearbyTank(anchoredPosition))
        {
            return false;
        }

        if (playerTank1 != null)
        {
            Destroy(playerTank1.gameObject);
            playerTank1 = null;
        }

        Player1TankType = tankType;
        playerEnergy -= energyCost;
        CreatePlayerTank(currentLevelData, cellSize, new Vector2Int(gridX, gridY), Color.white, 1, tankType, tank =>
        {
            playerTank1 = tank;
            if (playerTank1 != null)
            {
                playerTank1.RestoreState(Mathf.Max(1, Player1Health), Player1PowerLevel);
            }
        });

        return true;
    }

    public float PlayerEnergy => playerEnergy;
    public float MaxEnergy => MaxPlayerEnergy;
    public bool CanAffordPlayerTank(PlayerTankType tankType) => playerEnergy >= PlayerTank.GetEnergyCost(tankType);

    private bool IsBlockingTile(LevelTileType tileType)
    {
        return tileType == LevelTileType.Brick
            || tileType == LevelTileType.Steel
            || tileType == LevelTileType.Water
            || tileType == LevelTileType.Base;
    }

    private Color GetColorForTile(LevelTileType tileType)
    {
        return tileType switch
        {
            LevelTileType.Empty => new Color(0f, 0f, 0f, 0f),
            LevelTileType.Brick => new Color(0.7f, 0.3f, 0.1f),
            LevelTileType.Steel => new Color(0.5f, 0.5f, 0.5f),
            LevelTileType.Grass => new Color(0.1f, 0.7f, 0.1f),
            LevelTileType.Water => new Color(0.1f, 0.4f, 0.8f),
            LevelTileType.EnemySpawn => new Color(0.8f, 0.1f, 0.1f, 0.1f),
            LevelTileType.PlayerSpawn => new Color(0.1f, 0.8f, 0.1f, 0.1f),
            LevelTileType.Base => new Color(0.8f, 0.8f, 0.1f),
            _ => Color.clear
        };
    }

    private void CreateBoundary(LevelData levelData, float currentCellSize)
    {
        if (levelContainer == null || levelData == null)
        {
            return;
        }

        float width = levelData.width * currentCellSize;
        float height = levelData.height * currentCellSize;
        float thickness = currentCellSize * 0.5f;

        CreateBoundaryWall("Boundary_Bottom", new Vector2(width, thickness), new Vector2(0, -thickness));
        CreateBoundaryWall("Boundary_Top", new Vector2(width, thickness), new Vector2(0, height));
        CreateBoundaryWall("Boundary_Left", new Vector2(thickness, height), new Vector2(-thickness, 0));
        CreateBoundaryWall("Boundary_Right", new Vector2(thickness, height), new Vector2(width, 0));
    }

    private void CreateBoundaryWall(string name, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject boundary = new GameObject(name, typeof(RectTransform), typeof(BoxCollider2D), typeof(Rigidbody2D));
        boundary.transform.SetParent(levelContainer, false);
        boundary.layer = levelContainer.gameObject.layer;

        RectTransform rectTransform = boundary.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = Vector2.zero;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        BoxCollider2D collider = boundary.GetComponent<BoxCollider2D>();
        collider.size = size;
        collider.offset = size / 2f;

        Rigidbody2D rigidbody = boundary.GetComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Static;
        rigidbody.simulated = true;

        boundaryObjects.Add(boundary);
    }

    private void ClearLevelCells()
    {
        foreach (GameObject cell in levelCells)
        {
            if (cell != null)
            {
                Destroy(cell);
            }
        }

        levelCells.Clear();
    }

    private void ClearBoundaryObjects()
    {
        foreach (GameObject boundary in boundaryObjects)
        {
            if (boundary != null)
            {
                Destroy(boundary);
            }
        }

        boundaryObjects.Clear();
    }

    #region Event
    public sealed class PlayerDieEvent : GameEvent
    {
        public PlayerTank playerTank;

        public PlayerDieEvent(PlayerTank playerTank)
        {
            this.playerTank = playerTank;
        }
    }

    public sealed class EnemyDieEvent : GameEvent
    {
        public EnemyTank enemyTank;

        public EnemyDieEvent(EnemyTank enemyTank)
        {
            this.enemyTank = enemyTank;
        }
    }

    public sealed class EnemyCountChangedEvent : GameEvent
    {
        public int enemyCount;
        public int P1Health;
        public int P2Health;

        public EnemyCountChangedEvent(int enemyCount, int p1health, int p2health)
        {
            this.enemyCount = enemyCount;
            P1Health = p1health;
            P2Health = p2health;
        }
    }
    #endregion
}
