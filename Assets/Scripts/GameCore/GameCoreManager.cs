
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using LevelGeneration;
using UnityEngine.UI;
using Unity.VisualScripting;
using System;
using UnityEditor;
using UnityEngine.Video;

public class GameCoreManager : MonoBehaviour, IGameFeature
{


    private GameFramework.GameFramework Framework=>GameFramework.GameFramework.Instance;
    private DataManager dataManager;
    private LevelManager levelManager;
    private RectTransform levelContainer;
    private PlayerTank playerTank1;
    private PlayerTank playerTank2;


    private readonly List<GameObject> levelCells = new List<GameObject>();
    private readonly List<EnemyTank> enemyTanks = new List<EnemyTank>();
    private readonly List<GameObject> boundaryObjects = new List<GameObject>();

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
        levelContainer = transform.Find("levelContainer") as RectTransform;
        dataManager = Framework.GetFeature<DataManager>();
        levelManager = Framework.GetFeature<LevelManager>();
        if (levelManager != null)
        {
            Framework?.SubscribeEvent<LevelManager.LevelLoadedEvent>(OnLevelLoaded);
        }


        Framework.SubscribeEvent<GameStateChangedEvent>(onGameStateChanged);


        Framework.SubscribeEvent<BulletEvent>(CreateBullet);

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
            if (levelManager.CurrentLevelData == null)
            {
                Framework?.ChangeState(GameState.Loading);
            }
            else if(levelManager!=null && levelManager.CurrentLevelData != null)
            {
                RenderLevel(levelManager.CurrentLevelData);
            }
            
        }
    }

    public void Shutdown()
    {
         if (Framework != null)
        {
            Framework.UnsubscribeEvent<LevelManager.LevelLoadedEvent>(OnLevelLoaded);

            
            Framework.UnsubscribeEvent<GameStateChangedEvent>(onGameStateChanged);


            Framework.UnsubscribeEvent<BulletEvent>(CreateBullet);
        }
    }

    private void RenderLevel(LevelData levelData)
    {
        if (levelContainer == null || levelData == null) return;

        ClearLevelCells();
        ClearPlayerTank();
        ClearBoundaryObjects();

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
        float cellSize = Mathf.Min(cellWidth, cellHeight);

        // levelContainer.sizeDelta = new Vector2(cellSize * levelData.width, cellSize * levelData.height);

        for (int y = 0; y < levelData.height; y++)
        {
            for (int x = 0; x < levelData.width; x++)
            {
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
        SpawnEnemyTanks(levelData, cellSize);
    }

    private void SpawnPlayerTank(LevelData levelData, float cellSize)
    {
        if (levelContainer == null || levelData == null || dataManager == null) return;

        var gameMode = dataManager.GetGameData().gameMode;

        // 找到所有玩家出生点
        var spawnPoints = new List<Vector2Int>();
        for (int y = 0; y < levelData.height; y++)
        {
            for (int x = 0; x < levelData.width; x++)
            {
                if (levelData.GetTile(x, y) == LevelTileType.PlayerSpawn)
                {
                    spawnPoints.Add(new Vector2Int(x, y));
                }
            }
        }

        // 如果没有找到，使用默认位置
        if (spawnPoints.Count == 0)
        {
            spawnPoints.Add(new Vector2Int(levelData.width / 2, levelData.height / 2));
        }

        // 生成玩家坦克
        if (gameMode == GameMode.SinglePlayer && spawnPoints.Count == 1)
        {
            playerTank1 = CreatePlayerTank(levelData, cellSize, spawnPoints[0], Color.green, 1);
        }
        else if (gameMode == GameMode.TwoPlayer)
        {
            playerTank1 = CreatePlayerTank(levelData, cellSize, spawnPoints[0], Color.green, 1);
            if (spawnPoints.Count > 1)
            {
                playerTank2 = CreatePlayerTank(levelData, cellSize, spawnPoints[1], Color.blue, 2);
            }
            else
            {
                // 如果只有一个出生点，第二个玩家放在附近
                var pos2 = spawnPoints[0] + Vector2Int.right;
                if (levelData.IsPositionValid(pos2.x, pos2.y))
                {
                    playerTank2 = CreatePlayerTank(levelData, cellSize, pos2, Color.blue, 2);
                }
            }
        }
    }

    private PlayerTank CreatePlayerTank(LevelData levelData, float cellSize, Vector2Int spawnGrid, Color color, int playerIndex)
    {
        // GameObject tankObject = new GameObject($"PlayerTank{playerIndex}", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(PlayerTank));
        GameObject tankObject = Instantiate(Resources.Load<GameObject>("Prefabs/PlayerTank"));
        tankObject.transform.SetParent(levelContainer, false);

        var playerTank = tankObject.AddComponent<PlayerTank>();
        playerTank?.Initialize(cellSize, spawnGrid, new Vector2Int(levelData.width, levelData.height), color, playerIndex, levelData);
        return playerTank;
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
                Destroy(playerTank2.gameObject);
            }
            playerTank2 = null;
        }
    }

    private void SpawnEnemyTanks(LevelData levelData, float cellSize)
    {
        if (levelContainer == null || levelData == null) return;

        ClearEnemyTanks();

        // 找到所有敌方出生点
        var spawnPoints = new List<Vector2Int>();
        for (int y = 0; y < levelData.height; y++)
        {
            for (int x = 0; x < levelData.width; x++)
            {
                if (levelData.GetTile(x, y) == LevelTileType.EnemySpawn)
                {
                    spawnPoints.Add(new Vector2Int(x, y));
                }
            }
        }
        // 生成敌方坦克
        foreach (var spawnPoint in spawnPoints)
        {
            var enemyTank = CreateEnemyTank(levelData, cellSize, spawnPoint, Color.red);
            enemyTanks.Add(enemyTank);
        }
    }

    private EnemyTank CreateEnemyTank(LevelData levelData, float cellSize, Vector2Int spawnGrid, Color color)
    {
        // GameObject tankObject = new GameObject("EnemyTank", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(EnemyTank));
        GameObject tankObject = Instantiate(Resources.Load<GameObject>("Prefabs/EnemyTank"));
        
        tankObject.transform.SetParent(levelContainer, false);

        var enemyTank = tankObject.AddComponent<EnemyTank>();
        // tankObject.GetComponent<EnemyTank>();

        enemyTank?.Initialize(cellSize, spawnGrid, new Vector2Int(levelData.width, levelData.height), color, levelData);
        return enemyTank;
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
        var cell = new GameObject("TileCell", typeof(RectTransform), typeof(Image));
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
        GameObject bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet");
        

        GameObject bullet = Instantiate(bulletPrefab,parent.transform.position,Quaternion.identity);
        bullet.transform.SetParent(levelContainer);
        var Bullet = bullet.AddComponent<Bullet>();
        Bullet.movedir = parent.transform.up;
        bullet.tag = parent.tag;
        

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

  
}
