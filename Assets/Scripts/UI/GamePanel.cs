using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameFramework;
using LevelGeneration;
using Unity.VisualScripting;


public class GamePanel : UIPanel
{
    [Header("游戏面板")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Button pauseButton;

    private UIFramework uiFramework;

    private DataManager dataManager;
    protected GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;

    public override void Initialize()
    {
        base.Initialize();
        uiFramework = Framework.GetFeature<UIFramework>();
        dataManager = Framework.GetFeature<DataManager>();
        // 自动获取 UI 元素
        scoreText = transform.Find("scoreText")?.GetComponent<Text>();
        pauseButton = transform.Find("pauseButton")?.GetComponent<Button>();
        var gamecoreManager = this.gameObject.AddComponent<GameCoreManager>();
        Framework.RegisterFeature(gamecoreManager);
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseGame);
        }

        UpdateScore();

        
    }

    public override void OnShow()
    {
        base.OnShow();
        UpdateScore();
        
    }

    public override void OnHide()
    {
        base.OnHide();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(OnPauseGame);
        }

       
    }

    private void OnPauseGame()
    {
        var framework = GameFramework.GameFramework.Instance;
        if (framework != null)
        {
            framework.ChangeState(GameState.Paused);
        }
    }

    private void UpdateScore()
    {
        if (scoreText != null && dataManager != null)
        {
            var data = dataManager.GetGameData();
            scoreText.text = "Score: " + data.playerScore;
        }
    }
}