using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public enum EnemyState { Move, AttackObstacle, AttackAlly }
public class EnemyPositionAuthoring : MonoBehaviour
{
    public float3 position;
    public EnemyState enemyState;
}

// 3. Baker: SO → BlobAssetReference 변환
public class EnemyPositionBaker : Baker<EnemyPositionAuthoring>
{
    public override void Bake(EnemyPositionAuthoring authoring)
    {
        AddComponent(new EnemyPositionComponent() { position = authoring.position,enemyState = authoring.enemyState});
    }
}

public struct EnemyPositionComponent : IComponentData
{
    public int entityIndex;
    public float3 position;
    public float3 targetPosition;
    public int3 cell;
    public int3 targetCell;
    public int cellIndex;
    public EnemyState enemyState;

    // CellSize와 시드만 있으면 랜덤 위치 계산 가능
    public void SetTargetCell(int3 pos, float cellSize,int cellIndex, uint seed)
    {
        targetCell = pos;
        this.cellIndex = cellIndex;
        var random = new Unity.Mathematics.Random(seed);

        float3 cellMin = new float3(
            targetCell.x * cellSize,
            targetCell.y * cellSize,
            targetCell.z * cellSize
        );

        float3 cellMax = cellMin + cellSize;

        targetPosition = new float3(
            random.NextFloat(cellMin.x, cellMax.x),
            random.NextFloat(cellMin.z, cellMax.z),
            random.NextFloat(cellMin.y, cellMax.y)
        );
    }
}

