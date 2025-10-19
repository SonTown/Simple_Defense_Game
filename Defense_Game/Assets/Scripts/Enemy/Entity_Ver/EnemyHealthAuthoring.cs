using Unity.Entities;
using UnityEngine;

public class EnemyHealthAuthoring : MonoBehaviour
{
    public int Health;
}

// 3. Baker: SO → BlobAssetReference 변환
public class EnemyHealthBaker : Baker<EnemyHealthAuthoring>
{
    public override void Bake(EnemyHealthAuthoring authoring)
    {
        AddComponent(new EnemyHealthComponent() { Health = authoring.Health, isDead = false});
    }
}

public struct EnemyHealthComponent : IComponentData
{
    public int Health;
    public bool isDead;
}