using System;
using System.Collections.Generic;
using UnityEngine;

namespace LevelGeneration
{
    public static class LevelGenerator
    {
        /// <summary>
        /// 生成一个关卡数据对象。这个类只负责生成数据，游戏界面可以读取生成结果并展示。
        /// </summary>
        public static LevelData Generate(int levelNumber, int width = 26, int height = 26, int seed = -1)
        {
            if (seed < 0)
            {
                seed = Environment.TickCount;
            }

            var random = new System.Random(seed);
            var level = new LevelData(width, height)
            {
                levelNumber = levelNumber,
                levelName = $"Level_{levelNumber}",
                seed = seed,
            };

            FillEmpty(level);
            FillBorder(level);
            PlaceBase(level);
            PlacePlayerSpawn(level);
            PlaceEnemySpawns(level, random, 3);
            PlaceRandomBlocks(level, random);
            PlaceRandomSpecial(level, random);

            return level;
        }

        private static void FillEmpty(LevelData level)
        {
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    level.SetTile(x, y, LevelTileType.Empty);
                }
            }
        }

        private static void FillBorder(LevelData level)
        {
            for (int x = 0; x < level.width; x++)
            {
                level.SetTile(x, 0, LevelTileType.Steel);
                level.SetTile(x, level.height - 1, LevelTileType.Steel);
            }

            for (int y = 0; y < level.height; y++)
            {
                level.SetTile(0, y, LevelTileType.Steel);
                level.SetTile(level.width - 1, y, LevelTileType.Steel);
            }
        }

        private static void PlaceBase(LevelData level)
        {
            int baseX = level.width / 2;
            int baseY = 1;
            level.basePosition = new Vector2Int(baseX, baseY);
            level.SetTile(baseX, baseY, LevelTileType.Base);

            // 为基地周边留出安全区
            SetEmptyBlock(level, baseX - 1, baseY);
            SetEmptyBlock(level, baseX + 1, baseY);
            SetEmptyBlock(level, baseX, baseY + 1);
            SetEmptyBlock(level, baseX - 1, baseY + 1);
            SetEmptyBlock(level, baseX + 1, baseY + 1);
        }

        private static void PlacePlayerSpawn(LevelData level)
        {
            int spawnX = level.width / 2;
            int spawnY = 3;
            level.playerSpawn = new Vector2Int(spawnX, spawnY);
            level.SetTile(spawnX, spawnY, LevelTileType.PlayerSpawn);

            SetEmptyBlock(level, spawnX - 1, spawnY);
            SetEmptyBlock(level, spawnX + 1, spawnY);
            SetEmptyBlock(level, spawnX, spawnY + 1);
        }

        private static void PlaceEnemySpawns(LevelData level, System.Random random, int count)
        {
            var spawns = new List<Vector2Int>();
            int startY = level.height - 3;
            int[] spawnXs = { 2, level.width / 2, level.width - 3 };

            for (int i = 0; i < count; i++)
            {
                int x = spawnXs[i % spawnXs.Length];
                var position = new Vector2Int(x, startY);
                spawns.Add(position);
                level.SetTile(position.x, position.y, LevelTileType.EnemySpawn);
                SetEmptyBlock(level, position.x, position.y - 1);
                SetEmptyBlock(level, position.x - 1, position.y);
                SetEmptyBlock(level, position.x + 1, position.y);
            }

            level.enemySpawns = spawns.ToArray();
        }

        private static void PlaceRandomBlocks(LevelData level, System.Random random)
        {
            int totalCells = level.width * level.height;
            int brickCount = totalCells / 6;
            int steelCount = totalCells / 20;
            int grassCount = totalCells / 12;

            PlaceTiles(level, random, LevelTileType.Brick, brickCount);
            PlaceTiles(level, random, LevelTileType.Steel, steelCount);
            PlaceTiles(level, random, LevelTileType.Grass, grassCount);
        }

        private static void PlaceRandomSpecial(LevelData level, System.Random random)
        {
            int waterCount = (level.width * level.height) / 16;
            PlaceTiles(level, random, LevelTileType.Water, waterCount);
        }

        private static void PlaceTiles(LevelData level, System.Random random, LevelTileType tileType, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int x = random.Next(1, level.width - 1);
                int y = random.Next(1, level.height - 1);

                if (CanPlaceTile(level, x, y))
                {
                    level.SetTile(x, y, tileType);
                }
            }
        }

        private static bool CanPlaceTile(LevelData level, int x, int y)
        {
            if (!level.IsPositionValid(x, y))
            {
                return false;
            }

            var tile = level.GetTile(x, y);
            return tile == LevelTileType.Empty;
        }

        private static void SetEmptyBlock(LevelData level, int x, int y)
        {
            if (!level.IsPositionValid(x, y))
            {
                return;
            }

            level.SetTile(x, y, LevelTileType.Empty);
        }
    }
}
