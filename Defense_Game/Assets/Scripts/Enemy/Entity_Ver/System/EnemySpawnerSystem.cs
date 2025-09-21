using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class EnemySpawnerSystem : SystemBase
{
    /*
    public NativeArray<Entity> Enemies;
    protected override void OnCreate()
    {
        RequireForUpdate<EnemySpawnerComponent>();
    }

    protected override void OnStartRunning()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var spawner = SystemAPI.GetSingleton<EnemySpawnerComponent>();
        var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridData>();
        var gridData = SystemAPI.GetComponent<SpatialGridData>(gridEntity);
        var spatilPartion = SystemAPI.GetSingleton<SpatialPartition>();
        int enemyCount = 99999;

        // NativeArray 생성
        Enemies = new NativeArray<Entity>(enemyCount, Allocator.Persistent);
        int index = 0;
        for (int x = 0; x < 25; x++)
        {
            for (int y = 40; y < 50; y++)
            {
                for (int z = 0; z < 1; z++)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        var e = em.Instantiate(spawner.Prefab);
                        // 순서대로 NativeArray에 저장
                        Enemies[index] = e;
                        var random = Random.CreateFromIndex((uint)i);
                        LocalTransform t = LocalTransform.FromPosition(
                            new float3(random.NextFloat(x*gridData.GridSizeX, (x+1)*gridData.GridSizeX),random.NextFloat(y*gridData.GridSizeY, (y+1)*gridData.GridSizeY),random.NextFloat(z*gridData.GridSizeZ, (z+1)*gridData.GridSizeZ))
                        );
                        em.SetComponentData(e, t);

                        em.SetComponentData(e, new EnemyPositionComponent
                        {
                            entityIndex = i,
                            position = t.Position,
                            cell = new int3(x,y,z),
                            targetPosition=t.Position,
                            targetCell = new int3(x,y,z)
                        });
                        index++;
                    }
                }
            }
        }
        // 변경된 NativeArray를 다시 SpatialGridData에 반영
        em.SetComponentData(gridEntity, gridData);
    }
    protected override void OnDestroy()
    {
        // NativeArray Dispose
        if (SystemAPI.HasSingleton<SpatialGridData>())
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridData>();
            var gridData = em.GetComponentData<SpatialGridData>(gridEntity);
        }
    }
*/
    protected override void OnUpdate()
    {
    }
}