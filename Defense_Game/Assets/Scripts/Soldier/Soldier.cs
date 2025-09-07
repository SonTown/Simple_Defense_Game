using System;
using System.Collections.Generic;
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
            if (weapon.weaponData.isExplosive == false)
            {
                ShootAt(target);
            }
            else
            {
                ShootExplosiveAt(target);
            }
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
    void ShootExplosiveAt(Enemy targetEnemy)
    {
        Vector3 start = transform.position;
        Vector3 dir = (targetEnemy.transform.position - start).normalized;

        if (Physics.Raycast(start, dir, out RaycastHit hit, weapon.weaponData.AttackRange, hitMask))
        {
            // 총알 궤적
            ObjectPoolManager.Instance.SpawnBulletTrail(start, hit.point);

            // 폭발 처리
            Explode(hit.point);
        }
    }

    void Explode(Vector3 explosionPoint)
    {
        float explosionRadius = 5f;  // 폭발 반경
        int explosionDamage = 20;    // 기본 데미지

        // EnemyManager 활용 → 반경 내 적 리스트 가져오기
        List<Enemy> enemies = EnemyManager.Instance.GetEnemiesInRange(explosionPoint, explosionRadius);

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(enemy.transform.position, explosionPoint);
            float damagePercent = Mathf.Clamp01(1 - (dist / explosionRadius));
            int finalDamage = Mathf.RoundToInt(explosionDamage * damagePercent);

            enemy.TakeDamage(10);
        }
        weapon.ChangeState(FireState.Instance);
        ObjectPoolManager.Instance.SpawnImpactFx(explosionPoint, explosionRadius);
    }
}