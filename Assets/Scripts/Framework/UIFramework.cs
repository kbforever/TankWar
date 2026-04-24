using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using UnityEngine.UI;
using System.IO;


[DisallowMultipleComponent]
public class UIFramework : MonoBehaviour, IGameFeature
{
    [Header("UI 框架配置")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private List<UIPanel> initialPanels = new List<UIPanel>();
    private MainMenuPanel mainMenuPanel;

    public Canvas MainCanvas { get => mainCanvas; set => mainCanvas = value; }

    private readonly Dictionary<string, UIPanel> panels = new Dictionary<string, UIPanel>();
    private readonly Stack<UIPanel> panelStack = new Stack<UIPanel>();

    public bool IsActive { get; private set; }
    protected GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;

    public void Initialize()
    {
        IsActive = true;
        // 创建 Canvas
        if(this.mainCanvas == null)
        {
            GameObject canvasObj = new GameObject("MainCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
            this.mainCanvas = canvas;
       
        }


        foreach (var panel in initialPanels)
        {
            if (panel != null)
            {
                RegisterPanel(panel);
                panel.Initialize();
            }
        }

        LoadAndRegisterPanel<MainMenuPanel>("MainMenuPanel");
        LoadAndRegisterPanel<GamePanel>("GamePanel");
        LoadAndRegisterPanel<PausePanel>("PausePanel");
        Subscribe<GameStateChangedEvent>(OnStateChanged);
    }

    public void FeatureUpdate() { }
    public void FeatureFixedUpdate() { }
    public void FeatureLateUpdate() { }

    public void OnGameStateChanged(GameState previousState, GameState nextState)
    {



        // 根据游戏状态显示/隐藏 UI
        if (nextState == GameState.MainMenu)
        {
            HideAllPanels();
            ShowPanel("MainMenuPanel");
            
        }
        else if (nextState == GameState.Playing)
        {
            HideTopPanel();
            ShowPanel("GamePanel");
        }
        else if (nextState == GameState.Paused)
        {
            
            ShowPanel("PausePanel");
        }
        else if (nextState == GameState.GameOver)
        {
            ShowPanel("GameOverPanel");
        }
    }

    public void Shutdown()
    {
        IsActive = false;
        foreach (var panel in panels.Values)
        {
            if (panel != null)
            {
                panel.Shutdown();
            }
        }
        panels.Clear();
        panelStack.Clear();
        Unsubscribe<GameStateChangedEvent>(OnStateChanged);
    }

    public void RegisterPanel(UIPanel panel)
    {
        if (panel == null || panels.ContainsKey(panel.PanelName)) return;
        panels[panel.PanelName] = panel;
        
        panel.gameObject.SetActive(false);
    }

    public UIPanel GetPanel(string panelName)
    {
        panels.TryGetValue(panelName, out var panel);
        return panel;
    }

    public void LoadAndRegisterPanel<T>(string panelName) where T : UIPanel
    {
        string prefabPath = "Prefabs/" + panelName;
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, mainCanvas.transform);
            instance.name = panelName;
            T panel = instance.GetComponent<T>();
            if (panel == null)
            {
                panel = instance.AddComponent<T>();
            }
            RegisterPanel(panel);
            panel.Initialize();
            panel.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError($"Failed to load prefab at path: {prefabPath}. Make sure the prefab exists in Assets/Resources/Prefabs/ and is named {panelName}.prefab");
        }
    }

    public void ShowPanel(string panelName)
    {
        if (!panels.TryGetValue(panelName, out var panel)) return;
        panel.gameObject.SetActive(true);
        panel.OnShow();
        panelStack.Push(panel);
    }

    public void HidePanel(string panelName)
    {
        if (!panels.TryGetValue(panelName, out var panel)) return;
        panel.gameObject.SetActive(false);
        panel.OnHide();
        if (panelStack.Count > 0 && panelStack.Peek() == panel)
        {
            panelStack.Pop();
        }
    }

    public void HideAllPanels()
    {
        while (panelStack.Count > 0)
        {
            var panel = panelStack.Pop();
            panel.gameObject.SetActive(false);
            panel.OnHide();
        }
    }

    public void HideTopPanel()
    {
        if (panelStack.Count == 0) return;
        var topPanel = panelStack.Pop();
        topPanel.gameObject.SetActive(false);
        topPanel.OnHide();
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

public abstract class UIPanel : MonoBehaviour
{
    public string PanelName => gameObject.name;

    public virtual void Initialize() { }
    public virtual void OnShow() { }
    public virtual void OnHide() { }
    public virtual void Shutdown() { }
}