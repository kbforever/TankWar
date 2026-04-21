using UnityEngine;
using UnityEngine.UI;
using GameFramework;
using TMPro;

public class MainMenuPanel : UIPanel
{
    private Button SinglePlayer;
    private Button TwoPlayer;
    private Button QuitGame;

    private UIFramework uiFramework;
    private LevelManager levelManager;
    private DataManager dataManager;

    protected GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;

    public override void Initialize()
    {
        base.Initialize();
        uiFramework = Framework.GetFeature<UIFramework>();
        levelManager = Framework.GetFeature<LevelManager>();
        dataManager = Framework.GetFeature<DataManager>();

        // 自动获取 Button
        SinglePlayer = transform.Find(nameof(SinglePlayer))?.GetComponent<Button>();
        TwoPlayer = transform.Find(nameof(TwoPlayer))?.GetComponent<Button>();
        QuitGame = transform.Find(nameof(QuitGame))?.GetComponent<Button>();

        if (SinglePlayer != null)
        {
            SinglePlayer.onClick.AddListener(OnSinglePlayer);
        }

        if (TwoPlayer != null)
        {
            TwoPlayer.onClick.AddListener(OnTwoPlayer);
        }

        if (QuitGame != null)
        {
            QuitGame.onClick.AddListener(OnQuitGame);
        }
    }

    public override void OnShow()
    {
        base.OnShow();
        // 显示时的逻辑
    }

    public override void OnHide()
    {
        base.OnHide();
        // 隐藏时的逻辑
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (SinglePlayer != null)
        {
            SinglePlayer.onClick.RemoveListener(OnSinglePlayer);
        }

        if (TwoPlayer != null)
        {
            TwoPlayer.onClick.RemoveListener(OnTwoPlayer);
        }

        if (QuitGame != null)
        {
            QuitGame.onClick.RemoveListener(OnQuitGame);
        }
    }

    private void OnSinglePlayer()
    {
        if (dataManager != null)
        {
            dataManager.SetSavePath(GameMode.SinglePlayer);
        }
        StartGame();
    }

    private void OnTwoPlayer()
    {
        if (dataManager != null)
        {
            
            dataManager.SetSavePath(GameMode.TwoPlayer);
            
        }
        StartGame();
    }

    private void StartGame()
    {
        
        // 开始游戏：切换到 Playing 状态
        var framework = GameFramework.GameFramework.Instance;
        if (framework != null)
        {
            framework.ChangeState(GameState.Playing);
        }

        // 隐藏主菜单（UIFramework 会自动显示 Game 面板）
        if (uiFramework != null)
        {
            uiFramework.HidePanel("MainMenu");
        }
    }

    private void OnQuitGame()
    {
        // 退出游戏
        Debug.Log("退出游戏");
        Application.Quit();
    }
}