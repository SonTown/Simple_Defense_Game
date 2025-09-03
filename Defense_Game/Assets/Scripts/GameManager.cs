using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Batch")]
    public int batchSize = 50; // 프레임당 업데이트 그룹 수

    [Header("Optional Spawn Test")]
    public EnemyAI enemyPrefab;
    public EnemyData enemyData;
    public Transform[] spawnPoints;
    public int spawnCount = 1000;

    void Start()
    {
        // EnemyAI에서 사용할 배치 크기 설정
        EnemyAI.batchSize = Mathf.Max(1, batchSize);

        // (선택) 테스트 스폰
        if (enemyPrefab && enemyData && spawnPoints != null && spawnPoints.Length > 0)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                var p = spawnPoints[Random.Range(0, spawnPoints.Length)];
                Vector3 randomPos = new Vector3(Random.Range(1,50), 0, Random.Range(70,90));
                var e = Instantiate(enemyPrefab, randomPos, Quaternion.identity);
                e.data = enemyData;
                // 레이어/마스크는 프리팹에서 미리 세팅하는 걸 권장
            }
        }
    }

    void Update()
    {
        // 프레임마다 다음 배치로 (수백~수천 적의 AI를 분산)
        EnemyAI.NextBatch();

        // 동적으로 맵이 바뀌면 FlowField 재생성 호출 가능 (비용 큼 → 드물게)
        // if (Input.GetKeyDown(KeyCode.F)) FlowField.Instance.RebuildAll();
    }
}