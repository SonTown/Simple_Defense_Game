using System;
using System.Diagnostics;
using System.Numerics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using Unity.VisualScripting;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial class EnemyMoveSystem : SystemBase
{
    private NativeArray<int> CellStart;
    private NativeArray<int> CellCount;
    private NativeArray<int> CellIndices;
    private NativeArray<int> Personnel;
    private NativeArray<CellMoveInfo> MoveInfo;
    private NativeArray<float> weightsValues;
    private NativeArray<int> orderValues;
    private ComponentLookup<EnemyPositionComponent> TargetLookup;
    public NativeArray<Entity> Enemies;
    protected override void OnCreate()
    {
        RequireForUpdate<EnemySpawnerComponent>();
        CellStart= new NativeArray<int>(15000,Allocator.Persistent);
        for (int i = 0; i < CellStart.Length; i++)
        {
            CellStart[i] = 50*i;
        }
        CellCount= new NativeArray<int>(15000,Allocator.Persistent);
        CellIndices= new NativeArray<int>(15000*50,Allocator.Persistent);
        MoveInfo = new NativeArray<CellMoveInfo>(15000,Allocator.Persistent);
        Personnel=new NativeArray<int>(15000,Allocator.Persistent);
        TargetLookup = GetComponentLookup<EnemyPositionComponent>(false);
        weightsValues = new NativeArray<float>(15000, Allocator.Persistent);
        orderValues = new NativeArray<int>(15000, Allocator.TempJob);
    }

    protected override void OnStartRunning()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var spawner = SystemAPI.GetSingleton<EnemySpawnerComponent>();
        var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridData>();
        var gridData = SystemAPI.GetComponent<SpatialGridData>(gridEntity);
        int enemyCount = 15000;

        // NativeArray 생성
        Enemies = new NativeArray<Entity>(enemyCount, Allocator.Persistent);
        int index = 0;
        for (int x = 0; x < 25; x++)
        {
            for (int y = 40; y < 50; y++)
            {
                for (int z = 0; z < 1; z++)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        var e = em.Instantiate(spawner.Prefab);
                        // 순서대로 NativeArray에 저장
                        Enemies[index] = e;
                        var random = Random.CreateFromIndex((uint)i);
                        LocalTransform t = LocalTransform.FromPosition(
                            new float3(random.NextFloat(x*gridData.CellSize, (x+1)*gridData.CellSize),random.NextFloat(z*gridData.CellSize, (z+1)*gridData.CellSize),random.NextFloat(y*gridData.CellSize, (y+1)*gridData.CellSize))
                        );
                        em.SetComponentData(e, t);

                        em.SetComponentData(e, new EnemyPositionComponent
                        {
                            entityIndex = index,
                            position = t.Position,
                            cell = new int3(x,y,z),
                            cellIndex = x+y*gridData.GridSizeX+z*gridData.GridSizeX*gridData.GridSizeY,
                            targetPosition=t.Position,
                            targetCell = new int3(x,y,z)
                        });
                        Personnel[x + y * gridData.GridSizeX + z * gridData.GridSizeX * gridData.GridSizeY]++;
                        index++;
                    }
                }
            }
        }
        // 변경된 NativeArray를 다시 SpatialGridData에 반영
        em.SetComponentData(gridEntity, gridData);
        var query = GetEntityQuery(typeof(WeightElement), typeof(ProcessOrderElement));
        var entity = query.GetSingletonEntity();

        var weightsBuffer = EntityManager.GetBuffer<WeightElement>(entity);
        var orderBuffer = EntityManager.GetBuffer<ProcessOrderElement>(entity);

        var weightsNative = weightsBuffer.AsNativeArray();
        var orderNative = orderBuffer.AsNativeArray();
        for (int i = 0; i < weightsNative.Length; i++)
            weightsValues[i] = weightsNative[i].value;
        
        for (int i = 0; i < orderNative.Length; i++)
            orderValues[i] = orderNative[i].value;
    }
    protected override void OnUpdate()
    {
        
        TargetLookup.Update(this);
        for (int i = 0; i < CellCount.Length; i++)
        {
            CellCount[i] = 0;
        }
        
        var gridData = SystemAPI.GetSingleton<SpatialGridData>();
        // 1. EnemyMoveJob (병렬)
        var enemyMoveJob = new MoveEnemyJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            CellStart = CellStart,
            CellCount = CellCount,
            CellIndices =  CellIndices
        };
        JobHandle enemyMoveHandle = enemyMoveJob.ScheduleParallel(Dependency);

        // 2. MoveEnemyJob (메인 스레드)
        var replaceJob = new PositionReplaceJob()
        {
           ProcessOrder = orderValues,
           MoveInfo = MoveInfo,
           Personnel = Personnel,
           Available = CellCount,
           Weights = weightsValues,
           GridSizeX = gridData.GridSizeX,
           GridSizeY = gridData.GridSizeY,
           GridSizeZ = gridData.GridSizeZ
        };
        JobHandle moveEnemyHandle = replaceJob.Schedule(enemyMoveHandle); // 이전 Job 완료 후 실행

        // 3. AssignMoveJob (병렬)
        var assignMoveJob = new AssignMoveJob
        {
            CellStart = CellStart,
            CellCount = CellCount,
            CellIndices = CellIndices,
            targets = Enemies,
            CellMoves = MoveInfo,
            TargetLookup = TargetLookup,
            GridSizeX = gridData.GridSizeX,
            GridSizeY = gridData.GridSizeY,
            GridSizeZ = gridData.GridSizeZ
        };
        JobHandle assignMoveHandle = assignMoveJob.Schedule(gridData.GridSizeX*gridData.GridSizeY*gridData.GridSizeZ, 64, moveEnemyHandle); 
        // enemyCount는 처리할 요소 수, 64는 batch size

        // 최종 의존성 저장
        Dependency = assignMoveHandle;
    }
}



