using UnityEngine;
using UnityEngine.Pool;

public class BulletTrail : MonoBehaviour
{
    public LineRenderer line;
    public float duration = 0.1f;
    private ObjectPool<GameObject> pool;
    public void Init(Vector3 start, Vector3 end,ObjectPool<GameObject> pool)
    {
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        CancelInvoke();
        this.pool = pool;
        Invoke(nameof(Despawn), duration);
    }

    void Despawn()
    {
        // UnityEngine.Pool.ObjectPool에서 반환
        pool.Release(gameObject);
        gameObject.SetActive(false);
    }
}