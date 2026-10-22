using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class MonsterHealth : MonoBehaviour
{
    public int maxHp = 30;
    public int currentHp;
    public bool isDead;
    public float destroyDelay = 3f;     // 죽는 모션 후 사라지기까지

    private Animator animator;
    private Renderer[] renderers;
    private Color[] originalColors;
    private float flashTimer;

    public Slider hpBar;

    public DamagePopup damagePopupPrefab;

    public int expReward = 25;
    private PlayerStats playerStats;

    public AudioClip[] hitSounds;       // 맞을 때
    public AudioClip[] deathSounds;     // 죽을 때

    void Start()
    {
        currentHp = maxHp;
        animator = GetComponentInChildren<Animator>();

        // 해골 모델은 여러 부위(Renderer)로 나뉘어 있어서 전부 가져옴
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }

        hpBar.value = 1f;
        hpBar.gameObject.SetActive(false);  // 맞기 전엔 숨김

        playerStats = GameObject.FindWithTag("Player").GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
            {
                SetColor(false);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        currentHp -= damage;
        Sfx.Play(hitSounds, 0.9f);

        // 데미지 숫자 (살짝 랜덤 위치로 겹침 방지)
        Vector3 popupPos = transform.position + Vector3.up * 2.7f + Random.insideUnitSphere * 0.3f;
        DamagePopup popup = Instantiate(damagePopupPrefab, popupPos, Quaternion.identity);
        popup.Setup(damage);

        Debug.Log($"{name} HP: {currentHp}/{maxHp}");

        hpBar.gameObject.SetActive(true);
        hpBar.value = (float)currentHp / maxHp;

        GetComponent<MonsterController>().Aggro();

        SetColor(true);
        flashTimer = 0.1f;

        if (currentHp <= 0)
        {
            currentHp = 0;
            isDead = true;
            Die();
        }
         else
        {
            // 잡몹: 맞을 때마다 움찔 + 밀려남
            animator.SetTrigger("Hit");
            Vector3 pushDirection = transform.position - playerStats.transform.position;
            GetComponent<MonsterController>().Stagger(pushDirection);
        }
    }

    void SetColor(bool flash)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = flash ? Color.red : originalColors[i];
        }
    }

    void Die()
    {
        Debug.Log($"{name} Dead");

        hpBar.gameObject.SetActive(false);  // 죽으면 숨김

        animator.SetTrigger("Die");
        Sfx.Play(deathSounds, 0.9f);

        playerStats.AddExp(expReward);

        // 시체가 길을 막거나 공격에 맞지 않게
        GetComponent<NavMeshAgent>().enabled = false;
        GetComponent<Collider>().enabled = false;

        Destroy(gameObject, destroyDelay);
    }
}