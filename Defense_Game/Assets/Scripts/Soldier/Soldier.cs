using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class Soldier : MonoBehaviour
{
    [Header("Data")] public SoldierData data;
    [Header("Weapon")] public Weapon weapon;
    private Entity soldierEntity;
    private EntityManager entityManager;
    public LayerMask hitMask;
    private Entity targetEntity;
    public void Awake()
    {
        weapon.Initialize(this);
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        soldierEntity = entityManager.CreateEntity(typeof(SoldierComponent));
        entityManager.SetComponentData(soldierEntity, new SoldierComponent
        {
            id=GetInstanceID(),
            atkRange = weapon.weaponData.AttackRange,
            position = transform.position,
        });
        entityManager.AddBuffer<ShootEvent>(soldierEntity);
    }

    void Update()
    { 
        var comp = entityManager.GetComponentData<SoldierComponent>(soldierEntity);
        comp.position = transform.position;
        entityManager.SetComponentData(soldierEntity, comp);
        targetEntity=comp.target;
        if (weapon.isIdle)
        {
            comp.isReady = true;
        }
        else
        {
            comp.isReady = false;
        }
        if (targetEntity != Entity.Null)
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
        if (targetEntity != Entity.Null)
        {
           ShootAt(targetEntity);
        }
    }
    void ShootAt(Entity e)
    {
        if (!entityManager.HasComponent<EnemyPositionComponent>(e))
            return;

        var enemyPos = entityManager.GetComponentData<EnemyPositionComponent>(e);
        Vector3 start = transform.position;
        Vector3 targetPos = enemyPos.position;
        Vector3 dir = (targetPos - start);
        float dist = dir.magnitude;
        dir.Normalize();

        if (dist <= weapon.weaponData.AttackRange && weapon.leftAmmo > 0)
        {
            ObjectPoolManager.Instance.SpawnBulletTrail(start, targetPos);
            // SoldierEntity에 달린 DamageBuffer 가져오기
            var buffer = entityManager.GetBuffer<ShootEvent>(soldierEntity);
            buffer.Add(new ShootEvent()
            {
                target = e,
                damage = weapon.weaponData.Damage,
                range = weapon.weaponData.explodeRange,
            });
            weapon.ChangeState(FireState.Instance);
            transform.LookAt(enemyPos.position);
        }
        else
        {
            targetEntity = Entity.Null;
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