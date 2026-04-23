using UnityEngine;
using UnityEngine.UI;
using GameFramework;
using TMPro;
using UnityEngine.EventSystems;
using System.Transactions;

public class MainMenuPanel : UIPanel
{
    // private Button SinglePlayer;
    // private Button TwoPlayer;
    private Button QuitGame;


    private GameObject Title;
    private Toggle SinglePlayer;
    private Toggle TwoPlayer;
    private UIFramework uiFramework;
    private LevelManager levelManager;
    private DataManager dataManager;

    private InputManager inputManager;
    protected GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;

    public override void Initialize()
    {
        base.Initialize();
        uiFramework = Framework.GetFeature<UIFramework>();
        levelManager = Framework.GetFeature<LevelManager>();
        dataManager = Framework.GetFeature<DataManager>();
        inputManager =Framework.GetFeature<InputManager>();
        // 自动获取 Button
        // SinglePlayer = transform.Find(nameof(SinglePlayer))?.GetComponent<Button>();
        // TwoPlayer = transform.Find(nameof(TwoPlayer))?.GetComponent<Button>();
        QuitGame = transform.Find(nameof(QuitGame))?.GetComponent<Button>();
        // if (SinglePlayer != null)
        // {
        //     SinglePlayer.onClick.AddListener(OnSinglePlayer);
        // }

        // if (TwoPlayer != null)
        // {
        //     TwoPlayer.onClick.AddListener(OnTwoPlayer);
        // }


        Title = transform.Find(nameof(Title)).gameObject;
        if (Title != null)
        {
            SinglePlayer = Title.transform.Find(nameof(SinglePlayer))?.GetComponent<Toggle>();
            TwoPlayer = Title.transform.Find(nameof(TwoPlayer))?.GetComponent<Toggle>();
        }
        
        SinglePlayer.isOn=true;

       
    

        if (QuitGame != null)
        {
            QuitGame.onClick.AddListener(OnQuitGame);
        }
    }

    void Update()
    {
        if (Framework.CurrentState == GameState.MainMenu)
        {
            if (inputManager.GetButtonDown("MoveUp") || inputManager.GetButtonDown("MoveDown"))
            {
                if(SinglePlayer.isOn) {TwoPlayer.isOn=true;return;}
                if(TwoPlayer.isOn) {SinglePlayer.isOn = true;return;} 
            }
            if (inputManager.GetButtonDown("Attack"))
            {
                
                if(SinglePlayer.isOn) OnSinglePlayer();
                else if(TwoPlayer.isOn) OnTwoPlayer();
            }
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
        // if (SinglePlayer != null)
        // {
        //     SinglePlayer.onClick.RemoveListener(OnSinglePlayer);
        // }

        // if (TwoPlayer != null)
        // {
        //     TwoPlayer.onClick.RemoveListener(OnTwoPlayer);
        // }

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