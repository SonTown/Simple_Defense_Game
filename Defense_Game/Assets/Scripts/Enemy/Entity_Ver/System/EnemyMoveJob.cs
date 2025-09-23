using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct MoveEnemyJob : IJobEntity
{
    public float deltaTime;

    [NativeDisableParallelForRestriction]
    public NativeArray<int> CellStart;
    [NativeDisableParallelForRestriction]
    public NativeArray<int> CellCount;
    [NativeDisableParallelForRestriction]
    public NativeArray<int> CellIndices;

    public void Execute(ref LocalTransform transform, ref EnemyPositionComponent enemy, in EnemyBaseDataComponent baseData)
    {
        float3 dir = enemy.targetPosition - enemy.position;
        float distance = math.length(dir);
        float speed = baseData.enemy.Value.speed;

        if (distance <= speed * deltaTime)
        {
            enemy.position = enemy.targetPosition;
            transform.Position= enemy.position;
            enemy.cell=enemy.targetCell;
            int insertIndex = CellStart[enemy.cellIndex];
            CellCount[enemy.cellIndex] += 1;
            CellIndices[CellCount[enemy.cellIndex] + insertIndex] = enemy.entityIndex;
        }
        else
        {
            enemy.position += math.normalize(dir) * speed * deltaTime;
            transform.Position= enemy.position;
        }
    }
}

