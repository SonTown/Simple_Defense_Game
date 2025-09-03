using UnityEngine;
using UnityEngine.Pool;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance;

    public GameObject bulletTrailPrefab;
    public GameObject impactFxPrefab;

    private ObjectPool<GameObject> bulletTrailPool;
    private ObjectPool<GameObject> impactFxPool;

    void Awake()
    {
        Instance = this;

        bulletTrailPool = new ObjectPool<GameObject>(
            () => Instantiate(bulletTrailPrefab),
            go => go.SetActive(true),
            go => go.SetActive(false),
            go => Destroy(go), 
            false, 20, 100
        );

        impactFxPool = new ObjectPool<GameObject>(
            () => Instantiate(impactFxPrefab),
            go => go.SetActive(true),
            go => go.SetActive(false),
            go => Destroy(go),
            false, 20, 50
        );
    }

    public GameObject SpawnBulletTrail(Vector3 start, Vector3 end)
    {
        var go = bulletTrailPool.Get();
        go.GetComponent<BulletTrail>().Init(start, end, bulletTrailPool);
        return go;
    }

    public GameObject SpawnImpactFx(Vector3 pos, Quaternion rot)
    {
        var go = impactFxPool.Get();
        //go.GetComponent<ImpactFx>().Play(pos, rot);
        return go;
    }
}