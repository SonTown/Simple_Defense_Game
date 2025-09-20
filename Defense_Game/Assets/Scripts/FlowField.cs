using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public class FlowField : MonoBehaviour
{
    public static FlowField Instance;

    public static int gridSizeX = 25;
    public static int gridSizeY = 50;
    public static float cellSize = 2f;

    public Transform target; // 목표 위치 (예: 성)

    public NavMeshSurface surface;
    public GameObject obstaclePrefab;
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
        LoadObstacles();
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
    public void LoadObstacles()
    {
        string path = Path.Combine(Application.persistentDataPath, "obstacles.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning("No obstacle save file found.");
            return;
        }

        string json = File.ReadAllText(path);
        ObstacleDataList wrapper = JsonUtility.FromJson<ObstacleDataList>(json);

        // 기본 빈 상태 초기화
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                grid[x, y] = CellType.Empty;
            }
        }

        foreach (var data in wrapper.items)
        {
            for (int dx = -(data.size.x/2); dx < (data.size.x+1)/2; dx++)
            {
                for (int dy = -(data.size.y/2); dy < (data.size.y+1)/2; dy++)
                {
                    int gx = data.position.x + dx;
                    int gy = data.position.y + dy;
                    if (gx >= 0 && gx < gridSizeX && gy >= 0 && gy < gridSizeY)
                    {
                        grid[gx, gy] = data.cellType;
                    }
                }
            }
            Debug.Log(data.position);
            GameObject obj = Instantiate(obstaclePrefab,new Vector3( data.position.x*cellSize+cellSize/2,-2,data.position.y*cellSize+cellSize/2), Quaternion.identity);
        }
        surface.BuildNavMesh();
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
        SyncToECS();
    }
    public void SyncToECS()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        // FlowFieldComponent 달린 엔티티 찾기
        var query = em.CreateEntityQuery(typeof(FlowFieldComponent));
        var entities = query.ToEntityArray(Allocator.Temp);

        if (entities.Length == 0)
        {
            Debug.LogError("FlowFieldComponent를 가진 엔티티가 없습니다!");
            return;
        }

        var flowFieldEntity = entities[0]; // 하나만 있다고 가정

        var nativeArray = new NativeParallelHashMap<int,float3>(gridSizeX * gridSizeY, Allocator.Persistent);

        // 2차원 배열 → 1차원 NativeArray 복사
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                nativeArray[x + y * gridSizeX] = flowVectors[x, y];
            }
        }

        em.SetComponentData(flowFieldEntity, new FlowFieldComponent
        {
            FlowVectors = nativeArray
        });

        entities.Dispose();
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
