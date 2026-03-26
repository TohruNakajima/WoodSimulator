using System.Collections.Generic;
using UnityEngine;

namespace WoodSimulator
{
    /// <summary>
    /// Poisson Disk Sampling（Bridsonアルゴリズム、O(n)）
    /// 最小距離を保ちながら均等に点を配置
    /// </summary>
    public static class PoissonDiskSampling
    {
        /// <summary>
        /// Poisson Disk Samplingで点を生成
        /// </summary>
        /// <param name="center">中心座標</param>
        /// <param name="size">領域サイズ</param>
        /// <param name="minDistance">最小距離</param>
        /// <param name="maxAttempts">各点からの試行回数</param>
        /// <returns>生成された座標リスト</returns>
        public static List<Vector3> GeneratePoints(Vector3 center, Vector2 size, float minDistance, int maxAttempts = 30)
        {
            List<Vector3> points = new List<Vector3>();
            List<Vector3> activeList = new List<Vector3>();

            // グリッドベース空間分割
            float cellSize = minDistance / Mathf.Sqrt(2);
            int gridWidth = Mathf.CeilToInt(size.x / cellSize);
            int gridHeight = Mathf.CeilToInt(size.y / cellSize);
            Vector3[,] grid = new Vector3[gridWidth, gridHeight];

            // 初期点追加
            Vector3 firstPoint = center + new Vector3(
                Random.Range(-size.x / 2, size.x / 2),
                0,
                Random.Range(-size.y / 2, size.y / 2)
            );
            points.Add(firstPoint);
            activeList.Add(firstPoint);
            AddToGrid(grid, firstPoint, center, size, cellSize);

            // アクティブリストが空になるまで処理
            while (activeList.Count > 0)
            {
                int randomIndex = Random.Range(0, activeList.Count);
                Vector3 currentPoint = activeList[randomIndex];
                bool foundValidPoint = false;

                // 現在の点から新しい点を試行
                for (int i = 0; i < maxAttempts; i++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2);
                    float radius = Random.Range(minDistance, minDistance * 2);
                    Vector3 candidate = currentPoint + new Vector3(
                        Mathf.Cos(angle) * radius,
                        0,
                        Mathf.Sin(angle) * radius
                    );

                    // 領域内チェック
                    if (!IsInBounds(candidate, center, size))
                        continue;

                    // 他の点との距離チェック（グリッドベース）
                    if (IsValidPoint(grid, candidate, center, size, cellSize, minDistance))
                    {
                        points.Add(candidate);
                        activeList.Add(candidate);
                        AddToGrid(grid, candidate, center, size, cellSize);
                        foundValidPoint = true;
                        break;
                    }
                }

                // 有効な点が見つからなかったらアクティブリストから削除
                if (!foundValidPoint)
                {
                    activeList.RemoveAt(randomIndex);
                }
            }

            return points;
        }

        /// <summary>
        /// 指定本数になるまで生成（最小距離を動的調整）
        /// </summary>
        public static List<Vector3> GeneratePointsWithCount(Vector3 center, Vector2 size, int targetCount, int maxIterations = 10)
        {
            // 理論値から初期最小距離を推定
            float areaPerTree = (size.x * size.y) / targetCount;
            float minDistance = Mathf.Sqrt(areaPerTree) * 0.9f; // 90%で余裕を持たせる

            List<Vector3> points = new List<Vector3>();
            int iteration = 0;

            while (iteration < maxIterations)
            {
                points = GeneratePoints(center, size, minDistance);

                if (Mathf.Abs(points.Count - targetCount) <= targetCount * 0.05f) // 5%誤差許容
                {
                    break;
                }

                // 調整
                if (points.Count < targetCount)
                {
                    minDistance *= 0.95f; // 最小距離を減らす
                }
                else
                {
                    minDistance *= 1.05f; // 最小距離を増やす
                }

                iteration++;
            }

            Debug.Log($"PoissonDiskSampling: Generated {points.Count} points (target: {targetCount}, iterations: {iteration})");
            return points;
        }

        /// <summary>
        /// 領域内判定
        /// </summary>
        private static bool IsInBounds(Vector3 point, Vector3 center, Vector2 size)
        {
            Vector3 offset = point - center;
            return Mathf.Abs(offset.x) <= size.x / 2 && Mathf.Abs(offset.z) <= size.y / 2;
        }

        /// <summary>
        /// グリッドに追加
        /// </summary>
        private static void AddToGrid(Vector3[,] grid, Vector3 point, Vector3 center, Vector2 size, float cellSize)
        {
            Vector2Int gridPos = GetGridPosition(point, center, size, cellSize);
            if (gridPos.x >= 0 && gridPos.x < grid.GetLength(0) && gridPos.y >= 0 && gridPos.y < grid.GetLength(1))
            {
                grid[gridPos.x, gridPos.y] = point;
            }
        }

        /// <summary>
        /// グリッド座標取得
        /// </summary>
        private static Vector2Int GetGridPosition(Vector3 point, Vector3 center, Vector2 size, float cellSize)
        {
            Vector3 offset = point - center + new Vector3(size.x / 2, 0, size.y / 2);
            return new Vector2Int(
                Mathf.FloorToInt(offset.x / cellSize),
                Mathf.FloorToInt(offset.z / cellSize)
            );
        }

        /// <summary>
        /// 有効な点かチェック（周囲グリッドとの距離確認）
        /// </summary>
        private static bool IsValidPoint(Vector3[,] grid, Vector3 point, Vector3 center, Vector2 size, float cellSize, float minDistance)
        {
            Vector2Int gridPos = GetGridPosition(point, center, size, cellSize);
            int searchRadius = 2;

            for (int x = gridPos.x - searchRadius; x <= gridPos.x + searchRadius; x++)
            {
                for (int y = gridPos.y - searchRadius; y <= gridPos.y + searchRadius; y++)
                {
                    if (x < 0 || x >= grid.GetLength(0) || y < 0 || y >= grid.GetLength(1))
                        continue;

                    Vector3 neighbor = grid[x, y];
                    if (neighbor != Vector3.zero)
                    {
                        float distance = Vector3.Distance(point, neighbor);
                        if (distance < minDistance)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
