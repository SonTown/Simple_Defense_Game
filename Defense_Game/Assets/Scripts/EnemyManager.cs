using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    private int gridSizeX = FlowField.gridSizeX;
    private int gridSizeZ = FlowField.gridSizeY;
    private float cellSize = FlowField.cellSize;
    
    private List<Enemy>[,] grid;

    void Awake()
    {
        Instance = this;
        grid = new List<Enemy>[gridSizeX, gridSizeZ];
        for (int x = 0; x < gridSizeX; x++)
            for (int z = 0; z < gridSizeZ; z++)
                grid[x, z] = new List<Enemy>();
    }

    // 적 등록
    public void RegisterEnemy(Enemy enemy)
    {
        Vector2Int cell = GetCell(enemy.transform.position);
        grid[cell.x, cell.y].Add(enemy);
        enemy.currentCell = cell;
    }

    // 적 위치 업데이트
    public void UpdateEnemyCell(Enemy enemy)
    {
        Vector2Int newCell = GetCell(enemy.transform.position);
        if (newCell != enemy.currentCell)
        {
            grid[enemy.currentCell.x, enemy.currentCell.y].Remove(enemy);
            grid[newCell.x, newCell.y].Add(enemy);
            enemy.currentCell = newCell;
        }
    }

    // 범위 내 최적 적 검색 (진행률 최대)
    public Enemy GetBestTarget(Vector3 position, float range)
    {
        List<Enemy> candidates = new List<Enemy>();
        Vector2Int centerCell = GetCell(position);
        int cellRange = Mathf.CeilToInt(range / cellSize);
        bool isFound = false;
        for (int z = centerCell.y - cellRange; z <= centerCell.y + cellRange; z++)
        {
            for (int x = centerCell.x - cellRange; x <= centerCell.x + cellRange; x++)
            {
                if (x < 0 || z < 0 || x >= gridSizeX || z >= gridSizeZ) continue;

                foreach (var e in grid[x, z])
                {
                    if (!e.isAlive) continue;
                    if ((e.transform.position - position).sqrMagnitude <= range * range)
                    {
                        candidates.Add(e);
                        isFound = true;
                    }
                }
            }
            if(isFound) break;
        }
        if (candidates.Count == 0) return null;

        // 진행률 기준 정렬
        candidates.Sort((a, b) => b.progress.CompareTo(a.progress));
        return candidates[0];
    }
    // 폭발 범위 내 적 검색
    public List<Enemy> GetEnemiesInRange(Vector3 position, float range)
    {
        List<Enemy> result = new List<Enemy>();
        Vector2Int centerCell = GetCell(position);
        int cellRange = Mathf.CeilToInt(range / cellSize);

        for (int z = centerCell.y - cellRange; z <= centerCell.y + cellRange; z++)
        {
            for (int x = centerCell.x - cellRange; x <= centerCell.x + cellRange; x++)
            {
                if (x < 0 || z < 0 || x >= gridSizeX || z >= gridSizeZ) continue;

                foreach (var e in grid[x, z])
                {
                    if (!e.isAlive) continue;
                    if ((e.transform.position - position).sqrMagnitude <= range * range)
                    {
                        result.Add(e);
                    }
                }
            }
        }
        return result;
    }

    Vector2Int GetCell(Vector3 pos)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt(pos.x / cellSize), 0, gridSizeX - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(pos.z / cellSize), 0, gridSizeZ - 1);
        return new Vector2Int(x, z);
    }
}
