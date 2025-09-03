using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHp = 100;
    private int hp;

    void OnEnable()
    {
        hp = maxHp;
    }

    public void TakeDamage(int amount)
    {
        hp -= amount;
        if (hp <= 0) Die();
    }

    void Die()
    {
        // 풀링을 전제로 비활성화. 실제 파괴는 X
        gameObject.SetActive(false);
    }
}