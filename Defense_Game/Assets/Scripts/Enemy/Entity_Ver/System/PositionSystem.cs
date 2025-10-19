using System.Runtime.CompilerServices;
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
    private NativeArray<int> AllCount;
    private NativeArray<int> AllIndices;
    private NativeArray<float3> TargetPos;
    private NativeArray<CellMoveInfo> MoveInfo;
    private NativeArray<float> weightsValues;
    private NativeArray<int> orderValues;
    private ComponentLookup<EnemyPositionComponent> TargetLookup;
    private ComponentLookup<EnemyHealthComponent> HealthLookup;
    private ComponentLookup<IsAlive> IsAliveLookup;
    public NativeArray<Entity> Enemies;
    public NativeArray<Random> randomArray;
    private int enemyCount = 75000;
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
        
        AllCount = new NativeArray<int>(15000,Allocator.Persistent);
        AllIndices= new NativeArray<int>(15000*50,Allocator.Persistent);
        MoveInfo = new NativeArray<CellMoveInfo>(15000,Allocator.Persistent);
        Personnel=new NativeArray<int>(15000,Allocator.Persistent);
        TargetPos=new NativeArray<float3>(15000,Allocator.Persistent);
        TargetLookup = GetComponentLookup<EnemyPositionComponent>(false);
        HealthLookup = GetComponentLookup<EnemyHealthComponent>(false);
        IsAliveLookup= GetComponentLookup<IsAlive>(false);
        weightsValues = new NativeArray<float>(12500, Allocator.Persistent);
        orderValues = new NativeArray<int>(12500, Allocator.Persistent);
        randomArray = new NativeArray<Unity.Mathematics.Random>(enemyCount, Allocator.Persistent);
        for (int i = 0; i < enemyCount; i++)
        {
            randomArray[i] = Random.CreateFromIndex((uint)(i + 1));
        }
    }

    protected override void OnStartRunning()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var spawner = SystemAPI.GetSingleton<EnemySpawnerComponent>();
        var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridData>();
        var gridData = SystemAPI.GetSingleton<SpatialGridData>();

        // NativeArray 생성
        Enemies = new NativeArray<Entity>(enemyCount, Allocator.Persistent);
        int index = 0;
        for (int x = 0; x < 25; x++)
        {
            for (int y = 40; y < 50; y++)
            {
                for (int z = 0; z < 5; z++)
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
        HealthLookup.Update(this);
        IsAliveLookup.Update(this);
        var ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        var ecb = ecbSystem.CreateCommandBuffer();
        for (int i = 0; i < CellCount.Length; i++)
        {
            CellCount[i] = 0;
            TargetPos[i] = float3.zero;
            AllCount[i] = 0;
        }
        var gridData = SystemAPI.GetSingleton<SpatialGridData>();
        // 1. EnemyMoveJob- 적군 위치 이동 및 객체 할당
        var enemyMoveJob = new MoveEnemyJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            CellStart = CellStart,
            CellCount = CellCount,
            CellIndices =  CellIndices,
            AllCount = AllCount,
            AllIndices = AllIndices,
        };
        JobHandle enemyMoveHandle = enemyMoveJob.Schedule(Dependency);
        // 2. AllyFindingJob- 아군 위치를 그리드 별로 할당
        var allyFindingJob = new AllyFindingJob
        {
            Target = TargetPos,
            data = gridData,
            captureDist = 4,
            Weights = weightsValues,
        };
        JobHandle allyFindingHandle = allyFindingJob.ScheduleParallel(enemyMoveHandle);
        // 3. EnemyFindingJob- 아군에게 적을 할당
        var enemyFindingJob = new EnemyFindingJob
        {
            CellStart =  CellStart,
            CellCount = AllCount,
            CellIndices = AllIndices,
            Targets = Enemies,
            GridData = gridData,
            TargetLookup = TargetLookup,
            Weights = weightsValues,
        };
        JobHandle enemyFindingHandle = enemyFindingJob.ScheduleParallel(allyFindingHandle);
        // 4. PositionReplaceJob- 그리드별로 명 수 할당
        var replaceJob = new PositionReplaceJob()
        {
           ProcessOrder = orderValues,
           MoveInfo = MoveInfo,
           Personnel = AllCount,
           Available = CellCount,
           Weights = weightsValues,
           TargetPos = TargetPos,
           GridSizeX = gridData.GridSizeX,
           GridSizeY = gridData.GridSizeY,
           GridSizeZ = gridData.GridSizeZ,
           CellSize = gridData.CellSize,
           GridData=gridData,
        };
        JobHandle moveEnemyHandle = replaceJob.Schedule(enemyFindingHandle); // 이전 Job 완료 후 실행
        // 5. AssignMoveJob- 위치에 대상 객체 할당
        var assignMoveJob = new AssignMoveJob
        {
            randomArray = randomArray,
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
        //6. EnemyDamageJob- 적군에 총알에 대한 데미지 판정
        NativeList<ShootEvent> allEvents = new NativeList<ShootEvent>(Allocator.TempJob);
        foreach (var buffer in SystemAPI.Query<DynamicBuffer<ShootEvent>>())
        {
            allEvents.AddRange(buffer.AsNativeArray());
            buffer.Clear();
        }
        var enemyDamageJob = new EnemyDamageJob
        {
            CellStart = CellStart,
            CellCount = AllCount,
            CellIndices = AllIndices,
            Targets = Enemies,
            GridData = gridData,
            TargetLookup = TargetLookup,
            HealthLookup = HealthLookup,
            IsAliveLookup = IsAliveLookup,
            shootInfos = allEvents,
            ecb = ecb,
        };
        JobHandle enemyDamageHandle = enemyDamageJob.Schedule(assignMoveHandle);
        ecbSystem.AddJobHandleForProducer(enemyDamageHandle);
        allEvents.Dispose(enemyDamageHandle);
        Dependency = enemyDamageHandle;
    }
}


