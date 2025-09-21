using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public struct SpatialGridData : IComponentData
{
    public int GridSizeX;
    public int GridSizeY;
    public int GridSizeZ;
    public float CellSize;
}

public struct FlowFieldComponent : IComponentData
{
    [ReadOnly] public NativeArray<float> Weights;
    [ReadOnly] public NativeArray<int> ProcessOrder;
}

public partial class FlowFieldSystem : SystemBase
{
    public NativeArray<float> Weights;
    public NativeArray<int> ProcessOrder;

    protected override void OnCreate()
    {
        // 예시 크기
        int n = 100;

        // NativeArray 생성
        Weights = new NativeArray<float>(n, Allocator.Persistent);
        ProcessOrder = new NativeArray<int>(n, Allocator.Persistent);

        // 데이터를 초기화 (예시)
        for (int i = 0; i < n; i++)
        {
            Weights[i] = i * 0.1f;
            ProcessOrder[i] = i;
        }

        // Singleton Entity 생성 후 Component 할당
        var entity = EntityManager.CreateEntity();
        EntityManager.AddComponentData(entity, new FlowFieldComponent
        {
            Weights = Weights,
            ProcessOrder = ProcessOrder
        });
    }

    protected override void OnDestroy()
    {
        // NativeArray Dispose
        if (Weights.IsCreated) Weights.Dispose();
        if (ProcessOrder.IsCreated) ProcessOrder.Dispose();
    }

    protected override void OnUpdate() { }
}


// 전역적으로 접근할 수 있는 파티션 컨테이너
public struct SpatialPartition : IComponentData
{
    public NativeArray<int> Personnel;
}

[BurstCompile]
public struct CellMoveInfo
{
    public int ToPosX;
    public int ToNegX;
    public int ToPosY;
    public int ToNegY;
    public int ToPosZ;
    public int ToNegZ;

    public int Get(int index)
    {
        switch (index)
        {
            case 0: return ToPosX;
            case 1: return ToNegX;
            case 2: return ToPosY;
            case 3: return ToNegY;
            case 4: return ToPosZ;
            case 5: return ToNegZ;
            default: return 0;
        }
    }

    public void Set(int index, int value)
    {
        switch (index)
        {
            case 0: ToPosX = value; break;
            case 1: ToNegX = value; break;
            case 2: ToPosY = value; break;
            case 3: ToNegY = value; break;
            case 4: ToPosZ = value; break;
            case 5: ToNegZ = value; break;
        }
    }
}

[BurstCompile]
public struct AssignMoveJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int> CellStart;
    [ReadOnly] public NativeArray<int> CellCount;
    [ReadOnly] public NativeArray<int> CellIndices;
    [ReadOnly] public NativeArray<Entity> targets;
    [ReadOnly] public NativeArray<CellMoveInfo> CellMoves;

    public ComponentLookup<EnemyPositionComponent> TargetLookup; // 엔티티에 타겟 셀 기록
    [ReadOnly] public int GridSizeX;
    [ReadOnly] public int GridSizeY;
    [ReadOnly] public int GridSizeZ;
    [ReadOnly] public float CellSize;

    public void Execute(int cellId)
    {
        var moveInfo = CellMoves[cellId];
        int start = CellStart[cellId];
        int count = CellCount[cellId];
        if (count == 0) return;

        // direction offsets (6방향)
        Span<int3> dirs = stackalloc int3[6]
        {
            new int3( 1, 0, 0),
            new int3(-1, 0, 0),
            new int3( 0, 1, 0),
            new int3( 0,-1, 0),
            new int3( 0, 0, 1),
            new int3( 0, 0,-1)
        };

        int assignedOffset = 0;

        for (int d = 0; d < 6; d++)
        {
            int moveCount = moveInfo.Get(d);
            if (moveCount == 0) continue;

            // 이 셀의 (x,y,z) 좌표
            int x = cellId % GridSizeX;
            int y = (cellId / GridSizeX) % GridSizeY;
            int z = cellId / (GridSizeX * GridSizeY);

            int3 dst = new int3(x, y, z) + dirs[d];
            if (dst.x < 0 || dst.y < 0 || dst.z < 0 ||
                dst.x >= GridSizeX || dst.y >= GridSizeY || dst.z >= GridSizeZ)
                continue; // 범위 밖

            int targetCellId = dst.x + dst.y * GridSizeX + dst.z * GridSizeX * GridSizeY;

            // moveCount만큼 엔티티에 할당
            for (int i = 0; i < moveCount && (assignedOffset+i) < count; i++)
            {
                int entityIndex = CellIndices[start + assignedOffset + i];
                Entity e = targets[entityIndex];
                TargetLookup[e].SetTargetCell(dst,CellSize,targetCellId,(uint)(i * 12345 + 1));
            }

            assignedOffset += moveCount;
        }
    }
}


