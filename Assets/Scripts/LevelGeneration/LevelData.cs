using System;
using UnityEngine;

namespace LevelGeneration
{
    [Serializable]
    public enum LevelTileType
    {
        Empty = 0,
        Brick = 1,
        Steel = 2,
        Grass = 3,
        Water = 4,
        EnemySpawn = 5,
        PlayerSpawn = 6,
        Base = 7
    }

    [Serializable]
    public class LevelData
    {
        public int levelNumber;
        public string levelName;
        public int width;
        public int height;
        public LevelTileType[] tiles;
        public Vector2Int playerSpawn;
        public Vector2Int basePosition;
        public Vector2Int[] enemySpawns;
        public int seed;

        public LevelData() { }

        public LevelData(int width, int height)
        {
            this.width = Mathf.Max(5, width);
            this.height = Mathf.Max(5, height);
            tiles = new LevelTileType[this.width * this.height];
            playerSpawn = Vector2Int.zero;
            basePosition = Vector2Int.zero;
            enemySpawns = Array.Empty<Vector2Int>();
        }

        public bool IsValid => tiles != null && tiles.Length == width * height;

        public LevelTileType GetTile(int x, int y)
        {
            if (!IsPositionValid(x, y)) return LevelTileType.Empty;
            int index = (height - 1 - y) * width + x;
            return tiles[index];
        }

        public void SetTile(int x, int y, LevelTileType tileType)
        {
            if (!IsPositionValid(x, y)) return;
            int index = (height - 1 - y) * width + x;
            tiles[index] = tileType;
        }

        public string ToJson(bool prettyPrint = false)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        public static LevelData FromJson(string json)
        {
            return JsonUtility.FromJson<LevelData>(json);
        }

        public bool IsPositionValid(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
    }
}
