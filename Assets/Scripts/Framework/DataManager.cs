using System;
using System.IO;
using UnityEngine;
using GameFramework;

[DisallowMultipleComponent]
public class DataManager : MonoBehaviour, IGameFeature
{
    [Header("数据存档配置")]
    [SerializeField] private string saveFileName = "gameSave.json";

    private string savePath;
    private GameData currentData;

    public bool IsActive { get; private set; }
    protected GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;

    public void Initialize()
    {
        IsActive = true;
        savePath = Path.Combine(Application.persistentDataPath, saveFileName);
        LoadGameData();
        Subscribe<GameStateChangedEvent>(OnStateChanged);
    }

    public void FeatureUpdate() { }
    public void FeatureFixedUpdate() { }
    public void FeatureLateUpdate() { }

    public void OnGameStateChanged(GameState previousState, GameState nextState)
    {
        // 在游戏结束时自动保存
        if (nextState == GameState.GameOver)
        {
            SaveGameData();
        }
    }

    public void Shutdown()
    {
        IsActive = false;
        SaveGameData();
        Unsubscribe<GameStateChangedEvent>(OnStateChanged);
    }

    public void SaveGameData()
    {
        if (currentData == null) return;
        string json = JsonUtility.ToJson(currentData);
        File.WriteAllText(savePath, json);
        Debug.Log("游戏数据已保存: " + savePath);
    }

    public void LoadGameData()
    {
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
    public int playerLevel = 1;
    public int playerScore = 0;
    public Vector2 playerPosition = Vector2.zero;
    public string lastSaveTime = "";
    public GameMode gameMode = GameMode.SinglePlayer;

    public GameData()
    {
        lastSaveTime = DateTime.Now.ToString();
    }
}

public enum GameMode
{
    SinglePlayer,
    TwoPlayer
}