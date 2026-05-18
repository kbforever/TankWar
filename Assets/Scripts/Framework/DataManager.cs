using System;
using System.IO;
using UnityEngine;
using GameFramework;

[DisallowMultipleComponent]
public class DataManager : MonoBehaviour, IGameFeature
{
    [Header("数据存档配置")]
    [SerializeField] private string saveFileName = "gameSave.json";

    private string saverootPath;
    private GameData currentData;

    public bool IsActive { get; private set; }
    protected GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;

    public void Initialize()
    {
        // IsActive = true;
        
        currentData = new GameData();
        // Subscribe<GameStateChangedEvent>(OnStateChanged);
    }



    public void SetSavePath(GameMode gameMode)
    {
        saverootPath = Path.Combine(Application.persistentDataPath,gameMode.ToString());
        LoadGameData();
        currentData.gameMode = gameMode;
    }

    public void FeatureUpdate() { }
    public void FeatureFixedUpdate() { }
    public void FeatureLateUpdate() { }

    public void OnGameStateChanged(GameState previousState, GameState nextState)
    {
        
        
    }

    public void Shutdown()
    {
        IsActive = false;
        // SaveGameData();
        Unsubscribe<GameStateChangedEvent>(OnStateChanged);
    }

    public void SaveGameData()
    {
        if (string.IsNullOrEmpty(saverootPath)) return;

        if (!Directory.Exists(saverootPath))
        {
            Directory.CreateDirectory(saverootPath);
        }
        string json = JsonUtility.ToJson(currentData);
        string savePath = Path.Combine(saverootPath,saveFileName);
        File.WriteAllText(savePath, json);
        Debug.Log("游戏数据已保存: " + savePath);
    }

    public void LoadGameData()
    {
        string savePath = Path.Combine(saverootPath,saveFileName);
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            currentData = JsonUtility.FromJson<GameData>(json);
            Debug.Log("游戏数据已加载: " + savePath);
        }
        else
        {
            currentData = new GameData();
            Debug.Log("未找到存档文件，创建新数据");
        }
    }

    public GameData GetGameData()
    {
        return currentData;
    }

    public void SetGameData(GameData data)
    {
        currentData = data;
    }

    private void OnStateChanged(GameStateChangedEvent stateEvent)
    {
        OnGameStateChanged(stateEvent.PreviousState, stateEvent.NextState);
    }

    protected void Subscribe<T>(Action<T> callback) where T : GameEvent
    {
        Framework?.SubscribeEvent(callback);
    }

    protected void Unsubscribe<T>(Action<T> callback) where T : GameEvent
    {
        Framework?.UnsubscribeEvent(callback);
    }
}







[Serializable]
public class GameData
{
    public  int LevelIndex = 0;
    public int playerScore = 0;

    public int player1Health=3;

    public int player2Health=3;

    public int maxEnemyCount = 20;
    public Vector2 player1Position = Vector2.zero;

    public Vector2 player2Position = Vector2.zero;
    public int player1PowerLevel = 0;
    public int player2PowerLevel = 0;
    public int player1TankType = 0;
    public int player2TankType = 0;
    public float playerEnergy = 10f;

    public Vector2[] enmeyPositions = null;
    public int[] enemyTypes = null;
    public int[] enemyHealths = null;
    public bool[] enemyHasItems = null;
    public int[] enemyItemTypes = null;
    public Vector2[] itemPositions = null;
    public int[] itemTypes = null;

    public GameMode gameMode = GameMode.SinglePlayer;


    public GameData()
    {
    }
}

public enum GameMode
{
    SinglePlayer,
    TwoPlayer
}
