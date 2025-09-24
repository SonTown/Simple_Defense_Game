using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
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
        weightsValues = new NativeArray<float>(12500, Allocator.Persistent);
        orderValues = new NativeArray<int>(12500, Allocator.Persistent);
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
        enemyMoveHandle.Complete();
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
        moveEnemyHandle.Complete();
        
        for (int i = 0; i < 15000; i++)  // 처음 10개만 확인
        {
            if (MoveInfo[i].ToNegY != 0)
            {
                Debug.Log($"After MoveInfo[{i}] = {MoveInfo[i].ToNegY}");
            }
        }
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
            GridSizeZ = gridData.GridSizeZ,
            CellSize = gridData.CellSize
        };
        JobHandle assignMoveHandle = assignMoveJob.Schedule(gridData.GridSizeX*gridData.GridSizeY*gridData.GridSizeZ, 64, moveEnemyHandle); 
        // enemyCount는 처리할 요소 수, 64는 batch size

        // 최종 의존성 저장
        Dependency = assignMoveHandle;
    }
}


[BurstCompile(FloatMode = FloatMode.Fast)]
public struct PositionReplaceJob : IJob
{
    [ReadOnly] public NativeArray<int> ProcessOrder;
    public NativeArray<CellMoveInfo> MoveInfo;
    public NativeArray<int> Personnel;
    public NativeArray<int> Available;
    [ReadOnly] public NativeArray<float> Weights;
    public int GridSizeX, GridSizeY, GridSizeZ;

    [return: AssumeRange(0,15000)]
    public int GetOrder([AssumeRange(0,15000)] int index) => ProcessOrder[index];
    [return: AssumeRange(0,100)]
    public int GetAvailable([AssumeRange(0,15000)] int index) => Available[index];

    [SkipLocalsInit]
    public void Execute()
    {
        // stackalloc 대신 고정 개수의 로컬 변수로 대체
        int x0, x1, x2, x3, x4, x5;
        float d0, d1, d2, d3, d4, d5;
        int c0, c1, c2, c3, c4, c5;

        int length = ProcessOrder.Length;
        for (int orderIdx = 0; orderIdx < length; orderIdx++)
        {
            int idx = GetOrder(orderIdx);
            int availableHere = GetAvailable(idx);
            if (availableHere <= 0) continue;
            int x = idx % GridSizeX;
            int y = (idx / GridSizeX) % GridSizeY;
            int z = idx / (GridSizeX * GridSizeY);
            x0 = x > 0 ? idx - 1 : -1;
            x1 = x < GridSizeX - 1 ? idx + 1 : -1;
            x2 = y > 0 ? idx - GridSizeX : -1;
            x3 = y < GridSizeY - 1 ? idx + GridSizeX : -1;
            x4 = z > 0 ? idx - GridSizeX * GridSizeY : -1;
            x5 = z < GridSizeZ - 1 ? idx + GridSizeX * GridSizeY : -1;

            float weightHere = Weights[idx];

            // diffs
            d0 = (x0 >= 0) ? math.max(0f, weightHere - Weights[x0]) : 0f;
            d1 = (x1 >= 0) ? math.max(0f, weightHere - Weights[x1]) : 0f;
            d2 = (x2 >= 0) ? math.max(0f, weightHere - Weights[x2]) : 0f;
            d3 = (x3 >= 0) ? math.max(0f, weightHere - Weights[x3]) : 0f;
            d4 = (x4 >= 0) ? math.max(0f, weightHere - Weights[x4]) : 0f;
            d5 = (x5 >= 0) ? math.max(0f, weightHere - Weights[x5]) : 0f;

            float totalDiff = d0 + d1 + d2 + d3 + d4 + d5;
            if (totalDiff <= 0f) continue;

            int sum = 0;
            // counts
            c0 = (x0 < 0 || d0 <= 0f) ? 0 : math.min((int)(availableHere * (d0 / totalDiff)), 50 - Personnel[x0]);
            sum += c0;
            c1 = (x1 < 0 || d1 <= 0f) ? 0 : math.min((int)(availableHere * (d1 / totalDiff)), 50 - Personnel[x1]);
            sum += c1;
            c2 = (x2 < 0 || d2 <= 0f) ? 0 : math.min((int)(availableHere * (d2 / totalDiff)), 50 - Personnel[x2]);
            sum += c2;
            c3 = (x3 < 0 || d3 <= 0f) ? 0 : math.min((int)(availableHere * (d3 / totalDiff)), 50 - Personnel[x3]);
            sum += c3;
            c4 = (x4 < 0 || d4 <= 0f) ? 0 : math.min((int)(availableHere * (d4 / totalDiff)), 50 - Personnel[x4]);
            sum += c4;
            c5 = (x5 < 0 || d5 <= 0f) ? 0 : math.min((int)(availableHere * (d5 / totalDiff)), 50 - Personnel[x5]);
            sum += c5;

            int remaining = availableHere - sum;
            // 남은 것 보충
            if (remaining > 0)
            {
                // 보충 로직 (비슷하게 분기 단순화)
                if (x0 >= 0 && d0 > 0f && (50 - (Personnel[x0] + c0)) > 0) { c0++; remaining--;
                    sum++; if (remaining == 0) goto finishRemaining; }
                if (x1 >= 0 && d1 > 0f && (50 - (Personnel[x1] + c1)) > 0) { c1++; remaining--; sum++; if (remaining == 0) goto finishRemaining; }
                if (x2 >= 0 && d2 > 0f && (50 - (Personnel[x2] + c2)) > 0) { c2++; remaining--; sum++; if (remaining == 0) goto finishRemaining; }
                if (x3 >= 0 && d3 > 0f && (50 - (Personnel[x3] + c3)) > 0) { c3++; remaining--; sum++; if (remaining == 0) goto finishRemaining; }
                if (x4 >= 0 && d4 > 0f && (50 - (Personnel[x4] + c4)) > 0) { c4++; remaining--; sum++; if (remaining == 0) goto finishRemaining; }
                if (x5 >= 0 && d5 > 0f && (50 - (Personnel[x5] + c5)) > 0) { c5++; remaining--; sum++; if (remaining == 0) goto finishRemaining; }
            }
            finishRemaining:

            CellMoveInfo tmp = MoveInfo[idx];
            tmp.Set(0, c0); tmp.Set(1, c1); tmp.Set(2, c2);
            tmp.Set(3, c3); tmp.Set(4, c4); tmp.Set(5, c5);
            if (x0 >= 0) Personnel[x0] += c0; if (x1 >= 0) Personnel[x1] += c1;  if (x2 >= 0) Personnel[x2] += c2;
            if (x3 >= 0) Personnel[x3] += c3; if (x4 >= 0) Personnel[x4] += c4;  if (x5 >= 0) Personnel[x5] += c5;
            Personnel[idx] -= sum;
            MoveInfo[idx] = tmp;
        }
    }
}
