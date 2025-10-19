using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public static class FlowFieldUtils
{
    public static int Index(int x, int y, int gridSizeX)
    {
        return x + y * gridSizeX;
    }

    public static float GetWeight(
        [ReadOnly] NativeArray<float> flowVectors,
        SpatialGridData data, 
        float3 worldPos)
    {
        int x = math.clamp((int)(worldPos.x / data.CellSize), 0, data.GridSizeX - 1);
        int y = math.clamp((int)(worldPos.z / data.CellSize), 0, data.GridSizeY - 1);
        int z = math.clamp((int)(worldPos.y / data.CellSize), 0, data.GridSizeZ - 1);
        int index = x + y * data.GridSizeX + z * data.GridSizeX * data.GridSizeY; // 안전 체크
        return flowVectors[index];
    }

    public static int3 GetGrid(SpatialGridData data, float3 worldPos)
    {
        int x = math.clamp((int)(worldPos.x / data.CellSize), 0, data.GridSizeX - 1);
        int y = math.clamp((int)(worldPos.z / data.CellSize), 0, data.GridSizeY - 1);
        int z = math.clamp((int)(worldPos.y / data.CellSize), 0, data.GridSizeZ - 1);
        return new int3(x, y, z);
    }
}

public static class FindUtils
{
    public static bool HasLineOfSight(int3 start, int3 end, SpatialGridData grid, NativeArray<float> weights, bool checkStart = true)
    {
        int3 diff = math.abs(end - start);

        // sign 계산: start == end 일 때 0 처리
        int3 sign = new int3(
            start.x < end.x ? 1 : (start.x > end.x ? -1 : 0),
            start.y < end.y ? 1 : (start.y > end.y ? -1 : 0),
            start.z < end.z ? 1 : (start.z > end.z ? -1 : 0)
        );

        int3 pos = start;

        // 차이가 0이면 바로 통과
        if (math.all(diff == 0))
            return true;

        float3 delta = new float3(diff.x, diff.y, diff.z);

        // 가장 큰 축 기준 Bresenham
        if (diff.x >= diff.y && diff.x >= diff.z && diff.x != 0)
        {
            delta /= diff.x;
            float errY = -0.5f, errZ = -0.5f;

            for (int i = 0; i <= diff.x; i++)
            {
                if (!(i == 0 && !checkStart) && IsBlocked(pos, grid, weights))
                {
                    //Debug.LogWarning(pos+" "+delta+" "+sign+" "+diff+" "+start+" "+end);
                    return false;
                }

                errY += delta.y;
                errZ += delta.z;
                pos.x += sign.x;
                if (errY >= 0.5f) { pos.y += sign.y; errY -= 1f; }
                if (errZ >= 0.5f) { pos.z += sign.z; errZ -= 1f; }
            }
        }
        else if (diff.y >= diff.x && diff.y >= diff.z && diff.y != 0)
        {
            delta /= diff.y;
            float errX = -0.5f, errZ = -0.5f;

            for (int i = 0; i <= diff.y; i++)
            {
                if (!(i == 0 && !checkStart) && IsBlocked(pos, grid, weights))
                {
                    //Debug.LogWarning(pos+" "+delta+" "+sign+" "+diff+" "+start+" "+end);
                    return false;
                }

                errX += delta.x;
                errZ += delta.z;
                pos.y += sign.y;
                if (errX >= 0.5f) { pos.x += sign.x; errX -= 1f; }
                if (errZ >= 0.5f) { pos.z += sign.z; errZ -= 1f; }
            }
        }
        else if (diff.z != 0)
        {
            delta /= diff.z;
            float errX = -0.5f, errY = -0.5f;

            for (int i = 0; i <= diff.z; i++)
            {
                if (!(i == 0 && !checkStart) && IsBlocked(pos, grid, weights))
                {
                    //Debug.LogWarning(pos+" "+delta+" "+sign+" "+diff+" "+start+" "+end);
                    return false;
                }

                errX += delta.x;
                errY += delta.y;
                pos.z += sign.z;
                if (errX >= 0.5f) { pos.x += sign.x; errX -= 1f; }
                if (errY >= 0.5f) { pos.y += sign.y; errY -= 1f; }
            }
        }

        return true;
}


    // 셀 내부가 벽인지 체크
    public static bool IsBlocked(int3 cell, SpatialGridData grid, NativeArray<float> weights)
    {
        if (cell.x < 0 || cell.x >= grid.GridSizeX ||
            cell.y < 0 || cell.y >= grid.GridSizeY ||
            cell.z < 0 || cell.z >= grid.GridSizeZ)
            return true;
        int idx = GetCellIndex(cell, grid);
        return (weights[idx] > (float.MaxValue/10)); // 예: 0=빈공간, 1=벽
    }

    // 셀 인덱스 계산
    public static int GetCellIndex(int3 c, SpatialGridData grid)
    {
        return c.x + c.y * grid.GridSizeX + c.z * (grid.GridSizeX * grid.GridSizeY);
    }

    // 셀 중심 좌표
    public static float3 GetCellCenter(int3 c, SpatialGridData grid)
    {
        return new float3(
            (c.x + 0.5f) * grid.CellSize,
            (c.z + 0.5f) * grid.CellSize,
            (c.y + 0.5f) * grid.CellSize
        );
    }
}