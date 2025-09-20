using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
public static class FlowFieldUtils
{
    public static int Index(int x, int y, int gridSizeX)
    {
        return x + y * gridSizeX;
    }

    public static float3 GetFlowDirection(
        [ReadOnly] NativeParallelHashMap<int,float3> flowVectors,
        SpatialGridData data, 
        float3 worldPos)
    {
        int x = math.clamp((int)(worldPos.x / data.CellSize), 0, data.GridSizeX - 1);
        int y = math.clamp((int)(worldPos.z / data.CellSize), 0, data.GridSizeY - 1);
        int index = x + y * data.GridSizeX; // 안전 체크
        return flowVectors[index];
    }

    public static void SetFlowVector(FlowFieldComponent ff, SpatialGridData data, int x, int y, float3 dir)
    {
        ff.FlowVectors[Index(x, y, data.GridSizeX)] = dir;
    }

    public static int2 GetGrid(SpatialGridData data, float3 worldPos)
    {
        int x = math.clamp((int)(worldPos.x / data.CellSize), 0, data.GridSizeX - 1);
        int y = math.clamp((int)(worldPos.z / data.CellSize), 0, data.GridSizeY - 1);
        return new int2(x, y);
    }
}