using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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