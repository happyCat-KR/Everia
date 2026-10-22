using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int maxHp = 100;
    public int currentHp;
    public bool isDead = false;

    private Animator animator;

    public int level = 1;
    public int currentExp;
    public int baseExpToLevel = 100;    // 필요 경험치 = 100 × 레벨 (Lv1→2: 100, Lv2→3: 200 ...)
    public int hpPerLevel = 10;         // 레벨업마다 최대 HP 증가량

    public int ExpToNextLevel => baseExpToLevel * level;

    void Start()
    {
        currentHp = maxHp;
        animator = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(int damage)
    {
        // 죽었거나 대시 중(무적)이면 안 맞음
        if (isDead || GetComponent<PlayerController>().IsDashing)
        {
            return;
        }

        currentHp -= damage;
        Debug.Log($"Player HP: {currentHp}/{maxHp}");

        if (currentHp <= 0)
        {
            currentHp = 0;
            Die();
        }
        else
        {
            // 공격 중이면 움찔하지 않음 (데미지는 그대로 받음)
            if (!GetComponent<PlayerController>().IsAttacking)
            {
                animator.SetTrigger("Hit");
            }
        }
    }

    public void AddExp(int amount)
    {
        if (isDead)
        {
            return;
        }

        currentExp += amount;
        Debug.Log($"EXP +{amount} ({currentExp}/{ExpToNextLevel})");

        // 한 번에 많이 받으면 여러 번 레벨업할 수 있어서 while
        while (currentExp >= ExpToNextLevel)
        {
            currentExp -= ExpToNextLevel;
            LevelUp();
        }
    }

    void LevelUp()
    {
        level++;
        maxHp += hpPerLevel;
        currentHp = maxHp;          // 레벨업 시 풀피 (디아블로 방식)
        Debug.Log($"Level Up! Lv.{level}");
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Player Dead");

        animator.SetLayerWeight(1, 0f);            // 상체 레이어 끄기 → 온몸으로 쓰러지게
        animator.SetFloat("MoveX", 0f);
        animator.SetFloat("MoveZ", 0f);
        animator.SetTrigger("Die");

        GetComponent<PlayerController>().enabled = false;   // 이동/회전/공격 정지
    }
}