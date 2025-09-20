using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// 1. Blob 구조 정의
public struct EnemyBaseData
{
    public float speed;
    public float separationDistance;
    public float seperationPercent;
    public float wallAvoidPercent;
    public float wallAvoidDistance;
    public float randomAngleOffset;

    public float detectionRadius;
    public float attackCooldown;
    public int attackDamage;
    public bool isRanged;
    public float attackRange;

    public int maxHp;
    public int nonAllocBufferSize;
}

// 2. Authoring Component (SO 참조용)
public class EnemyBaseDataAuthoring : MonoBehaviour
{
    public EnemyData enemyData; // 기존 ScriptableObject
}

// 3. Baker: SO → BlobAssetReference 변환
public class EnemyBaseDataBaker : Baker<EnemyBaseDataAuthoring>
{
    public override void Bake(EnemyBaseDataAuthoring authoring)
    {
        var blobRef = EnemyBlobCache.GetBlob(authoring.enemyData);
        AddComponent(new EnemyBaseDataComponent { enemy = blobRef });
    }
}

// 4. ECS Component: BlobAssetReference 보관
public struct EnemyBaseDataComponent : IComponentData
{
    public BlobAssetReference<EnemyBaseData> enemy;
}
