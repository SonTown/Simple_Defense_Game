using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial struct EnemyFindingJob : IJobEntity
{
    [ReadOnly] public NativeArray<int> CellStart;     // 각 셀의 시작 인덱스
    [ReadOnly] public NativeArray<int> CellCount;     // 각 셀에 들어 있는 개수
    [ReadOnly] public NativeArray<int> CellIndices;   // 전체 엔티티 인덱스
    [ReadOnly] public NativeArray<Entity> Targets;    // 모든 적 엔티티
    [ReadOnly] public SpatialGridData GridData;
    [ReadOnly] public ComponentLookup<EnemyPositionComponent> TargetLookup;

    void Execute(ref SoldierComponent soldier)
    {
        int3 cell = FlowFieldUtils.GetGrid(GridData, soldier.position);

        float bestDistSq = soldier.atkRange * soldier.atkRange;
        Entity bestTarget = Entity.Null;
        bool found = false;

        // 탐색 범위: soldier 사거리 내 최대 셀 반경
        int maxRadius = (int)math.ceil(soldier.atkRange / GridData.CellSize);
        // 후보 셀을 (거리², cellIndex)로 NativeList에 저장
        NativeList<(float distSq, int cellIndex)> candidateCells = new NativeList<(float, int)>(Allocator.TempJob);
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
                (neighbor.y + 0.5f) * GridData.CellSize,
                (neighbor.z + 0.5f) * GridData.CellSize
            );

            float distSq = math.distancesq(soldier.position, cellCenter);

            // 사거리 밖이면 스킵
            if (distSq <= bestDistSq)
            {
                if (CellCount[cellIndex] > 0)
                {
                    candidateCells.Add((distSq, cellIndex));
                    Debug.Log(neighbor+" : "+cell);
                }
            }
        }

        // 거리 기준 오름차순 정렬
        candidateCells.Sort(new CandidateComparer());

        // 후보 셀 순회
        for (int i = 0; i < candidateCells.Length; i++)
        {
            int cellIndex = candidateCells[i].cellIndex;

            int start = CellStart[cellIndex];
            int count = CellCount[cellIndex];

            for (int j = 0; j < count; j++)
            {
                Entity e = Targets[CellIndices[start + j]];
                if (!TargetLookup.HasComponent(e)) continue;

                float3 enemyPos = TargetLookup[e].position;
                float distSq = math.distancesq(soldier.position, enemyPos);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestTarget = e;
                    found = true;
                }
            }
            // 이미 사거리 내 가장 가까운 적 찾았으면 종료
            if (found) break;
        }

        if (bestTarget != Entity.Null)
        {
            Debug.Log(bestTarget.Index + " : " + bestDistSq);
        }

        soldier.target = bestTarget;
        candidateCells.Dispose();
    }

    // NativeList sort용 comparer
    struct CandidateComparer : System.Collections.Generic.IComparer<(float distSq, int cellIndex)>
    {
        public int Compare((float distSq, int cellIndex) a, (float distSq, int cellIndex) b)
        {
            return a.distSq.CompareTo(b.distSq);
        }
    }
}
