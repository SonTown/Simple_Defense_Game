using Unity.Entities;
using UnityEngine;
public class AllyTagAuthoring : MonoBehaviour
{
    
}
public class AllyTagBaker : Baker<AllyTagAuthoring>
{
    public override void Bake(AllyTagAuthoring authoring)
    {
        AddComponent(new AllyTag());
    }
}

public struct AllyTag : IComponentData
{
    
}