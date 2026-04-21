using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GameFramework;

public class GameEntry : MonoBehaviour
{
    private GameFramework.GameFramework gameFramework;
    private UIFramework uiFramework;
    private LevelManager levelManager;
    private DataManager dataManager;
    private InputManager inputManager;

    private void Awake()
    {
        // 创建 GameFramework
        GameObject frameworkObj = new GameObject("GameFramework");
        gameFramework = frameworkObj.AddComponent<GameFramework.GameFramework>();
        DontDestroyOnLoad(frameworkObj);

        // 创建 UIFramework
        GameObject uiObj = new GameObject("UIFramework");
        uiFramework = uiObj.AddComponent<UIFramework>();
        DontDestroyOnLoad(uiObj);

   

        // 创建 EventSystem（如果不存在）
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventObj = new GameObject("EventSystem");
            eventObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        //创建 LevelManager
        GameObject levelObj = new GameObject("LevelManager");
        levelManager = levelObj.AddComponent<LevelManager>();
        DontDestroyOnLoad(levelObj);

        // 创建 DataManager
        GameObject dataObj = new GameObject("DataManager");
        dataManager = dataObj.AddComponent<DataManager>();
        DontDestroyOnLoad(dataObj);

        // 创建 InputManager
        GameObject inputObj = new GameObject("InputManager");
        inputManager = inputObj.AddComponent<InputManager>();
        DontDestroyOnLoad(inputObj);

        // 注册所有 Feature
        
        gameFramework.RegisterFeature(levelManager);
        gameFramework.RegisterFeature(dataManager);
        gameFramework.RegisterFeature(inputManager);
        gameFramework.RegisterFeature(uiFramework);
    }

    private void Start()
    {
        // 启动游戏，进入主菜单
        if (gameFramework != null)
        {
            gameFramework.ChangeState(GameState.MainMenu);
        }
    }
}