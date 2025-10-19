using System.Numerics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct AllyFindingJob : IJobEntity
{
    [NativeDisableParallelForRestriction]
    public NativeArray<float3> Target; // 각 셀의 가장 가까운 Ally 좌표 저장
    [ReadOnly] public NativeArray<float> Weights;
    public SpatialGridData data;
    public int captureDist;

    void Execute(in SoldierComponent soldier)
    {
        int3 cell = FlowFieldUtils.GetGrid(data, soldier.position);

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
                    if (!FindUtils.HasLineOfSight(cell, neighbor, data, Weights))
                    {
                        //Debug.LogError(cell+ " "+neighbor);
                        continue;
                    }

                    float newDist = math.distancesq(soldier.position, cellCenter);

                    // 기존 값이 있는지 확인
                    float3 prev = Target[index];
                    if (math.all(math.abs(prev)<1e-3f))
                    {
                        // 비어 있으면 무조건 기록
                        Target[index] = soldier.position;
                    }
                    else
                    {
                        float prevDist = math.distancesq(prev, cellCenter);
                        if (newDist < prevDist)
                        {
                            // 더 가까우면 갱신
                            Target[index] = soldier.position;
                        }
                    }
                }
            }
        }
    }
}