[BurstCompile(FloatMode = FloatMode.Fast)]
public struct PositionReplaceJob : IJob
{
    [ReadOnly] public NativeArray<int> ProcessOrder;
    public NativeArray<CellMoveInfo> MoveInfo;
    public NativeArray<int> Personnel;
    public NativeArray<int> Available;
    public NativeArray<float3> TargetPos;
    [ReadOnly] public NativeArray<float> Weights;
    public int GridSizeX, GridSizeY, GridSizeZ;
    public float CellSize;
    public SpatialGridData GridData;

    [return: AssumeRange(0,15000)]
    public int GetOrder([AssumeRange(0,15000)] int index) => ProcessOrder[index];
    [return: AssumeRange(0,100)]
    public int GetAvailable([AssumeRange(0,15000)] int index) => Available[index];
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    float CalcDist(int idx, int cell, int3 targetPos)
    {
        if (cell < 0) return float.MaxValue;

        int3 cellPos = new int3(
            cell % GridSizeX,
            (cell / GridSizeX) % GridSizeY,
            (cell / (GridSizeX * GridSizeY))
        );
        return math.distance(cellPos, targetPos);
    }
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
            int sum = 0;
            x0 = (x > 0 && Weights[idx - 1] < 1000000) ? idx - 1 : -1;
            x1 = (x < GridSizeX - 1 && Weights[idx + 1] < 1000000) ? idx + 1 : -1;
            x2 = (y > 0 && Weights[idx - GridSizeX] < 1000000) ? idx - GridSizeX : -1;
            x3 = (y < GridSizeY - 1 && Weights[idx + GridSizeX] < 1000000) ? idx + GridSizeX : -1;
            x4 = (z > 0 && Weights[idx - GridSizeX * GridSizeY] < 1000000) ? idx - GridSizeX * GridSizeY : -1;
            x5 = (z < GridSizeZ - 1 && Weights[idx + GridSizeX * GridSizeY] < 1000000) ? idx + GridSizeX * GridSizeY : -1;
            c0 = 0;
            c1 = 0;
            c2 = 0;
            c3 = 0;
            c4 = 0;
            c5 = 0;
            CellMoveInfo tmp = MoveInfo[idx];
            float weightHere = Weights[idx];
            // 1단계: 중력 기반 낙하 이동
            if (x4 >= 0 && Weights[x4]<1000000)
            {
                if (Personnel[x4] < 50)
                {
                    c4 = math.min(50 - Personnel[x4], availableHere);
                    availableHere -= c4;
                    Personnel[idx] -= c4;
                    Personnel[x4] += c4;
                }
            }
            // 2단계: 타겟 기반 이동
            if (availableHere > 0)
            {
                float3 target = TargetPos[idx];
                if (math.lengthsq(target) > 1e-3f){ // target이 거의 0이 아닐 때
                    int3 tarPos = FlowFieldUtils.GetGrid(GridData, target);
                    if (math.any(tarPos != new int3(x, y, z))){
                        // 각 방향별 후보 위치
                        float dist0 = CalcDist(idx,x0, tarPos);
                        float dist1 = CalcDist(idx,x1, tarPos);
                        float dist2 = CalcDist(idx,x2, tarPos);
                        float dist3 = CalcDist(idx,x3, tarPos);
                        float dist4 = CalcDist(idx,x4, tarPos);
                        float dist5 = float.MaxValue;
                        // 거리 순 정렬 후, 최대 3개까지 분배
                        for (int count = 0; count < 3 && availableHere > 0; count++)
                        {
                            int bestDir = -1;
                            float bestDist = float.MaxValue/10;

                            if (x0 >= 0 && dist0 < bestDist && Personnel[x0] < 50) { bestDir = 0; bestDist = dist0; }
                            if (x1 >= 0 && dist1 < bestDist && Personnel[x1] < 50) { bestDir = 1; bestDist = dist1; }
                            if (x2 >= 0 && dist2 < bestDist && Personnel[x2] < 50) { bestDir = 2; bestDist = dist2; }
                            if (x3 >= 0 && dist3 < bestDist && Personnel[x3] < 50) { bestDir = 3; bestDist = dist3; }
                            if (x4 >= 0 && dist4 < bestDist && Personnel[x4] < 50) { bestDir = 4; bestDist = dist4; }
                            if (x5 >= 0 && dist5 < bestDist && Personnel[x5] < 50) { bestDir = 5; bestDist = dist5; }

                            if (bestDir < 0) break;

                            int moveCnt = 0;
                            switch (bestDir)
                            {
                                case 0: moveCnt = math.min(availableHere, 50 - Personnel[x0]); Personnel[idx] -= moveCnt; c0 += moveCnt; dist1 = float.MaxValue; dist0 = float.MaxValue; break;
                                case 1: moveCnt = math.min(availableHere, 50 - Personnel[x1]); Personnel[idx] -= moveCnt; c1 += moveCnt; dist0 = float.MaxValue; dist1 = float.MaxValue; break;
                                case 2: moveCnt = math.min(availableHere, 50 - Personnel[x2]); Personnel[idx] -= moveCnt; c2 += moveCnt; dist3 = float.MaxValue; dist2 = float.MaxValue; break;
                                case 3: moveCnt = math.min(availableHere, 50 - Personnel[x3]); Personnel[idx] -= moveCnt; c3 += moveCnt; dist2 = float.MaxValue; dist3 = float.MaxValue; break;
                                case 4: moveCnt = math.min(availableHere, 50 - Personnel[x4]); Personnel[idx] -= moveCnt; c4 += moveCnt; dist5 = float.MaxValue; dist4 = float.MaxValue; break;
                                case 5: moveCnt = math.min(availableHere, 50 - Personnel[x5]); Personnel[idx] -= moveCnt; c5 += moveCnt; dist4 = float.MaxValue; dist5 = float.MaxValue; break;
                            }

                            availableHere -= moveCnt;
                        }

                    }
                }
                else
                {
                    // diffs
                    d0 = (x0 >= 0) ? math.max(0f, weightHere - Weights[x0]) : 0f;
                    d1 = (x1 >= 0) ? math.max(0f, weightHere - Weights[x1]) : 0f;
                    d2 = (x2 >= 0) ? math.max(0f, weightHere - Weights[x2]) : 0f;
                    d3 = (x3 >= 0) ? math.max(0f, weightHere - Weights[x3]) : 0f;
                    d5 = (x5 >= 0) ? math.max(0f, weightHere - Weights[x5]) : 0f;

                    float totalDiff = d0 + d1 + d2 + d3 + d5;
                    if (totalDiff <= 0f)
                    {
                        continue;
                    }
                    // counts
                    c0 += (x0 < 0 || d0 <= 0f) ? 0 : math.min((int)(availableHere * (d0 / totalDiff)), 50 - Personnel[x0]-c0);
                    sum += c0;
                    c1 += (x1 < 0 || d1 <= 0f) ? 0 : math.min((int)(availableHere * (d1 / totalDiff)), 50 - Personnel[x1]-c1);
                    sum += c1;
                    c2 += (x2 < 0 || d2 <= 0f) ? 0 : math.min((int)(availableHere * (d2 / totalDiff)), 50 - Personnel[x2]-c2);
                    sum += c2;
                    c3 += (x3 < 0 || d3 <= 0f) ? 0 : math.min((int)(availableHere * (d3 / totalDiff)), 50 - Personnel[x3]-c3);
                    sum += c3;
                    c5 += (x5 < 0 || d5 <= 0f) ? 0 : math.min((int)(availableHere * (d5 / totalDiff)), 50 - Personnel[x5]-c5);
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

                        if (x5 >= 0 && (50 - (Personnel[x5] + c5)) > 0)
                        {
                            int c6 = math.min(50 - Personnel[x5] - c5, remaining);
                            c5+=c6; 
                            sum+=c6;
                        }
                    }
                }
            }
            finishRemaining:
            tmp.Set(0, c0); tmp.Set(1, c1); tmp.Set(2, c2);
            tmp.Set(3, c3); tmp.Set(4, c4); tmp.Set(5, c5);
            if (x0 >= 0) Personnel[x0] += c0; if (x1 >= 0) Personnel[x1] += c1;  if (x2 >= 0) Personnel[x2] += c2;
            if (x3 >= 0) Personnel[x3] += c3; if (x5 >= 0) Personnel[x5] += c5;
            Personnel[idx] -= sum;
            MoveInfo[idx] = tmp;
        }
    }
}
