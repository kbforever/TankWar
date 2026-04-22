using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using LevelGeneration;

public class LevelEditor : MonoBehaviour, IGameFeature
{
    [Header("Level Editor")]
    [SerializeField] private bool visible = false;
    [SerializeField] private int defaultWidth = 16;
    [SerializeField] private int defaultHeight = 12;
    [SerializeField] private string defaultLevelName = "custom_level";

    private LevelManager levelManager;
    private LevelData editorLevel;
    private Vector2 scrollPosition;
    private LevelTileType selectedTile = LevelTileType.Brick;
    private bool showGrid = true;
    private string levelNameField;
    private string widthFieldText;
    private string heightFieldText;
    private int widthField;
    private int heightField;
    private bool isDirty;

    public bool IsActive { get; private set; }
    protected GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;

    public void Initialize()
    {
        IsActive = true;
        levelManager = Framework.GetFeature<LevelManager>();
        widthField = defaultWidth;
        heightField = defaultHeight;
        widthFieldText = defaultWidth.ToString();
        heightFieldText = defaultHeight.ToString();
        levelNameField = defaultLevelName;
        CreateNewLevel();
    }

    public void FeatureUpdate()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            visible = !visible;
        }
    }

    public void FeatureFixedUpdate() { }
    public void FeatureLateUpdate() { }
    public void OnGameStateChanged(GameState previousState, GameState nextState) { }

    public void Shutdown()
    {
        IsActive = false;
    }

    private void OnGUI()
    {
        if (!visible) return;

        // 计算缩放因子，使界面占满屏幕
        float scaleX = Screen.width / 1080f;
        float scaleY = Screen.height / 1920f;
        float scale = Mathf.Min(scaleX, scaleY);

        // 保存原始矩阵
        Matrix4x4 originalMatrix = GUI.matrix;

        // 应用缩放
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));

        // 调整窗口大小以占满屏幕（在缩放后）
        float windowWidth = Screen.width / scale;
        float windowHeight = Screen.height / scale;

        GUI.Box(new Rect(0, 0, windowWidth, windowHeight), "手工关卡编辑器");
        GUILayout.BeginArea(new Rect(20, 40, windowWidth - 40, windowHeight - 80));

        // 调整字体大小
        float fontSize =  24f;
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = Mathf.RoundToInt(fontSize * scale);
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = Mathf.RoundToInt(fontSize * scale);
        GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField);
        textFieldStyle.fontSize = Mathf.RoundToInt(fontSize * scale);
        GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
        toggleStyle.fontSize = Mathf.RoundToInt(fontSize * scale);


        float alllabelHeight = 100 * scale;

        GUILayout.BeginHorizontal();
        GUILayout.Label("关卡名称", labelStyle);
        levelNameField = GUILayout.TextField(levelNameField, 64, textFieldStyle, GUILayout.Height(alllabelHeight));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("地图宽度", labelStyle);
        widthFieldText = GUILayout.TextField(widthFieldText, textFieldStyle, GUILayout.Height(alllabelHeight));
        if (int.TryParse(widthFieldText, out var parsedWidth))
        {
            widthField = Mathf.Clamp(parsedWidth, 5, 64);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("地图高度", labelStyle);
        heightFieldText = GUILayout.TextField(heightFieldText, textFieldStyle, GUILayout.Height(alllabelHeight));
        if (int.TryParse(heightFieldText, out var parsedHeight))
        {
            heightField = Mathf.Clamp(parsedHeight, 5, 64);
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("新建关卡", buttonStyle, GUILayout.Height(30 * scale)))
        {
            CreateNewLevel();
        }

        if (GUILayout.Button("读取关卡", buttonStyle, GUILayout.Height(30 * scale)))
        {
            LoadLevel();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("保存关卡", buttonStyle, GUILayout.Height(30 * scale)))
        {
            SaveLevel();
        }

        if (GUILayout.Button("从资源读取", buttonStyle, GUILayout.Height(30 * scale)))
        {
            LoadLevelFromResources();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("瓦片类型选择：", labelStyle);
        DrawTilePalette(buttonStyle, 24 * scale);

        GUILayout.Space(10);
        showGrid = GUILayout.Toggle(showGrid, "显示网格", toggleStyle);

        GUILayout.Space(10);
        GUILayout.Label($"当前关卡: {editorLevel?.levelName ?? "<空>"}", labelStyle);
        GUILayout.Label($"尺寸: {editorLevel?.width} x {editorLevel?.height}", labelStyle);
        GUILayout.Label($"已修改: {isDirty}", labelStyle);

        if (editorLevel != null)
        {
            DrawGridEditor(windowWidth - 40, windowHeight-10* scale); // 调整网格区域高度
        }

        GUILayout.EndArea();

        // 恢复原始矩阵
        GUI.matrix = originalMatrix;
    }


    private void CreateNewLevel()
    {
        editorLevel = new LevelData(widthField, heightField)
        {
            levelName = string.IsNullOrEmpty(levelNameField) ? defaultLevelName : levelNameField,
            tiles = new LevelTileType[widthField * heightField]
        };
        for (int y = 0; y < heightField; y++)
        {
            for (int x = 0; x < widthField; x++)
            {
                editorLevel.SetTile(x, y, LevelTileType.Empty);
            }
        }
        editorLevel.playerSpawn = new Vector2Int(0, 0);
        isDirty = false;
    }

    private void LoadLevel()
    {
        if (levelManager == null || string.IsNullOrEmpty(levelNameField)) return;
        var loaded = levelManager.LoadLevelFromFile(levelNameField);
        if (loaded != null)
        {
            editorLevel = loaded;
            widthField = editorLevel.width;
            heightField = editorLevel.height;
            isDirty = false;
        }
    }

    private void LoadLevelFromResources()
    {
        if (levelManager == null || string.IsNullOrEmpty(levelNameField)) return;
        string resourcePath = $"Levels/{levelNameField}";
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset != null)
        {
            var loaded = LevelData.FromJson(textAsset.text);
            if (loaded != null && loaded.IsValid)
            {
                editorLevel = loaded;
                widthField = editorLevel.width;
                heightField = editorLevel.height;
                isDirty = false;
                Debug.Log($"资源关卡已加载: {resourcePath}");
            }
        }
    }

    private void SaveLevel()
    {
        if (levelManager == null || editorLevel == null || string.IsNullOrEmpty(levelNameField)) return;


        editorLevel.levelName = levelNameField;
        editorLevel.player1Health = 3; // 可以根据需要设置默认生命值
        editorLevel.player2Health = 3;  
        editorLevel.maxEnemyCount = 10; // 可以根据需要设置默认敌人数量
        if (levelManager.SaveLevelToFile(editorLevel, levelNameField))
        {
            isDirty = false;
        }
    }

    private void DrawTilePalette(GUIStyle buttonStyle, float buttonHeight)
    {
        GUILayout.BeginHorizontal();
        foreach (LevelTileType tile in Enum.GetValues(typeof(LevelTileType)))
        {
            if (GUILayout.Button(tile.ToString(), buttonStyle, GUILayout.Height(buttonHeight)))
            {
                selectedTile = tile;
            }
        }
        GUILayout.EndHorizontal();
    }

    private void DrawGridEditor(float areaWidth, float areaHeight)
    {
        // int cellSize = Mathf.RoundToInt(50 * (Screen.width / 1080f)); // 按比例调整单元格大小
        int cellSize = Mathf.RoundToInt(Mathf.Min((areaWidth-20)/editorLevel.width, (areaHeight-20)/editorLevel.height)); // 按比例调整单元格大小
    
        float gridWidth = editorLevel.width * cellSize;
        float gridHeight = editorLevel.height * cellSize;
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(areaWidth), GUILayout.Height(areaHeight)); // 调整滚动视图高度
        Rect gridRect = GUILayoutUtility.GetRect(gridWidth, gridHeight);

        for (int y = 0; y < editorLevel.height; y++)
        {
            for (int x = 0; x < editorLevel.width; x++)
            {
                Rect cellRect = new Rect(10+gridRect.x + x * cellSize, 10+gridRect.y + y * cellSize, cellSize, cellSize);
                Color cellColor = GetColorForTile(editorLevel.GetTile(x, editorLevel.height - 1 - y));
                EditorDrawCell(cellRect, cellColor, x, y);
            }
        }

        GUILayout.EndScrollView();
    }

    private void EditorDrawCell(Rect rect, Color color, int x, int y)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (showGrid)
        {
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width - 1, rect.y, 1, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            editorLevel.SetTile(x, editorLevel.height - 1 - y, selectedTile);
            if (selectedTile == LevelTileType.PlayerSpawn)
            {
                editorLevel.playerSpawn = new Vector2Int(x, editorLevel.height - 1 - y);
            }
            isDirty = true;
        }
    }

    private Color GetColorForTile(LevelTileType tileType)
    {
        return tileType switch
        {
            LevelTileType.Empty => new Color(0f, 0f, 0f, 0.2f),
            LevelTileType.Brick => new Color(0.7f, 0.3f, 0.1f),
            LevelTileType.Steel => new Color(0.5f, 0.5f, 0.5f),
            LevelTileType.Grass => new Color(0.1f, 0.7f, 0.1f),
            LevelTileType.Water => new Color(0.1f, 0.4f, 0.8f),
            LevelTileType.EnemySpawn => new Color(0.8f, 0.1f, 0.1f),
            LevelTileType.PlayerSpawn => new Color(0.1f, 0.8f, 0.1f),
            LevelTileType.Base => new Color(0.8f, 0.8f, 0.1f),
            _ => Color.clear,
        };
    }
}
