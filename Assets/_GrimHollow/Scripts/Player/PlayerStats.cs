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

    public AudioClip[] hurtSounds;      // 몬스터에게 맞을 때

    // 스태미나: 대시·전사 공격에 사용 (막 구르거나 연타하는 걸 막는 자원)
    public int maxStamina = 100;
    public float currentStamina;
    public float staminaRegen = 20f;        // 초당 회복량
    public float staminaRegenDelay = 0.6f;  // 쓴 뒤 이만큼 지나야 회복 시작
    private float staminaRegenTimer;

    // 마나: 마법사 공격·스킬에 사용 (연사를 막는 자원)
    public int maxMp = 100;
    public float currentMp;
    public float mpRegen = 6f;          // 초당 회복량

    void Start()
    {
        currentHp = maxHp;
        currentMp = maxMp;
        currentStamina = maxStamina;
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (isDead)
        {
            return;
        }

        currentMp = Mathf.Min(maxMp, currentMp + mpRegen * Time.deltaTime);

        if (staminaRegenTimer > 0f)
        {
            staminaRegenTimer -= Time.deltaTime;
        }
        else
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegen * Time.deltaTime);
        }
    }

    // 스태미나가 충분하면 쓰고 true (쓴 직후엔 잠깐 회복이 멈춤)
    public bool SpendStamina(int cost)
    {
        if (currentStamina < cost)
        {
            return false;
        }

        currentStamina -= cost;
        staminaRegenTimer = staminaRegenDelay;
        return true;
    }

    // 마나가 충분하면 쓰고 true
    public bool SpendMp(int cost)
    {
        if (currentMp < cost)
        {
            return false;
        }

        currentMp -= cost;
        return true;
    }

    public void TakeDamage(int damage)
    {
        // 죽었거나 대시 중(무적)이면 안 맞음
        if (isDead || GetComponent<PlayerController>().IsDashing)
        {
            return;
        }

        currentHp -= damage;
        Sfx.Play(hurtSounds, 0.8f);
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

    public void Heal(int amount)
    {
        if (isDead)
        {
            return;
        }

        currentHp = Mathf.Min(currentHp + amount, maxHp);
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