using UnityEngine;

public class Enemy : MonoBehaviour
{
    public bool isAlive = true;
    public float progress; // 경로 진행률 (0~1)
    [HideInInspector] public Vector2Int currentCell;

    void Start()
    {
        EnemyManager.Instance.RegisterEnemy(this);
    }

    void Update()
    {
        // Cell 갱신
        if (isAlive)
        {
            EnemyManager.Instance.UpdateEnemyCell(this);
            progress = 1 - transform.position.z / 100f;
        }
    }

    public void TakeDamage(int dmg)
    {
        // 체력 처리
        isAlive = false;
        gameObject.SetActive(false);
    }
}