[BurstCompile]
public partial struct PositionReplaceJob : IJobEntity
{
    [ReadOnly] public NativeArray<int> ProcessOrder;
    public NativeArray<CellMoveInfo> MoveInfo;
    public NativeArray<int> Personnel;
    public NativeArray<int> Available;
    [ReadOnly] public NativeArray<float> Weights;
    public int GridSizeX;
    public int GridSizeY;
    public int GridSizeZ;
    public void Execute()
    {
        Span<int> neighbors= stackalloc int[6];
        Span<float> diffs = stackalloc float[6];
        Span<int> counts = stackalloc int[6];
        int idx, availableHere;
        int x, y, z, i;
        float weightHere, totalDiff;
        int nIdx, sum;
        float diff;
        var length = ProcessOrder.Length;
        for (int orderIdx = 0; orderIdx < length; orderIdx++)
        {
            idx = ProcessOrder[orderIdx];
            availableHere = Available[idx];
            if (availableHere <= 0) continue;
            x = idx % GridSizeX;
            y = (idx / GridSizeX) % GridSizeY;
            z = idx / (GridSizeX * GridSizeY);
            //Span<int> neighbors = stackalloc int[6];
            neighbors[0] = x > 0 ? idx - 1 : -1;
            neighbors[1] = x < GridSizeX - 1 ? idx + 1 : -1;
            neighbors[2] = y > 0 ? idx - GridSizeX : -1;
            neighbors[3] = y < GridSizeY - 1 ? idx + GridSizeX : -1;
            neighbors[4] = z > 0 ? idx - GridSizeX * GridSizeY : -1;
            neighbors[5] = z < GridSizeZ - 1 ? idx + GridSizeX * GridSizeY : -1;
            weightHere = Weights[idx];
            // --- 후보를 찾고 분배 ---
            totalDiff = 0;
            for (i = 0; i < 6; i++)
            {
                nIdx = neighbors[i];
                if (nIdx < 0)
                {
                    diffs[i] = 0;
                    continue;
                }
                diff = math.max(0, weightHere - Weights[nIdx]);

                diffs[i] = diff;
                totalDiff += diff;
            }

            if (totalDiff <= 0) continue;
            sum = 0;
            for (i = 0; i < 6; i++)
            {
                int targetIdx = neighbors[i];
                if (targetIdx < 0 || diffs[i] <= 0)
                {
                    counts[i] = 0;
                    continue;
                }

                int moveCount = (int)math.floor(availableHere * (diffs[i] / totalDiff));
                moveCount = math.min(moveCount, 50 - Personnel[targetIdx]);

                counts[i] = moveCount;
                sum += moveCount;
            }

            // 남은 수를 Weight 차 큰 순서대로 보충
            int remaining = availableHere - sum;
            for (i = 0; i < 6 && remaining > 0; i++)
            {
                int targetIdx = neighbors[i];
                if (targetIdx < 0 || diffs[i] <= 0) continue;

                int capacity = 50 - (Personnel[targetIdx] + counts[i]);
                if (capacity > 0)
                {
                    counts[i]++;
                    sum++;
                    remaining--;
                }
            }
            var tmp = MoveInfo[idx]; // 복사본 가져오기
            for (i = 0; i < 6; i++)
            {
                tmp.Set(i, counts[i]); // 복사본에 값 세팅
            }

            MoveInfo[idx] = tmp;
        }
    }
}



