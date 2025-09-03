using UnityEngine;

public class Soldier : MonoBehaviour
{
    public float attackRange = 20f;
    public float attackCooldown = 0.3f;
    private float lastAttackTime;
    private Enemy target;
    public LayerMask hitMask;

    void Update()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            if (target == null || !target.isAlive || Vector3.Distance(transform.position, target.transform.position) > attackRange)
            {
                target = EnemyManager.Instance.GetBestTarget(transform.position, attackRange);
            }

            if (target != null)
            {
                ShootAt(target);
                lastAttackTime = Time.time;
            }
        }
    }

    void ShootAt(Enemy e)
    {
        Vector3 start = transform.position;
        Vector3 dir = (e.transform.position - start).normalized;

        if (Physics.Raycast(start, dir, out RaycastHit hit, attackRange, hitMask))
        {
            // 총알 궤적
            if (hit.collider.tag == "Enemy")
            {
                ObjectPoolManager.Instance.SpawnBulletTrail(start, hit.point);

                // Enemy에 맞았으면 데미지
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(10);
                }
            }
            else
            {
                target = null;
            }
        }
    }
}