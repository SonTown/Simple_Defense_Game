using Unity.Entities;
using UnityEngine;

public class EnemySpawnerAuthoring : MonoBehaviour
{
    public GameObject EnemyPrefab; // 인스펙터에서 드래그
}

public class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
{
    public override void Bake(EnemySpawnerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);

        // GameObject prefab → Entity prefab으로 변환
        var prefabEntity = GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic);

        AddComponent(entity, new EnemySpawnerComponent
        {
            Prefab = prefabEntity
        });
    }
}

public struct EnemySpawnerComponent : IComponentData
{
    public Entity Prefab;
}