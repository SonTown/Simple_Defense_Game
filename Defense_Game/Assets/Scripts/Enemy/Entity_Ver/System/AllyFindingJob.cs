using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct AllyFindingJob : IJobEntity
{
    public NativeArray<float3> Target;   // 각 셀의 가장 가까운 Ally 좌표 저장
    public SpatialGridData data;
    public int captureDist;

    void Execute(in LocalTransform transform, in AllyTag tag)
    {
        int3 cell = FlowFieldUtils.GetGrid(data, transform.Position);

        // 탐색 범위 설정
        for (int dx = -captureDist; dx <= captureDist; dx++)
        {
            for (int dy = -captureDist; dy <= captureDist; dy++)
            {
                for (int dz = -captureDist; dz <= captureDist; dz++)
                {
                    int3 neighbor = cell + new int3(dx, dy, dz);

                    // 범위 체크
                    if (neighbor.x < 0 || neighbor.x >= data.GridSizeX ||
                        neighbor.y < 0 || neighbor.y >= data.GridSizeY ||
                        neighbor.z < 0 || neighbor.z >= data.GridSizeZ)
                        continue;

                    int index = neighbor.x 
                              + neighbor.y * data.GridSizeX
                              + neighbor.z * (data.GridSizeX * data.GridSizeY);

                    // 셀 중심 좌표 계산
                    float3 cellCenter = new float3(
                        (neighbor.x + 0.5f) * data.CellSize,
                        (neighbor.y + 0.5f) * data.CellSize,
                        (neighbor.z + 0.5f) * data.CellSize
                    );

                    float newDist = math.distancesq(transform.Position, cellCenter);

                    // 기존 값이 있는지 확인
                    float3 prev = Target[index];
                    if (math.all(prev == float3.zero))
                    {
                        // 비어 있으면 무조건 기록
                        Target[index] = transform.Position;
                    }
                    else
                    {
                        float prevDist = math.distancesq(prev, cellCenter);
                        if (newDist < prevDist)
                        {
                            // 더 가까우면 갱신
                            Target[index] = transform.Position;
                        }
                    }
                }
            }
        }
    }
}
