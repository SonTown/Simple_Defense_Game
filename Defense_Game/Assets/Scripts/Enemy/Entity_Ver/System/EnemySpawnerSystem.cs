using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class EnemySpawnerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<EnemySpawnerComponent>();
    }

    protected override void OnStartRunning()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var spawner = SystemAPI.GetSingleton<EnemySpawnerComponent>();
        var gridData = SystemAPI.GetSingleton<SpatialGridData>();
        for (int i = 0; i < 99999; i++)
        {
            var e = em.Instantiate(spawner.Prefab);
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)i);
            LocalTransform t =
                LocalTransform.FromPosition(new float3(random.NextFloat(1, 50), 0, random.NextFloat(70, 90)));
            em.SetComponentData(e,t);
            em.SetComponentData(e, new EnemyPositionComponent
            {
                position = t.Position,
                cell = FlowFieldUtils.GetGrid(gridData,t.Position)
            });

        }
    }

    protected override void OnUpdate() { }
}