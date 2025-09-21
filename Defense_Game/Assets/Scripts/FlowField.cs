using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Priority_Queue;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.AI.Navigation; // SimplePriorityQueue를 위해 필요 (https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp)

public class FlowField : MonoBehaviour
{
    public static FlowField Instance;

    public static int gridSizeX = 25;
    public static int gridSizeY = 50;
    public static int gridSizeZ = 10;
    public static float cellSize = 2f;

    public Transform target;
    public NavMeshSurface surface;// 목표 위치 (예: 성)
    public GameObject obstaclePrefab;

    public enum CellType { Empty, Obstacle, Wall, Water }
    private CellType[,,] grid;

    private Vector3[,,] flowVectors;   // 각 셀이 향해야 하는 방향
    private float[,,] costMap;         // 목표까지 거리 비용

    private Vector3Int[] moves = new Vector3Int[]
    {
        new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
        new Vector3Int(0,1,0), new Vector3Int(0,-1,0),
        new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
    };
    private int[] moveCost = new int[] {1,1,1,1,3,3};

    private void Awake()
    {
        Instance = this;

        grid = new CellType[gridSizeX, gridSizeY, gridSizeZ];
        flowVectors = new Vector3[gridSizeX, gridSizeY, gridSizeZ];
        costMap = new float[gridSizeX, gridSizeY, gridSizeZ];

        LoadObstacles();
    }

    private void Start()
    {
        GenerateFlowField();
    }

    #region Cell 관리
    public void SetCell(int x, int y, int z, CellType type)
    {
        if (InBounds(x, y, z))
        {
            grid[x, y, z] = type;
            GenerateFlowField(); // 필요 시 전체 필드 재계산
        }
    }

    public CellType GetCell(int x, int y, int z)
    {
        if (InBounds(x, y, z)) return grid[x, y, z];
        return CellType.Wall; // 범위 밖은 벽
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

        // 초기화
        for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
                for (int z = 0; z < gridSizeZ; z++)
                    grid[x, y, z] = CellType.Empty;

        foreach (var data in wrapper.items)
        {
            for (int dx = -(data.size.x / 2); dx <= data.size.x / 2; dx++)
                for (int dy = -(data.size.y / 2); dy <= data.size.y / 2; dy++)
                    for (int dz = 0; dz < data.size.z; dz++)
                    {
                        int gx = data.position.x + dx;
                        int gy = data.position.y + dy;
                        int gz = data.position.z + dz;
                        if (InBounds(gx, gy, gz))
                            grid[gx, gy, gz] = data.cellType;
                    }

            // 시각화
            Vector3 pos = new Vector3(data.position.x * cellSize + cellSize / 2,
                                      data.position.y * cellSize + cellSize / 2,
                                      data.position.z * cellSize);
            Instantiate(obstaclePrefab, pos, Quaternion.identity);
        }
    }
    #endregion

