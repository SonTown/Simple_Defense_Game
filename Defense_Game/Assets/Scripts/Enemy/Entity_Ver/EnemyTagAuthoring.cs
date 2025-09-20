using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
public class EnemyTagAuthoring : MonoBehaviour
{
    
}
public class EnemyTagBaker : Baker<EnemyTagAuthoring>
{
    public override void Bake(EnemyTagAuthoring authoring)
    {
        AddComponent(new EnemyTag());
    }
}

public struct EnemyTag : IComponentData
{
    
}