using System.Collections.Generic;
using UnityEngine;

public enum EnemyState { Move, AttackObstacle, AttackAlly }

public class EnemyAI : MonoBehaviour
{
    [Header("Data")]
    public EnemyData data;

    [Header("Layers")]
    public LayerMask enemyMask;     // Enemy 레이어
    public LayerMask obstacleMask;  // Obstacle 레이어
    public LayerMask targetMask;    // Ally / Obstacle 등 공격 대상 레이어

    [Header("State")]
    public EnemyState state = EnemyState.Move;

    // 내부
    private Rigidbody rb;
    private Transform currentTarget;
    private float attackTimer;
    // NonAlloc 버퍼
    private Collider[] detectionBuffer;
    private Collider[] separationBuffer;

    // Batch
    public static readonly List<EnemyAI> All = new List<EnemyAI>();
    private int index = 0;
    private static int batchCursor = 0;     // 0..(batchSize-1)
    public static int batchSize = 50;       // 프레임당 업데이트할 그룹 수
    //Lerp
    private Vector3 targetPosition;
    private Vector3 velocity;
    private Vector3 moveDir;
    void Awake()
    {
        All.Add(this);
        targetPosition=transform.position;
        index = All.IndexOf(this);
        rb= gameObject.GetComponent<Rigidbody>();
        
    }

    void OnDestroy()
    {
        All.Remove(this);
    }

    void Start()
    {
        int n = Mathf.Max(4, data.nonAllocBufferSize);
        detectionBuffer = new Collider[n];
        separationBuffer = new Collider[n];
    }

    void FixedUpdate()
    {
        // 이동
        if (moveDir.sqrMagnitude > 1e-4f)
        {
            Vector3 nextPos = rb.position + moveDir * (data.speed * Time.fixedDeltaTime);
            rb.MovePosition(nextPos);

            // 회전 보정
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.fixedDeltaTime * 10f));
        }
    }

    void Update()
    {
        if (!ShouldUpdateThisFrame()) return;

        switch (state)
        {
            case EnemyState.Move:
                moveDir= TickMove();
                DetectTargetsNonAlloc();
                break;

            case EnemyState.AttackObstacle:
            case EnemyState.AttackAlly:
                TickAttack();
                break;
        }
    }

    bool ShouldUpdateThisFrame()
    {
        // 인덱스 계산 비용 줄이고 싶으면, 스폰 시 자신 고유 index 저장해도 됨
        if (index < 0) return false;
        return (index % batchSize) == batchCursor;
    }

    public static void NextBatch()
    {
        batchCursor = (batchCursor + 1) % batchSize;
    }

    Vector3 TickMove()
    {
        Vector3 flowDir = FlowField.Instance.GetFlowDirection(transform.position);
        // 목적지 셀에선 flowDir이 0일 수 있으므로, 역추적 대신 멈춤
        if (flowDir.sqrMagnitude < 1e-4f)
            flowDir = (FlowField.Instance.target.position - transform.position).normalized;
        flowDir.Normalize();
        // 이웃 분리(Separation) - 적끼리 충돌 대신 가벼운 보정
        Vector3 separation = Vector3.zero;
        int neighbors = Physics.OverlapSphereNonAlloc(transform.position, data.separationDistance,
                                                      separationBuffer, enemyMask);
        for (int i = 0; i < neighbors; i++)
        {
            var c = separationBuffer[i];
            if (!c || c.transform == transform) continue;
            Vector3 toMe = transform.position - c.transform.position;
            float d = toMe.magnitude;
            if (d > 1e-3f) separation += toMe / (d * d); // 가까울수록 강하게 밀어냄
        }
        separation.y = 0;
        separation.Normalize();
        Vector3 moveDir = (flowDir + separation*data.seperationPercent).normalized;
        float angleOffset = Random.Range(-data.randomAngleOffset, data.randomAngleOffset);
        return Quaternion.Euler(0, angleOffset, 0) * moveDir;
    }

    void DetectTargetsNonAlloc()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, data.detectionRadius,
                                                  detectionBuffer, targetMask);
        if (count <= 0) return;
        
        // 가장 가까운 타겟 선택(간단)
        float best = float.MaxValue;
        Transform bestT = null;

        for (int i = 0; i < count; i++)
        {
            var col = detectionBuffer[i];
            if (!col) continue;
            float d = (col.transform.position - transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestT = col.transform;
            }
        }

        currentTarget = bestT;
        if (!currentTarget) return;

        // 어떤 레이어인지로 상태 결정
        int layer = currentTarget.gameObject.layer;
        int obstacleLayer = LayerMaskToLayerIndex(obstacleMask);

        state = (layer == obstacleLayer) ? EnemyState.AttackObstacle : EnemyState.AttackAlly;
    }

    void TickAttack()
    {
        if (!currentTarget)
        {
            state = EnemyState.Move;
            return;
        }

        // 사거리 체크 (근/원거리)
        float required = data.isRanged ? data.attackRange : data.detectionRadius * 0.9f;
        float dist = Vector3.Distance(transform.position, currentTarget.position);

        // 근접이면 접근(Flow 보정) / 원거리면 정지 후 시선만
        if (!data.isRanged && dist > required * 0.9f)
        {
            // 아주 간단히: 목표를 바라보고 한 발짝(과도한 path는 Flow에 맡김)
            Vector3 dir = (currentTarget.position - transform.position).normalized;
            //controller.SimpleMove(dir * (data.speed * 0.7f));
        }
        else
        {
            //controller.SimpleMove(Vector3.zero);
        }

        // 회전 보정
        Vector3 lookDir = (currentTarget.position - transform.position);
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 1e-3f)
        {
            Quaternion r = Quaternion.LookRotation(lookDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, r, Time.deltaTime * 10f);
        }

        // 공격
        attackTimer += Time.deltaTime;
        if (attackTimer >= data.attackCooldown)
        {
            attackTimer = 0f;
            var hp = currentTarget.GetComponent<Health>();
            if (hp) hp.TakeDamage(data.attackDamage);
        }

        // 타겟이 멀어지면 Move 복귀
        if (dist > data.detectionRadius * 1.2f || !currentTarget.gameObject.activeSelf)
        {
            currentTarget = null;
            state = EnemyState.Move;
        }
    }

    // obstacleMask에서 단일 레이어 인덱스를 추정(단일 레이어 사용 가정)
    int LayerMaskToLayerIndex(LayerMask mask)
    {
        int m = mask.value;
        for (int i = 0; i < 32; i++)
            if ((m & (1 << i)) != 0) return i;
        return -1;
    }
}
