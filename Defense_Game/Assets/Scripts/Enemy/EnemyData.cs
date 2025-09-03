using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "RTS/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Move")]
    public float speed = 3f;
    public float separationDistance = 0.8f;
    public float seperationPercent = 0.2f;// 적-적 최소 간격
    public float wallAvoidDistance = 0.8f;    // 벽 전방 회피 레이 길이
    public float randomAngleOffset = 30f;
    
    [Header("Perception/Combat")]
    public float detectionRadius = 2f;        // 근접/공격 감지 반경
    public float attackCooldown = 1f;
    public int attackDamage = 10;
    public bool isRanged = false;
    public float attackRange = 4f;            // 원거리일 때 사거리

    [Header("HP")]
    public int maxHp = 100;

    [Header("Batch/NonAlloc")]
    public int nonAllocBufferSize = 12;       // 감지/분리 배열 길이
}