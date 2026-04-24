using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using UnityEngine.InputSystem;


[DisallowMultipleComponent]
public partial class InputManager : MonoBehaviour, IGameFeature
{
    [Header("输入配置")]
    [SerializeField] private InputConfig defaultConfig;
    private Dictionary<string, KeyCode> keyMappings = new Dictionary<string, KeyCode>();
    private Dictionary<string, bool> axisStates = new Dictionary<string, bool>();

    Vector2 inputdir;
    public bool IsActive { get; private set; }
    protected GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;

    public void Initialize()
    {
        // new system
        
        inputActions = new PlayerContorller();
        AcitonByName["Player1"] = inputActions.Player1;
        AcitonByName["Player2"] = inputActions.Player2;
        
        isPaused = false;
        //

        // inputActions.Player1.Pause.started+=ctx=>isPaused = !isPaused;


        IsActive = true;
 
        

        if (defaultConfig != null)
        {
            LoadConfig(defaultConfig);
        }
        else
        {
            // 默认映射
            keyMappings["MoveUp"] = KeyCode.W;
            keyMappings["MoveDown"] = KeyCode.S;
            keyMappings["MoveLeft"] = KeyCode.A;
            keyMappings["MoveRight"] = KeyCode.D;
            keyMappings["Jump"] = KeyCode.Space;
            keyMappings["Attack"] = KeyCode.J;
            keyMappings["Pause"] = KeyCode.P;


            // 第二个玩家映射
            keyMappings["P2MoveUp"] = KeyCode.UpArrow;
            keyMappings["P2MoveDown"] = KeyCode.DownArrow;
            keyMappings["P2MoveLeft"] = KeyCode.LeftArrow;
            keyMappings["P2MoveRight"] = KeyCode.RightArrow;
            keyMappings["P2Jump"] = KeyCode.Keypad0;
            keyMappings["P2Attack"] = KeyCode.Keypad2;
        }
    }


    public void FeatureUpdate()
    {
        // // 更新轴状态
        // foreach (var key in keyMappings.Keys)
        // {
        //     axisStates[key] = Input.GetKey(keyMappings[key]);
        // }
        
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                Framework.ChangeState(GameState.Paused);
            }
            else
            {
                Framework.ChangeState(GameState.Playing);
            }
            isPaused = !isPaused;

        }
    }



    public void EnableMaps(GameMode gameMode)
    {
        if(gameMode == GameMode.SinglePlayer)
        {
            inputActions.Player1.Enable();
            inputActions.Player2.Disable();
        }
        else if(gameMode == GameMode.TwoPlayer)
        {
            inputActions.Player1.Enable();
            inputActions.Player2.Enable();
        }
        
    }

    public void EnableMaps()
    {
        foreach(var actions in AcitonByName.Values)
        {
            actions .Enable();
        }
    }

    public void DisableMaps()
    {
        foreach(var actions in AcitonByName.Values)
        {
            actions .Disable();
        }
    }

    public void FeatureFixedUpdate() { }
    public void FeatureLateUpdate() { }

    public void OnGameStateChanged(GameState previousState, GameState nextState) { }

    public void Shutdown()
    {
        IsActive = false;
        inputActions.Player1.Disable();
        inputActions.Player2.Disable();
    }

    public void LoadConfig(InputConfig config)
    {
        keyMappings.Clear();
        foreach (var mapping in config.mappings)
        {
            keyMappings[mapping.actionName] = mapping.key;
        }
    }

    public bool GetButton(string actionName)
    {
        return keyMappings.TryGetValue(actionName, out var key) && Input.GetKey(key);
    }

    public bool GetButtonDown(string actionName)
    {
        return keyMappings.TryGetValue(actionName, out var key) && Input.GetKeyDown(key);
    }

    public bool GetButtonUp(string actionName)
    {
        return keyMappings.TryGetValue(actionName, out var key) && Input.GetKeyUp(key);
    }

    public float GetAxis(string positiveAction, string negativeAction)
    {
        float positive = GetButton(positiveAction) ? 1f : 0f;
        float negative = GetButton(negativeAction) ? -1f : 0f;
        return positive + negative;
    }

    public Vector2 GetVector2(string up, string down, string left, string right)
    {
        return new Vector2(GetAxis(right, left), GetAxis(up, down));
    }

    public void RemapKey(string actionName, KeyCode newKey)
    {
        if (keyMappings.ContainsKey(actionName))
        {
            keyMappings[actionName] = newKey;
        }
    }

    public KeyCode GetMappedKey(string actionName)
    {
        return keyMappings.TryGetValue(actionName, out var key) ? key : KeyCode.None;
    }
}

[CreateAssetMenu(fileName = "InputConfig", menuName = "Game/Input Config")]
public class InputConfig : ScriptableObject
{
    public List<KeyMapping> mappings = new List<KeyMapping>();
}

[Serializable]
public class KeyMapping
{
    public string actionName;
    public KeyCode key;
}