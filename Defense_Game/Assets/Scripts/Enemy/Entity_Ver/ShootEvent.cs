using Unity.Entities;
using Unity.Mathematics;

public struct ShootEvent : IBufferElementData
{
    public int shooterId;
    public float damage;
    public float3 origin;
    public float3 direction;
    public float range;
    public bool isExplosive;
    public Entity target;   // <- 유탄처럼 목표가 있는 경우
}