using System.Numerics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;

public partial class EnemyMoveSystem : SystemBase
{
    private NativeArray<float3> allPositions;
    private NativeArray<int> entityToIndex;
    private NativeParallelMultiHashMap<int, Entity> mapA;
    private NativeParallelMultiHashMap<int, Entity> mapB;
    private bool useMapA = true;
    protected override void OnCreate()
    {
        int estimatedEntityCount = 10000;
        mapA = new NativeParallelMultiHashMap<int, Entity>(estimatedEntityCount, Allocator.Persistent);
        mapB = new NativeParallelMultiHashMap<int, Entity>(estimatedEntityCount, Allocator.Persistent);
    }

    protected override void OnUpdate()
    {
        float dt = SystemAPI.Time.DeltaTime;
        SpatialGridData gridData = SystemAPI.GetSingleton<SpatialGridData>();
        FlowFieldComponent flowField = SystemAPI.GetSingleton<FlowFieldComponent>();

        // 현재 읽기용, 쓰기용 Map 결정
        var readMap = useMapA ? mapA : mapB;
        var writeMap = useMapA ? mapB : mapA;

        // 쓰기용은 매 프레임 Clear
        writeMap.Clear();
        var deltaTime = SystemAPI.Time.DeltaTime;
        var enemyQuery = SystemAPI.QueryBuilder()
                            .WithAll<EnemyTag, EnemyBaseDataComponent>()
                            .WithOptions(EntityQueryOptions.IncludeDisabledEntities)
                            .Build();
        int enemyCount = enemyQuery.CalculateEntityCount();

        // --- NativeArray 초기화 ---
        if (!allPositions.IsCreated || allPositions.Length != enemyCount)
        {
            if (allPositions.IsCreated) allPositions.Dispose();
            if (entityToIndex.IsCreated) entityToIndex.Dispose();

            allPositions = new NativeArray<float3>(100000, Allocator.TempJob);
            entityToIndex = new NativeArray<int>(100000, Allocator.TempJob);
        }

        // cellEntities 초기화
        int cellCount = gridData.GridSizeX * gridData.GridSizeY;
        if (!cellEntities.IsCreated || cellEntities.Length != cellCount)
        {
            if (cellEntities.IsCreated)
            {
                for (int i = 0; i < cellEntities.Length; i++)
                    cellEntities[i].Dispose();
                cellEntities.Dispose();
            }

            cellEntities = new NativeArray<NativeList<int>>(cellCount, Allocator.TempJob);
            for (int i = 0; i < cellCount; i++)
                cellEntities[i] = new NativeList<int>(Allocator.TempJob);
        }
        else
        {
            for (int i = 0; i < cellEntities.Length; i++)
                cellEntities[i].Clear();
        }

        // --- 위치 복사 & 셀 매핑 ---
        int index = 0;
        var entities = enemyQuery.ToEntityArray(Allocator.Temp);
        foreach (var e in entities)
        {
            var pos = SystemAPI.GetComponent<LocalTransform>(e).Position;
            allPositions[index] = pos;
            //entityToIndex[e.Index] = index;

            int2 cell = FlowFieldUtils.GetGrid(gridData, pos);
            int key = cell.x + cell.y * gridData.GridSizeX;
            cellEntities[key].Add(index);

            index++;
        }

        // --- Job 스케줄링 ---
        new EnemyMoveJob
        {
            deltaTime = deltaTime,
            gridData = gridData,
            flowField = flowField,
            allPositions = allPositions,
            entityToIndex = entityToIndex,
            //cellEntities = cellEntities
        }.ScheduleParallel();

        // 다음 프레임에 Map 교체
        useMapA = !useMapA;
    }
}



[BurstCompile]
public partial struct EnemyMoveJob : IJobEntity
{
    public float deltaTime;
    public SpatialGridData gridData;
    public FlowFieldComponent flowField;

    [ReadOnly] public NativeArray<float3> allPositions;  // 모든 적 위치 미리 복사
    [ReadOnly] public NativeArray<int> entityToIndex;     // Entity -> Index 매핑
    //[ReadOnly] public NativeArray<NativeList<int>> cellEntities; // 각 셀의 적 인덱스 리스트

    public void Execute(Entity entity,
                        ref LocalTransform localTransform,
                        in EnemyBaseDataComponent baseData,
                        in EnemyTag tag)
    {
        var data = baseData.enemy.Value;
        float3 oldPos = localTransform.Position;

        // Flowfield 이동 방향
        float3 dir = FlowFieldUtils.GetFlowDirection(flowField.FlowVectors, gridData, oldPos);

        // --- Separation Force ---
        float3 separation = float3.zero;
        int2 cell = FlowFieldUtils.GetGrid(gridData, oldPos);

        for (int ox = 1; ox <= 0; ox++)
        {
            for (int oy = 1; oy <= 0; oy++)
            {
                int nx = cell.x + ox;
                int ny = cell.y + oy;
                if (nx < 0 || nx >= gridData.GridSizeX || ny < 0 || ny >= gridData.GridSizeY)
                    continue;

                int key = nx + ny * gridData.GridSizeX;
                NativeList<int> neighbors = new NativeList<int>();

                for (int i = 0; i < neighbors.Length; i++)
                {
                    int otherIndex = neighbors[i];
                    if (entityToIndex[entity.Index] == otherIndex) continue;

                    float3 otherPos = allPositions[otherIndex];
                    float3 toMe = oldPos - otherPos;
                    float dSqr = math.lengthsq(toMe);

                    if (dSqr > 1e-6f && dSqr < data.separationDistance * data.separationDistance)
                        separation += toMe / dSqr;
                }
            }
        }

        separation.y = 0;
        if (!math.all(separation == float3.zero))
            separation = math.normalize(separation);

        // 최종 방향 = Flow + Separation
        float3 finalDir = math.normalize(dir + separation * data.seperationPercent);

        // 위치 갱신
        float3 newPos = oldPos + finalDir * data.speed * deltaTime;
        localTransform.Position = newPos;
    }
}



