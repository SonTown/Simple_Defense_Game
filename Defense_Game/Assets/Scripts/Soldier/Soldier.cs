using System;
using UnityEngine;

public class Soldier : MonoBehaviour
{
    [Header("Data")] public SoldierData data;
    [Header("Weapon")] public Weapon weapon;
    
    private Enemy target;
    public LayerMask hitMask;

    public void Awake()
    {
        weapon.Initialize(this);
    }

    void Update()
    { 
        if (target == null || !target.isAlive || Vector3.Distance(transform.position, target.transform.position) > weapon.weaponData.AttackRange)
        {
            target = EnemyManager.Instance.GetBestTarget(transform.position, weapon.weaponData.AttackRange);
        }

        if (target != null)
        {
            weapon.TryFire();
        }
    }

    public void TryReload()
    {
        weapon.TryReload();
    }

    public void TrySupply()
    {
        weapon.TrySupply();
    }
    public void Fire()
    {
        if (target != null)
        {
            ShootAt(target);
        }
    }
    void ShootAt(Enemy e)
    {
        Vector3 start = transform.position;
        Vector3 dir = (e.transform.position - start).normalized;

        if (Physics.Raycast(start, dir, out RaycastHit hit, weapon.weaponData.AttackRange, hitMask))
        {
            // 총알 궤적
            if (hit.collider.tag == "Enemy")
            {
                ObjectPoolManager.Instance.SpawnBulletTrail(start, hit.point);

                // Enemy에 맞았으면 데미지
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null && weapon.leftAmmo > 0)
                {
                    enemy.TakeDamage(10);
                    weapon.ChangeState(FireState.Instance);
                    this.gameObject.transform.LookAt(enemy.transform.position);
                }
            }
            else
            {
                target = null;
            }
        }
    }
}