using Unity.Entities;
using Unity.Mathematics;

public struct SoldierComponent : IComponentData
{
    public int id;
    public float3 position;
    public float atkRange;
    public Entity target;
}