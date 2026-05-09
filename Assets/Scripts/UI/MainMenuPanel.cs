using GameFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuPanel : UIPanel
{
    private Button startGameButton;
    private Button quitGameButton;

    private UIFramework uiFramework;
    private DataManager dataManager;
    private InputManager inputManager;

    protected GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;

    public override void Initialize()
    {
        base.Initialize();

        uiFramework = Framework.GetFeature<UIFramework>();
        dataManager = Framework.GetFeature<DataManager>();
        inputManager = Framework.GetFeature<InputManager>();

        startGameButton = FindButton("StartGame");
        quitGameButton = FindButton("QuitGame");

        if (startGameButton != null)
        {
         
            startGameButton.onClick.AddListener(OnStartGame);
        }

        if (quitGameButton != null)
        {
           
            quitGameButton.onClick.AddListener(OnQuitGame);
        }
    }

    public override void OnShow()
    {
        base.OnShow();
        inputManager?.DisableMaps();

        if (startGameButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(startGameButton.gameObject);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(OnStartGame);
        }

        if (quitGameButton != null)
        {
            quitGameButton.onClick.RemoveListener(OnQuitGame);
        }
    }

    private Button FindButton(string buttonName)
    {
        return transform.Find(buttonName)?.GetComponent<Button>();
    }

    private void OnStartGame()
    {
        dataManager?.SetSavePath(GameMode.SinglePlayer);
        inputManager?.EnableMaps(GameMode.SinglePlayer);

        if (Framework != null)
        {
            Framework.ChangeState(GameState.Playing);
        }

        uiFramework?.HidePanel(PanelName);
    }

    private void OnQuitGame()
    {
        Debug.Log("Quit game");
        Application.Quit();
    }
}
