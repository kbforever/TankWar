using System;
using System.Collections;
using System.IO;
using UnityEngine;
using GameFramework;
using LevelGeneration;

[DisallowMultipleComponent]
public class LevelManager : MonoBehaviour, IGameFeature
{
    [Header("关卡管理配置")]
    [SerializeField] private int defaultLevelWidth = 26;
    [SerializeField] private int defaultLevelHeight = 26;
    [SerializeField] private bool loadLevelFromResources = true;
    [SerializeField] private string resourceLevelPath = "Levels/level";


    public int CurrentLevelIndex => currentLevelIndex;  
    private int currentLevelIndex = -1;
    private bool isLoading = false;
    private LevelData currentLevelData;

    public LevelData CurrentLevelData => currentLevelData;
    public bool IsActive { get; private set; }
    protected GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;

    public void Initialize()
    {
        IsActive = true;
        currentLevelData = null;
        // Subscribe<GameStateChangedEvent>(OnStateChanged);
        Subscribe<LoadLevelEvent>(OnLoadLevel);
    }

    public void FeatureUpdate() { }
    public void FeatureFixedUpdate() { }
    public void FeatureLateUpdate() { }

    public void OnGameStateChanged(GameState previousState, GameState nextState)
    {
        if (nextState == GameState.MainMenu)
        {
            LoadMainMenu();
        }
        else if (nextState == GameState.Loading)
        {
            if (currentLevelData == null)
            {
                LoadLevel(currentLevelIndex);
            }
            
        }
    }

    public void Shutdown()
    {
        IsActive = false;
        Unsubscribe<GameStateChangedEvent>(OnStateChanged);
        Unsubscribe<LoadLevelEvent>(OnLoadLevel);
    }

    public void LoadMainMenu()
    {
        if (isLoading) return;
        
    }

    public void LoadLevel(int levelIndex)
    {
        if (isLoading || levelIndex < 0) return;
        currentLevelIndex = levelIndex;
        
        StartCoroutine(LoadLevelDataAsync(levelIndex));
    }

    public void LoadNextLevel()
    {
        currentLevelIndex++;
        LoadLevel(currentLevelIndex);
    }

    public void RestartLevel()
    {
        LoadLevel(currentLevelIndex);
    }

    public int GetCurrentLevelIndex()
    {
        return currentLevelIndex;
    }

    public string GetCurrentLevelName()
    {
        return currentLevelData != null ? currentLevelData.levelName : $"Level_{currentLevelIndex}";
    }

    private IEnumerator LoadLevelDataAsync(int levelIndex)
    {
        isLoading = true;
        yield return null;

        currentLevelData = LoadLevelData(levelIndex);

        isLoading = false;
        // PublishLevelLoaded();
        Framework.ChangeState(GameState.Playing);
        // if (Framework?.CurrentState == GameState.Loading)
        // {
            
        // }
    }

    private LevelData LoadLevelData(int levelIndex)
    {
        if (loadLevelFromResources)
        {
            string resourcePath = resourceLevelPath + levelIndex;
            TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset != null)
            {
                var loadedData = LevelData.FromJson(textAsset.text);
                if (loadedData != null && loadedData.IsValid)
                {
                    return loadedData;
                }
            }
        }

        return LevelGenerator.Generate(levelIndex, defaultLevelWidth, defaultLevelHeight);
    }

    public string GetLevelSavePath(string levelName)
    {
        if (string.IsNullOrEmpty(levelName)) return null;

        string directory = Path.Combine(Application.persistentDataPath, "Levels");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return Path.Combine(directory, levelName + ".json");
    }

    public bool SaveLevelToFile(LevelData levelData, string levelName)
    {
        if (levelData == null || string.IsNullOrEmpty(levelName)) return false;
        string path = GetLevelSavePath(levelName);
        if (string.IsNullOrEmpty(path)) return false;

        try
        {
            File.WriteAllText(path, levelData.ToJson(true));
            Debug.Log($"关卡已保存: {path}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"保存关卡失败: {ex.Message}");
            return false;
        }
    }

    public LevelData LoadLevelFromFile(string levelName)
    {
        if (string.IsNullOrEmpty(levelName)) return null;
        string path = GetLevelSavePath(levelName);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            var loadedData = LevelData.FromJson(json);
            if (loadedData != null && loadedData.IsValid)
            {
                Debug.Log($"关卡已加载: {path}");
                return loadedData;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"读取关卡失败: {ex.Message}");
        }

        return null;
    }

    private void PublishLevelLoaded()
    {
        if (currentLevelData == null) return;
        Framework?.PublishEvent(new LevelLoadedEvent(currentLevelData));
    }

    private void OnStateChanged(GameStateChangedEvent stateEvent)
    {
        OnGameStateChanged(stateEvent.PreviousState, stateEvent.NextState);
    }

    private void OnLoadLevel(LoadLevelEvent loadEvent)
    {
        LoadLevel(loadEvent.LevelIndex);
    }

    protected void Subscribe<T>(Action<T> callback) where T : GameEvent
    {
        Framework?.SubscribeEvent(callback);
    }

    protected void Unsubscribe<T>(Action<T> callback) where T : GameEvent
    {
        Framework?.UnsubscribeEvent(callback);
    }

    public sealed class LoadLevelEvent : GameEvent
    {
        public int LevelIndex { get; }

        public LoadLevelEvent(int levelIndex)
        {
            LevelIndex = levelIndex;
        }
    }

    public sealed class LevelLoadedEvent : GameEvent
    {
        public LevelData LevelData { get; }

        public LevelLoadedEvent(LevelData levelData)
        {
            LevelData = levelData;
        }
    }
}