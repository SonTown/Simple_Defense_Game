using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct SpatialGridData : IComponentData
{
    public int GridSizeX;
    public int GridSizeY;
    public float CellSize;
}
public struct FlowFieldComponent : IComponentData
{
    [ReadOnly] public NativeParallelHashMap<int,float3> FlowVectors;
}

// 전역적으로 접근할 수 있는 파티션 컨테이너
public struct SpatialPartition : IComponentData
{
    public NativeParallelMultiHashMap<int, Entity> CellMap;
}

[BurstCompile]
public partial struct SpatialPartitionSystem : ISystem
{
    private NativeParallelMultiHashMap<int, Entity> cellMap;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SpatialGridData>();
        int estimatedEntityCount = 10000; // 대략 전체 엔티티 개수 예상
        cellMap = new NativeParallelMultiHashMap<int, Entity>(estimatedEntityCount, Allocator.Persistent);

        // 전역으로 쓸 수 있게 Entity에 붙여줌
        var e = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(e, new SpatialPartition { CellMap = cellMap });
        state.EntityManager.AddComponentData(e, new FlowFieldComponent {FlowVectors = new NativeParallelHashMap<int,float3>(25*50, Allocator.Persistent)});
        state.EntityManager.AddComponentData(e, new SpatialGridData
        {
            GridSizeX = 25,
            GridSizeY = 50,
            CellSize = 2f
        });
    }

    public void OnDestroy(ref SystemState state)
    {
        if (cellMap.IsCreated)
            cellMap.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        
    }
}
[BurstCompile]
public partial struct UpdateCellMapJob : IJobEntity
{
    public SpatialGridData gridData;
    public NativeParallelMultiHashMap<int, Entity>.ParallelWriter cellMap;

    void Execute(ref EnemyPositionComponent positionComponent, in LocalTransform localTransform, in Entity entity)
    {
        positionComponent.position = localTransform.Position;
        positionComponent.cell = FlowFieldUtils.GetGrid(gridData, positionComponent.position);

        int2 pos = positionComponent.cell;
        int x = pos.x;
        int y = pos.y;

        if (x >= 0 && x < gridData.GridSizeX &&
            y >= 0 && y < gridData.GridSizeY)
        {
            int cellIndex = x + y * gridData.GridSizeX;
            cellMap.Add(cellIndex, entity);
        }
    }
}
