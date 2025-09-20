using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

public static class EnemyBlobCache
{
    // 타입별 Blob 저장
    private static Dictionary<EnemyData, BlobAssetReference<EnemyBaseData>> cache = new();

    public static BlobAssetReference<EnemyBaseData> GetBlob(EnemyData data)
    {
        if (!cache.TryGetValue(data, out var blob))
        {
            // 없으면 새로 생성
            var builder = new BlobBuilder(Allocator.Temp);
            ref EnemyBaseData root = ref builder.ConstructRoot<EnemyBaseData>();
            
            // 데이터 복사
            root.speed = data.speed;
            root.separationDistance = data.separationDistance;
            root.seperationPercent = data.seperationPercent;
            root.wallAvoidPercent = data.wallAvoidPercent;
            root.wallAvoidDistance = data.wallAvoidDistance;
            root.randomAngleOffset = data.randomAngleOffset;
            
            root.detectionRadius = data.detectionRadius;
            root.attackCooldown = data.attackCooldown;
            root.attackDamage = data.attackDamage;
            root.isRanged = data.isRanged;
            root.attackRange = data.attackRange;
            
            root.maxHp = data.maxHp;
            root.nonAllocBufferSize = data.nonAllocBufferSize;

            blob = builder.CreateBlobAssetReference<EnemyBaseData>(Allocator.Persistent);
            builder.Dispose();

            cache[data] = blob; // 캐시에 저장
        }

        return blob;
    }
}