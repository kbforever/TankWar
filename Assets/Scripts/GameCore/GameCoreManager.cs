
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using LevelGeneration;
using UnityEngine.UI;
using System.Linq;
using System.Collections;


public class GameCoreManager : MonoBehaviour, IGameFeature
{


    private GameFramework.GameFramework Framework=>GameFramework.GameFramework.Instance;
    private DataManager dataManager;
    private InputManager inputManager;
    private LevelManager levelManager;
    private RectTransform levelContainer;
    private PlayerTank playerTank1;
    private PlayerTank playerTank2;



    private int _CurEnemyCount;

    public int CurEnemyCount
    {
        get=>_CurEnemyCount;
        set
        {
            if (_CurEnemyCount != value)
            {
                _CurEnemyCount=value;
                Framework?.PublishEvent<EnemyCountChangedEvent>(new EnemyCountChangedEvent(_CurEnemyCount,Player1Health,Player2Health));
            }
        }
    }

    private int Player1Health;

    private int Player2Health;

    private float cellSize;
    private List<Vector2Int> spawnEnemyPoints;
    private List<Vector2Int> spawnPlayerPoints;

    private GameMode currentGameMode;
    private GameData currentGameData=> dataManager.GetGameData();
    private LevelData currentLevelData;
    private readonly List<GameObject> levelCells = new List<GameObject>();
    public List<EnemyTank> enemyTanks = new List<EnemyTank>();
    private readonly List<GameObject> boundaryObjects = new List<GameObject>();
    private int spawnContextVersion;
    private const float TankSpawnEffectDuration = 1.5f;
    private const float PlayerShieldDuration = 1.5f;

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
        public bool ConsumeEnemyCount;
        public int SpawnContextVersion;
    }



    #region Prefabs
    private GameObject PlayerPrefab;
    private GameObject EnemyPrefab;
    private GameObject SpawnEffectPrefab;
    private GameObject ShieldEffectPrefab;
    private GameObject DieEffectPrefab;

    private GameObject BulletPrefab;
    private GameObject[] CellPrefabs;

    #endregion

    public bool IsActive{get;private set;}

    public void FeatureFixedUpdate()
    {
        
    }

    public void FeatureLateUpdate()
    {
        
    }

    public void FeatureUpdate()
    {
       
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


        Framework.SubscribeEvent<GameStateChangedEvent>(onGameStateChanged);


        Framework.SubscribeEvent<BulletEvent>(CreateBullet);
        Framework.SubscribeEvent<PlayerDieEvent>(PlayerDie);
        Framework.SubscribeEvent<EnemyDieEvent>(EnemyDie);
        
        
    }


    bool isInit=false;
    async void LoadPrefabsAndLevels()
    {
        Framework.ChangeState(GameState.Loading);
        if(!isInit)
        {
            
            string rootPrefabsPath = "Assets/Prefabs/Maps/";
            // PlayerPrefab = ResourceManager.LoadResource<GameObject>("Prefabs/Maps/PlayerTank1");
            PlayerPrefab = await ResourceManager.AsycnLoadAddressable<GameObject>(rootPrefabsPath+ "PlayerTank1.prefab");

            // EnemyPrefab = ResourceManager.LoadResource<GameObject>("Prefabs/Maps/EnemyTank");
            EnemyPrefab = await ResourceManager.AsycnLoadAddressable<GameObject>(rootPrefabsPath+"EnemyTank.prefab");
            SpawnEffectPrefab = await ResourceManager.AsycnLoadAddressable<GameObject>("Assets/Prefabs/Effect/SpawnEffect.prefab");
            ShieldEffectPrefab = await ResourceManager.AsycnLoadAddressable<GameObject>("Assets/Prefabs/Effect/ShieldEffect.prefab");
            DieEffectPrefab = await ResourceManager.AsycnLoadAddressable<GameObject>("Assets/Prefabs/Effect/DieEffect.prefab");

            CellPrefabs = new GameObject[(int)LevelTileType.Base];
            // BulletPrefab = ResourceManager.LoadResource<GameObject>("Prefabs/Maps/Bullet");

            BulletPrefab = await ResourceManager.AsycnLoadAddressable<GameObject>(rootPrefabsPath+"Bullet.prefab");

            for(int i = 0; i < (int)LevelTileType.Base; i++)
            {
                var tile = (LevelTileType)i;
                if(tile==LevelTileType.EnemySpawn||tile==LevelTileType.PlayerSpawn) continue;
                CellPrefabs[i] = await ResourceManager.AsycnLoadAddressable<GameObject>(rootPrefabsPath+tile.ToString()+".prefab");
            }
            
        }
        isInit=true;
        levelManager.LoadLevel(currentGameData.LevelIndex);

    }


    private void EnemyDie(EnemyDieEvent enemyDieEvent)
    {
        EnemyTank enemyTank = enemyDieEvent.enemyTank;
        enemyTanks.Remove(enemyTank);
        PlayTankDieEffect(enemyTank);
        Destroy(enemyTank.gameObject);
        
        if(CurEnemyCount>0) SpawnEnemyTanks(currentLevelData, cellSize,true);
        else
        {
            if (enemyTanks.Count <= 0)
            {
                 // 游戏胜利 如:跳入下一关 or 显示胜利界面
                Debug.Log("You Win!");
                dataManager.SetGameData(new GameData()
                {
                    gameMode = currentGameMode
                });
                Framework.ChangeState(GameState.Loading);
                levelManager.LoadNextLevel();
            }
           
        }
    }

    private void PlayerDie(PlayerDieEvent playerDieEvent)
    {
        var playerTank = playerDieEvent.playerTank;
        PlayTankDieEffect(playerTank);
        if (playerTank == playerTank1)
        {
            Player1Health--;
            if(Player1Health>0) CreatePlayerTank(currentLevelData, cellSize, spawnPlayerPoints[0], Color.green, 1, tank => playerTank1 = tank);
        }
        if(playerTank == playerTank2)
        {
            Player2Health--;
            if(Player2Health>0) CreatePlayerTank(currentLevelData, cellSize, spawnPlayerPoints[1], Color.blue, 2, tank => playerTank2 = tank);
        }

        Destroy(playerTank.gameObject);
        Debug.LogError(Player1Health+"_"+Player2Health);
        if(Player1Health<=0 && Player2Health<=0)
        {
            
            Debug.LogError("Game Over");
            Framework.ChangeState(GameState.GameOver);
        }
        Framework?.PublishEvent<EnemyCountChangedEvent>(new EnemyCountChangedEvent(CurEnemyCount,Player1Health,Player2Health));

    }

    private void CreateBullet(BulletEvent bulletEvent)
    {
        CreateBullet(bulletEvent.gameObject);
    }

    private void onGameStateChanged(GameStateChangedEvent changeevent)
    {
        OnGameStateChanged(changeevent.PreviousState,changeevent.NextState);
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
            if(previousState == GameState.Paused)
            {
                inputManager.EnableMaps();
                Time.timeScale = 1f;
                return;
            }

            
            if (previousState == GameState.MainMenu && levelManager.CurrentLevelIndex!=currentGameData.LevelIndex) 
            {
                
                LoadPrefabsAndLevels();
                // levelManager.LoadLevel(currentGameData.LevelIndex);
                return;
                
            }
            if(levelManager.CurrentLevelData != null)
            {
                
                currentLevelData = levelManager.CurrentLevelData;

                
                
                if(currentGameData.enmeyPositions != null && currentGameData.player1Health>0)
                {
                    RenderLevel(currentLevelData,false);

                   
                    Player1Health = currentGameData.player1Health;
                    Player2Health = currentGameData.gameMode==GameMode.SinglePlayer? 0 : currentGameData.player2Health;
                    CurEnemyCount = currentGameData.maxEnemyCount+currentGameData.enmeyPositions.Length;
                    // 恢复玩家位置
                    if (playerTank1 != null)
                    {
                        playerTank1.GetComponent<RectTransform>().anchoredPosition = currentGameData.player1Position;
                        if(Player1Health<=0)
                        {
                            Destroy(playerTank1.gameObject);
                            playerTank1 = null;
                        }
                    }
                    if (playerTank2 != null)
                    {
                        playerTank2.GetComponent<RectTransform>().anchoredPosition = currentGameData.player2Position;
                        if(Player2Health<=0)
                        {
                            Destroy(playerTank2.gameObject);
                            playerTank2 = null;
                        }
                    }
                    
                    
                    enemyTanks.Clear();
                    // 恢复敌人位置
                    for (int i = 0; i < currentGameData.enmeyPositions.Length; i++)
                    {
                        CreateEnemyTank(cellSize,currentGameData.enmeyPositions[i], Color.white, false, tank => enemyTanks.Add(tank));
                    }
                }
                else
                {
                    RenderLevel(currentLevelData);
                    
                    UpdateGameData();
                }

                Time.timeScale = 1f;
                inputManager.EnableMaps();
            }

            

         
            
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
            UpdateGameData();
            
        }
        
        
        
    }

    void OnApplicationQuit()
    {
        if (Framework != null)
        {
            Framework.UnsubscribeEvent<LevelManager.LevelLoadedEvent>(OnLevelLoaded);

            
            Framework.UnsubscribeEvent<GameStateChangedEvent>(onGameStateChanged);


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
        ClearBoundaryObjects();
    }
    public void Shutdown()
    {
        
    }

    private void RenderLevel(LevelData levelData,bool GenEnemy = true)
    {
        if (levelContainer == null || levelData == null) return;

        spawnContextVersion++;
        ClearLevelCells();
        ClearPlayerTank();
        ClearBoundaryObjects();
        ClearEnemyTanks();

        this.Player1Health= levelData.player1Health;
        this.Player2Health= levelData.player2Health;
        this.CurEnemyCount = levelData.maxEnemyCount;
        float cellSpaceSize = 5f; // 每个格子碰撞器往里收缩的大小
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
        // levelContainer.sizeDelta = new Vector2(cellSize * levelData.width, cellSize * levelData.height);

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
                var tileType = levelData.GetTile(x, y);
                var cell = CreateCell(tileType);
                cell.name = $"Tile_{x}_{y}";
                cell.transform.SetParent(levelContainer, false);
                var rectTransform = cell.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(0, 0);
                rectTransform.pivot = new Vector2(0, 0); 
                rectTransform.anchoredPosition = new Vector2(x * cellSize, y * cellSize);
                rectTransform.sizeDelta = new Vector2(cellSize, cellSize);

                var obstacleCollider = cell.GetComponent<BoxCollider2D>();
                if (obstacleCollider != null)
                {
                    obstacleCollider.size = new Vector2(cellSize-cellSpaceSize, cellSize-cellSpaceSize);
                    obstacleCollider.offset = new Vector2(cellSize / 2f, cellSize / 2f);
                }

                levelCells.Add(cell);
            }
        }

        CreateBoundary(levelData, cellSize);
        SpawnPlayerTank(levelData, cellSize);
        if(GenEnemy) SpawnEnemyTanks(levelData, cellSize);
    }

    private void SpawnPlayerTank(LevelData levelData, float cellSize)
    {
        if (levelContainer == null || levelData == null || dataManager == null) return;

        var gameMode = currentGameMode;

        

        // 如果没有找到，使用默认位置
        if (spawnPlayerPoints.Count == 0)
        {
            spawnPlayerPoints.Add(new Vector2Int(levelData.width / 2, levelData.height / 2));
        }

        // 生成玩家坦克
        if (gameMode == GameMode.SinglePlayer && spawnPlayerPoints.Count >= 1)
        {
            this.Player2Health=0;
            CreatePlayerTank(levelData, cellSize, spawnPlayerPoints[0], Color.white, 1, tank => playerTank1 = tank);
        }
        else if (gameMode == GameMode.TwoPlayer)
        {
            CreatePlayerTank(levelData, cellSize, spawnPlayerPoints[0], Color.white, 1, tank => playerTank1 = tank);
            if (spawnPlayerPoints.Count > 1)
            {
                CreatePlayerTank(levelData, cellSize, spawnPlayerPoints[1], Color.blue, 2, tank => playerTank2 = tank);
            }
            else
            {
                // 如果只有一个出生点，第二个玩家放在附近
                var pos2 = spawnPlayerPoints[0] + Vector2Int.right;
                if (levelData.IsPositionValid(pos2.x, pos2.y))
                {
                    CreatePlayerTank(levelData, cellSize, pos2, Color.blue, 2, tank => playerTank2 = tank);
                }
            }
        }
    }

    private void CreatePlayerTank(LevelData levelData, float cellSize, Vector2Int spawnGrid, Color color, int playerIndex, System.Action<PlayerTank> onSpawned = null)
    {
        StartCoroutine(SpawnTankRoutine<PlayerTank>(new TankSpawnRequest
        {
            Kind = TankSpawnKind.Player,
            Prefab = PlayerPrefab,
            LevelData = levelData,
            SpawnGrid = spawnGrid,
            Color = Color.white,
            PlayerIndex = playerIndex,
            SpawnContextVersion = spawnContextVersion
        }, onSpawned));
    }

    private void ClearPlayerTank()
    {
        if (playerTank1 != null)
        {
            if (playerTank1.gameObject != null)
            {
                Destroy(playerTank1.gameObject);
            }
            playerTank1 = null;
        }

        if (playerTank2 != null)
        {
            if (playerTank2.gameObject != null)
            {
                PlayTankDieEffect(playerTank2);
                Destroy(playerTank2.gameObject);
            }
            playerTank2 = null;
        }
    }

    private void SpawnEnemyTanks(LevelData levelData, float cellSize,bool GenOneEnmey = false)
    {
        if (levelContainer == null || levelData == null) return;

        // ClearEnemyTanks();

        // 找到所有敌方出生点
        
        // 生成敌方坦克
        // 每个出生地都生成
        if (!GenOneEnmey)
        {
            foreach (var spawnPoint in spawnEnemyPoints)
            {
                CreateEnemyTank(cellSize, spawnPoint, Color.red, tank => enemyTanks.Add(tank));
            }
        }
        // 随机一个出生地生成一个敌人
        else
        {
            var randomSpawnPoint = spawnEnemyPoints[Random.Range(0, spawnEnemyPoints.Count)];
            CreateEnemyTank(cellSize, randomSpawnPoint, Color.red, tank => enemyTanks.Add(tank));
        }
    }

    private void CreateEnemyTank(float cellSize, Vector2Int spawnGrid, Color color, System.Action<EnemyTank> onSpawned = null)
    {
        StartCoroutine(SpawnTankRoutine<EnemyTank>(new TankSpawnRequest
        {
            Kind = TankSpawnKind.Enemy,
            Prefab = EnemyPrefab,
            SpawnGrid = spawnGrid,
            Color = Color.white,
            EnemyId = CurEnemyCount--,
            ConsumeEnemyCount = true,
            SpawnContextVersion = spawnContextVersion
        }, onSpawned));
    }


    private void CreateEnemyTank(float cellSize,Vector2 pos, Color color,bool IsRestore=false, System.Action<EnemyTank> onSpawned = null)
    {
        StartCoroutine(SpawnTankRoutine<EnemyTank>(new TankSpawnRequest
        {
            Kind = TankSpawnKind.Enemy,
            Prefab = EnemyPrefab,
            AnchoredPosition = pos,
            UseAnchoredPosition = true,
            Color = color,
            EnemyId = CurEnemyCount--,
            ConsumeEnemyCount = !IsRestore,
            SpawnContextVersion = spawnContextVersion
        }, onSpawned));
    }

    private IEnumerator SpawnTankRoutine<T>(TankSpawnRequest request, System.Action<T> onSpawned) where T : Component
    {
        if (request.Prefab == null || levelContainer == null)
        {
            yield break;
        }

        GameObject spawnEffect = PlayTankSpawnEffect(request);
        yield return new WaitForSeconds(TankSpawnEffectDuration);

        if (spawnEffect != null)
        {
            Destroy(spawnEffect);
        }

        if (!IsSpawnRequestValid(request))
        {
            yield break;
        }

        T tank = SpawnTank<T>(request);
        if (tank == null)
        {
            yield break;
        }

        onSpawned?.Invoke(tank);

        if (tank is PlayerTank playerTank)
        {
            yield return StartCoroutine(PlayPlayerShieldEffect(playerTank));
        }
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

    private Vector2 GetSpawnAnchoredPosition(TankSpawnRequest request)
    {
        if (request.UseAnchoredPosition)
        {
            return request.AnchoredPosition;
        }

        return new Vector2((request.SpawnGrid.x+0.5f ) * cellSize, (request.SpawnGrid.y+0.5f) * cellSize);
    }

    private void SetupEffectObject(GameObject effectObject, Vector2 anchoredPosition, Transform parent)
    {
        effectObject.transform.SetParent(parent, false);
        effectObject.transform.SetAsLastSibling();

        var rectTransform = effectObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = anchoredPosition==Vector2.zero? new Vector2(cellSize*1.2f,cellSize*1.2f):new Vector2(cellSize,cellSize);
        }
        else
        {
            effectObject.transform.localPosition = anchoredPosition;
        }
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
                enemyTank.Initialize(cellSize, request.AnchoredPosition, request.Color, request.EnemyId);
            }
            else
            {
                enemyTank.Initialize(cellSize, request.SpawnGrid, request.Color, request.EnemyId);
            }
        }
    }

    private GameObject PlayTankSpawnEffect(TankSpawnRequest request)
    {
        if (SpawnEffectPrefab == null || levelContainer == null)
        {
            return null;
        }

        GameObject effectObject = Instantiate(SpawnEffectPrefab);
        SetupEffectObject(effectObject, GetSpawnAnchoredPosition(request), levelContainer);
        return effectObject;
    }

    private IEnumerator PlayPlayerShieldEffect(PlayerTank playerTank)
    {
        if (playerTank == null)
        {
            yield break;
        }

        playerTank.SetShielded(true);
        GameObject shieldEffect = null;
        if (ShieldEffectPrefab != null)
        {
            shieldEffect = Instantiate(ShieldEffectPrefab);
            SetupEffectObject(shieldEffect, new Vector2(0.5f*cellSize,0.5f*cellSize), playerTank.transform);
        }

        yield return new WaitForSeconds(PlayerShieldDuration);

        if (shieldEffect != null)
        {
            Destroy(shieldEffect);
        }

        if (playerTank != null)
        {
            playerTank.SetShielded(false);
        }
    }

    private void PlayTankDieEffect(Component tank)
    {
        if (tank == null || DieEffectPrefab == null || levelContainer == null)
        {
            return;
        }

        RectTransform tankRectTransform = tank.GetComponent<RectTransform>();
        if (tankRectTransform == null)
        {
            return;
        }

        GameObject effectObject = Instantiate(DieEffectPrefab);
        SetupEffectObject(effectObject, tankRectTransform.anchoredPosition, levelContainer);
        StartCoroutine(DestroyEffectWhenFinished(effectObject));
    }

    private IEnumerator DestroyEffectWhenFinished(GameObject effectObject)
    {
        if (effectObject == null)
        {
            yield break;
        }

        Animator animator = effectObject.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            yield return null;

            while (effectObject != null && animator != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (!animator.IsInTransition(0) && stateInfo.normalizedTime >= 1f)
                {
                    break;
                }

                yield return null;
            }
        }
        else
        {
            ParticleSystem[] particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>();
            if (particleSystems.Length > 0)
            {
                while (effectObject != null)
                {
                    bool isAlive = false;
                    foreach (ParticleSystem particleSystem in particleSystems)
                    {
                        if (particleSystem != null && particleSystem.IsAlive(true))
                        {
                            isAlive = true;
                            break;
                        }
                    }

                    if (!isAlive)
                    {
                        break;
                    }

                    yield return null;
                }
            }
            else
            {
                yield return null;
            }
        }

        if (effectObject != null)
        {
            Destroy(effectObject);
        }
    }
    
    private void ClearEnemyTanks()
    {
        foreach (var enemyTank in enemyTanks)
        {
            if (enemyTank != null && enemyTank.gameObject != null)
            {
                Destroy(enemyTank.gameObject);
            }
        }
        enemyTanks.Clear();
       
    }

    private GameObject CreateCell(LevelTileType tileType)
    {
        // var cell = new GameObject("TileCell", typeof(RectTransform), typeof(Image));
        
        if(tileType==LevelTileType.PlayerSpawn || tileType == LevelTileType.EnemySpawn)
        {
            return CreateCell(LevelTileType.Empty);
        }
        var cell = Instantiate(CellPrefabs[(int)tileType]);

        var image = cell.GetComponent<Image>();
        image.color = GetColorForTile(tileType);

        if (IsBlockingTile(tileType))
        {
            var collider = cell.AddComponent<BoxCollider2D>();
            collider.offset = Vector2.zero;
            collider.size = Vector2.one;
            var rigidbody = cell.AddComponent<Rigidbody2D>();
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
            gameMode = dataManager.GetGameData().gameMode,
            LevelIndex = levelManager.CurrentLevelIndex,
            player1Health = Player1Health,
            player2Health = Player2Health,
            maxEnemyCount = CurEnemyCount
            
        };
        gameData.enmeyPositions = enemyTanks!=null? new Vector2[enemyTanks.Count] : null;
        foreach (var enemyTank in enemyTanks)
        {
            if (enemyTank != null)
            {
                gameData.enmeyPositions[enemyTanks.IndexOf(enemyTank)] = enemyTank.GetComponent<RectTransform>().anchoredPosition;
            }
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
        

        GameObject bullet = Instantiate(BulletPrefab,parent.transform.position,Quaternion.identity);
        bullet.transform.SetParent(levelContainer);
        var Bullet = bullet.AddComponent<Bullet>();
        Bullet.movedir = parent.transform.up;
        Bullet.selfTag = parent.tag;
        

    }


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
            LevelTileType.EnemySpawn => new Color(0.8f, 0.1f, 0.1f,0.1f),
            LevelTileType.PlayerSpawn => new Color(0.1f, 0.8f, 0.1f,0.1f),
            LevelTileType.Base => new Color(0.8f, 0.8f, 0.1f),



            _ => Color.clear,
        };
    }

    private void CreateBoundary(LevelData levelData, float cellSize)
    {
        if (levelContainer == null || levelData == null) return;

        float width = levelData.width * cellSize;
        float height = levelData.height * cellSize;
        float thickness = cellSize * 0.5f;
        
        CreateBoundaryWall("Boundary_Bottom", new Vector2(width, thickness), new Vector2(0, -thickness));
        CreateBoundaryWall("Boundary_Top", new Vector2(width, thickness), new Vector2(0, height));
        CreateBoundaryWall("Boundary_Left", new Vector2(thickness, height), new Vector2(-thickness,0));
        CreateBoundaryWall("Boundary_Right", new Vector2(thickness, height), new Vector2(width, 0));
    }

    private void CreateBoundaryWall(string name, Vector2 size, Vector2 anchoredPosition)
    {
        var boundary = new GameObject(name, typeof(RectTransform), typeof(BoxCollider2D), typeof(Rigidbody2D));
        boundary.transform.SetParent(levelContainer, false);
        boundary.layer = levelContainer.gameObject.layer;


        var rectTransform = boundary.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0, 0);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        var collider = boundary.GetComponent<BoxCollider2D>();
        collider.size = size;
        collider.offset = size / 2f;

        var rigidbody = boundary.GetComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Static; 
        rigidbody.simulated = true;

        boundaryObjects.Add(boundary);
    }

    private void ClearLevelCells()
    {
        foreach (var cell in levelCells)
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
        foreach (var boundary in boundaryObjects)
        {
            if (boundary != null)
            {
                Destroy(boundary);
            }
        }
        boundaryObjects.Clear();
    }

    

    #region Event
    public sealed class PlayerDieEvent: GameEvent
    {
        public PlayerTank playerTank;
        public PlayerDieEvent(PlayerTank playerTank)
        {
            this.playerTank = playerTank;
        }
    }

    public sealed class EnemyDieEvent: GameEvent
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
        public EnemyCountChangedEvent(int enemyCount,int p1health,int p2health)
        {
            this.enemyCount=enemyCount;
            this.P1Health = p1health;
            this.P2Health = p2health;
        }
    }
    #endregion
}