    #region FlowField 생성
    public void GenerateFlowField()
    {
        // 비용 초기화
        for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
                for (int z = 0; z < gridSizeZ; z++)
                    costMap[x, y, z] = float.MaxValue;

        // 목표 좌표
        int targetX = Mathf.Clamp(Mathf.FloorToInt(target.position.x / cellSize), 0, gridSizeX - 1);
        int targetY = Mathf.Clamp(Mathf.FloorToInt(target.position.y / cellSize), 0, gridSizeY - 1);
        int targetZ = Mathf.Clamp(Mathf.FloorToInt(target.position.z / cellSize), 0, gridSizeZ - 1);

        // 우선순위 큐
        var heap = new SimplePriorityQueue<Vector3Int, float>();
        Vector3Int dest = new Vector3Int(targetX, targetY, targetZ);
        heap.Enqueue(dest, 0);
        costMap[targetX, targetY, targetZ] = 0;

        while (heap.Count > 0)
        {
            Vector3Int current = heap.Dequeue();
            float currentCost = costMap[current.x, current.y, current.z];

            for (int i = 0; i < moves.Length; i++)
            {
                Vector3Int move = moves[i];
                int nx = current.x + move.x;
                int ny = current.y + move.y;
                int nz = current.z + move.z;

                if (!InBounds(nx, ny, nz)) continue;
                if (grid[nx, ny, nz] == CellType.Obstacle || grid[nx, ny, nz] == CellType.Wall)
                    continue;

                float newCost = currentCost + moveCost[i];
                if (newCost < costMap[nx, ny, nz])
                {
                    costMap[nx, ny, nz] = newCost;
                    heap.Enqueue(new Vector3Int(nx, ny, nz), newCost);
                }
            }
        }

        // Flow vector 계산
        for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
                for (int z = 0; z < gridSizeZ; z++)
                {
                    Vector3 bestDir = Vector3.zero;
                    float minCost = costMap[x, y, z];

                    for (int i = 0; i < moves.Length; i++)
                    {
                        int nx = x + moves[i].x;
                        int ny = y + moves[i].y;
                        int nz = z + moves[i].z;
                        if (!InBounds(nx, ny, nz)) continue;

                        if (costMap[nx, ny, nz] < minCost)
                        {
                            minCost = costMap[nx, ny, nz];
                            bestDir = new Vector3(moves[i].x, moves[i].y, moves[i].z);
                        }
                    }

                    flowVectors[x, y, z] = bestDir.normalized;
                }

        // ECS 동기화
        SyncToECS();
    }

    public void SyncToECS()
    {
        
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var query = em.CreateEntityQuery(typeof(FlowFieldComponent));
        var entities = query.ToEntityArray(Allocator.Temp);

        if (entities.Length == 0)
        {
            Debug.LogError("FlowFieldComponent 엔티티 없음!");
            return;
        }

        var flowFieldEntity = entities[0];
        int totalCells = gridSizeX * gridSizeY * gridSizeZ;
        var weightsNative = new NativeArray<float>(totalCells, Allocator.Persistent);

        // 2. 인덱스 리스트 생성 (0 ~ totalCells-1)
        List<int> indices = new List<int>(totalCells);
        for (int x = 0; x < gridSizeX; x++)
        for (int y = 0; y < gridSizeY; y++)
        for (int z = 0; z < gridSizeZ; z++)
        {
            int idx = x + y * gridSizeX + z * gridSizeX * gridSizeY;
            weightsNative[idx] = costMap[x, y, z]; // Weights에 채우기
            indices.Add(idx);                     // 인덱스 리스트에 추가
        }

        // 3. MonoBehaviour에서 List.Sort 사용
        indices.Sort((a, b) => weightsNative[a].CompareTo(weightsNative[b]));

        // 4. ProcessOrder용 NativeArray 생성
        var processOrderNative = new NativeArray<int>(totalCells, Allocator.Persistent);
        for (int i = 0; i < totalCells; i++)
            processOrderNative[i] = indices[i];
        em.SetComponentData(flowFieldEntity, new FlowFieldComponent
        {
            Weights = weightsNative,
            ProcessOrder=processOrderNative
        });

        entities.Dispose();
    }
    #endregion

    #region 유닛이 방향 얻기
    public Vector3 GetFlowDirection(Vector3 position)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt(position.x / cellSize), 0, gridSizeX - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(position.y / cellSize), 0, gridSizeY - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(position.z / cellSize), 0, gridSizeZ - 1);

        return flowVectors[x, y, z];
    }
    #endregion

    #region 유틸
    private bool InBounds(int x, int y, int z)
    {
        return x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY && z >= 0 && z < gridSizeZ;
    }
    #endregion

    #region 비용 기준 좌표 정렬
    public List<Vector3Int> GetSortedCoordsByCost()
    {
        List<Vector3Int> sortedCoords = new List<Vector3Int>();
        for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
                for (int z = 0; z < gridSizeZ; z++)
                    if (costMap[x, y, z] < float.MaxValue)
                        sortedCoords.Add(new Vector3Int(x, y, z));

        sortedCoords.Sort((a, b) => costMap[a.x, a.y, a.z].CompareTo(costMap[b.x, b.y, b.z]));
        return sortedCoords;
    }
    #endregion
}

