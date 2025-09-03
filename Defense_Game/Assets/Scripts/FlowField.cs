using UnityEngine;
using System.Collections.Generic;

public class FlowField : MonoBehaviour
{
    public static FlowField Instance;

    public static int gridSizeX = 50;
    public static int gridSizeY = 100;
    public static float cellSize = 1f;

    public Transform target; // 목표 위치 (예: 성)

    // 셀 타입 정의
    public enum CellType { Empty, Obstacle, Wall, Water }
    private CellType[,] grid;

    // Flow field 데이터
    private Vector3[,] flowVectors;   // 각 셀이 향해야 하는 방향
    private float[,] costMap;         // 목표까지 거리 비용
    private Vector2Int[] dirs = new[] {
        new Vector2Int(1,0), new Vector2Int(-1,0),
        new Vector2Int(0,1), new Vector2Int(0,-1)
    };

    private Vector2Int[] dirs8 = new[]
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(-1, 1),
        new Vector2Int(-1, -1), new Vector2Int(1, -1)
    };
    private void Awake()
    {
        Instance = this;
        grid = new CellType[gridSizeX, gridSizeY];
        flowVectors = new Vector3[gridSizeX, gridSizeY];
        costMap = new float[gridSizeX, gridSizeY];
    }

    void Start()
    {
        GenerateFlowField();
    }

    // --------------------------
    // 셀 관리
    // --------------------------

    public void SetCell(int x, int y, CellType type)
    {
        if (InBounds(x, y))
        {
            grid[x, y] = type;
            GenerateFlowField(); // 필요 시 전체 필드 재계산
        }
    }

    public CellType GetCell(int x, int y)
    {
        if (InBounds(x, y)) return grid[x, y];
        return CellType.Wall; // 범위 밖은 벽 취급
    }

    // --------------------------
    // FlowField 생성
    // --------------------------

    public void GenerateFlowField()
    {
        // 1. 비용 초기화
        for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
                costMap[x, y] = float.MaxValue;
        for (int x = 0; x < 20; x++)
        {
            grid[x, 50] = CellType.Obstacle;
            grid[x+30, 50] = CellType.Obstacle;
            grid[x, 51] = CellType.Obstacle;
            grid[x+30, 51] = CellType.Obstacle;
            grid[x, 49] = CellType.Obstacle;
            grid[x+30, 49] = CellType.Obstacle;
        }

        // 2. BFS로 목표까지 거리 계산
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        int targetX = Mathf.Clamp(Mathf.FloorToInt(target.position.x / cellSize), 0, gridSizeX - 1);
        int targetY = Mathf.Clamp(Mathf.FloorToInt(target.position.z / cellSize), 0, gridSizeY - 1);
        
        for (int x = 0; x < gridSizeX; x++)
        {
            costMap[x, targetY] = 0;
            queue.Enqueue(new Vector2Int(x, targetY));
        }

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            float currentCost = costMap[current.x, current.y];

            foreach (var dir in dirs)
            {
                int nx = current.x + dir.x;
                int ny = current.y + dir.y;

                if (!InBounds(nx, ny)) continue;

                // 장애물 셀은 통과 불가
                if (grid[nx, ny] == CellType.Obstacle || grid[nx, ny] == CellType.Wall)
                    continue;

                if (costMap[nx, ny] > currentCost + 1)
                {
                    costMap[nx, ny] = currentCost + 1;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        // 3. 각 셀의 Flow 방향 결정
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                if (grid[x, y] == CellType.Obstacle || grid[x, y] == CellType.Wall)
                {
                    flowVectors[x, y] = Vector3.zero;
                    continue;
                }

                Vector3 bestDir = Vector3.zero;
                float minCost = costMap[x, y];

                foreach (var dir in dirs8)
                {
                    int nx = x + dir.x;
                    int ny = y + dir.y;
                    if (!InBounds(nx, ny)) continue;

                    if (costMap[nx, ny] < minCost)
                    {
                        minCost = costMap[nx, ny];
                        bestDir = new Vector3(dir.x, 0, dir.y);
                    }
                }

                flowVectors[x, y] = bestDir.normalized;
            }
        }
    }

    // --------------------------
    // 유닛이 방향을 얻는 함수
    // --------------------------

    public Vector3 GetFlowDirection(Vector3 position)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt(position.x / cellSize), 0, gridSizeX - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(position.z / cellSize), 0, gridSizeY - 1);
        return flowVectors[x, y];
    }

    // --------------------------
    // 유틸
    // --------------------------

    private bool InBounds(int x, int y)
    {
        return x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY;
    }
}
