using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public enum EnemyState { Move, AttackObstacle, AttackAlly }
public class EnemyPositionAuthoring : MonoBehaviour
{
    public float3 position;
    public float3 velocity;
    public EnemyState enemyState;
}

// 3. Baker: SO → BlobAssetReference 변환
public class EnemyPositionBaker : Baker<EnemyPositionAuthoring>
{
    public override void Bake(EnemyPositionAuthoring authoring)
    {
        AddComponent(new EnemyPositionComponent() { position = authoring.position, velocity = authoring.velocity,enemyState = authoring.enemyState});
    }
}

// 4. ECS Component: BlobAssetReference 보관
public struct EnemyPositionComponent : IComponentData
{
    public float3 position;
    public float3 velocity;
    public int2 cell;
    public EnemyState enemyState;
}
