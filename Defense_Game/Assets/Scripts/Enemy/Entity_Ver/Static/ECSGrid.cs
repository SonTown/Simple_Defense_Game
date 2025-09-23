using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct SpatialGridData : IComponentData
{
    public int GridSizeX;
    public int GridSizeY;
    public int GridSizeZ;
    public float CellSize;
}

public struct WeightElement : IBufferElementData
{
    public float value;
}

public struct ProcessOrderElement : IBufferElementData
{
    public int value;
}

public struct PersonnelElement : IBufferElementData
{
    public float value;
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
    [NativeDisableParallelForRestriction]
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
            new int3( -1, 0, 0),
            new int3(1, 0, 0),
            new int3( 0, -1, 0),
            new int3( 0,1, 0),
            new int3( 0, 0, -1),
            new int3( 0, 0,1)
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
                var enemy = TargetLookup[e];  // ref로 접근
                enemy.SetTargetCell(dst, CellSize, targetCellId, (uint)(i * 12345 + 1));
                TargetLookup[e] = enemy;
            }

            assignedOffset += moveCount;
        }
    }
}


