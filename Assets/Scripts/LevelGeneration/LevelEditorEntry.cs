using UnityEngine;
using GameFramework;

public class LevelEditorEntry : MonoBehaviour
{
    private GameFramework.GameFramework gameFramework;
    private LevelManager levelManager;
    private LevelEditor levelEditor;

    private void Awake()
    {
        // 创建 GameFramework
        GameObject frameworkObj = new GameObject("GameFramework");
        gameFramework = frameworkObj.AddComponent<GameFramework.GameFramework>();
        DontDestroyOnLoad(frameworkObj);

        // 创建 LevelManager
        GameObject levelObj = new GameObject("LevelManager");
        levelManager = levelObj.AddComponent<LevelManager>();
        DontDestroyOnLoad(levelObj);

        // 创建 LevelEditor
        GameObject editorObj = new GameObject("LevelEditor");
        levelEditor = editorObj.AddComponent<LevelEditor>();
        DontDestroyOnLoad(editorObj);

        gameFramework.RegisterFeature(levelManager);
        gameFramework.RegisterFeature(levelEditor);
    }

    private void Start()
    {
        if (gameFramework != null)
        {
            gameFramework.ChangeState(GameState.Boot);
        }
    }
}
