using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
[BurstCompile]
public partial struct EnemyDamageJob : IJob
{
    [ReadOnly] public NativeArray<int> CellStart;     // 각 셀의 시작 인덱스
    [ReadOnly] public NativeArray<int> CellCount;     // 각 셀에 들어 있는 개수
    [ReadOnly] public NativeArray<int> CellIndices;   // 전체 엔티티 인덱스
    [ReadOnly] public NativeArray<Entity> Targets;
    [ReadOnly] public SpatialGridData GridData;
    [ReadOnly] public ComponentLookup<EnemyPositionComponent> TargetLookup; 
    public ComponentLookup<EnemyHealthComponent> HealthLookup;
    public ComponentLookup<IsAlive> IsAliveLookup;
    [ReadOnly] public NativeList<ShootEvent> shootInfos;
    public EntityCommandBuffer ecb;
    public void Execute()
    {
        foreach (ShootEvent shootEvent in shootInfos)
        {
            Entity target = shootEvent.target;
            float3 targetPosition = TargetLookup[target].position;
            int3 cell = FlowFieldUtils.GetGrid(GridData,targetPosition);
            
            float bestDistSq = (shootEvent.range+GridData.CellSize) * (shootEvent.range+GridData.CellSize);
            int maxRadius = (int)math.ceil(shootEvent.range / GridData.CellSize);
            for (int dx = -maxRadius; dx <= maxRadius; dx++)
            for (int dy = -maxRadius; dy <= maxRadius; dy++)
            for (int dz = -maxRadius; dz <= maxRadius; dz++)
            {
                int3 neighbor = cell + new int3(dx, dy, dz);

                if (neighbor.x < 0 || neighbor.x >= GridData.GridSizeX ||
                    neighbor.y < 0 || neighbor.y >= GridData.GridSizeY ||
                    neighbor.z < 0 || neighbor.z >= GridData.GridSizeZ)
                    continue;

                int cellIndex = neighbor.x
                                + neighbor.y * GridData.GridSizeX
                                + neighbor.z * (GridData.GridSizeX * GridData.GridSizeY);

                // 셀 중심 좌표
                float3 cellCenter = new float3(
                    (neighbor.x + 0.5f) * GridData.CellSize,
                    (neighbor.z + 0.5f) * GridData.CellSize,
                    (neighbor.y + 0.5f) * GridData.CellSize
                );
                float distSq = math.distancesq(targetPosition, cellCenter);
                // 사거리 밖이면 스킵
                if (distSq <= bestDistSq)
                {
                    for (int i = 0; i < CellCount[cellIndex]; i++)
                    {
                        Entity T = Targets[CellIndices[CellStart[cellIndex] + i]];
                        float3 TPosition = TargetLookup[T].position;
                        if (bestDistSq >= math.distancesq(targetPosition, TPosition))
                        {
                            var TComponent =HealthLookup.GetRefRW(T);
                            TComponent.ValueRW.Health -= (int)shootEvent.damage;
                            IsAliveLookup.SetComponentEnabled(T,false);
                            ecb.SetEnabled(T,false);
                        }
                    }
                }
            }
        }
    }
}
