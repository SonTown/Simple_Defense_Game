using Unity.Entities;
using UnityEngine;

public class IsAliveAuthoring : MonoBehaviour
{
    public class Baker : Baker<IsAliveAuthoring>
    {
        public override void Bake(IsAliveAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new IsAlive());
            SetComponentEnabled<IsAlive>(entity, true);
        }
    }
}
public struct IsAlive : IComponentData, IEnableableComponent
{
    
}
