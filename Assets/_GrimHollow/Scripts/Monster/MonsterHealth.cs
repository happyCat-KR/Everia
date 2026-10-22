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
    public GameObject hitVfx;           // 맞을 때 불꽃

    // 경직 게이지: 받은 데미지가 쌓여 이 값을 넘으면 잠깐 멈춤 (매번 경직되면 몬스터가 공격을 못 함)
    public float poise = 20f;
    public float poiseRecovery = 6f;    // 초당 게이지 회복량
    public bool staggerImmune;          // 보스: 경직·넉백 면역
    private float poiseDamage;

    public Color telegraphColor = new Color(1f, 0.55f, 0.15f);    // 공격 예고 때 몸 색
    private bool telegraphing;

    // 생성되는 즉시 준비 (스포너가 만든 그 프레임에 맞아도 에러가 나지 않게)
    void Awake()
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

        playerStats = GameObject.FindWithTag("Player").GetComponent<PlayerStats>();
    }

    void Start()
    {
        hpBar.value = 1f;
        hpBar.gameObject.SetActive(false);  // 맞기 전엔 숨김
    }

    void Update()
    {
        poiseDamage = Mathf.Max(0f, poiseDamage - poiseRecovery * Time.deltaTime);

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
            {
                SetColor(false);
            }
        }
    }

    // heavy: 스킬처럼 강한 공격 (게이지와 상관없이 경직 + 넉백)
    public void TakeDamage(int damage, bool heavy = false)
    {
        if (isDead)
        {
            return;
        }

        currentHp -= damage;
        Sfx.Play(hitSounds, 0.9f);

        // 몬스터마다 키가 달라서 충돌체 크기 기준으로 위치를 잡음
        Bounds body = GetComponent<Collider>().bounds;

        if (hitVfx != null)
        {
            GameObject vfx = Instantiate(hitVfx, body.center, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        // 데미지 숫자 (살짝 랜덤 위치로 겹침 방지)
        Vector3 popupPos = new Vector3(transform.position.x, body.max.y + 0.6f, transform.position.z) + Random.insideUnitSphere * 0.3f;
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
        else if (!staggerImmune)
        {
            // 게이지가 깨지거나 강한 공격이면 움찔 (넉백은 강한 공격만)
            poiseDamage += damage;
            if (heavy || poiseDamage >= poise)
            {
                poiseDamage = 0f;
                animator.SetTrigger("Hit");
                Vector3 pushDirection = transform.position - playerStats.transform.position;
                GetComponent<MonsterController>().Stagger(pushDirection, heavy);
            }
        }
    }

    void SetColor(bool flash)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Color normal = telegraphing ? telegraphColor : originalColors[i];
            renderers[i].material.color = flash ? Color.red : normal;
        }
    }

    // 공격 예고 중에는 몸을 주황색으로 (MonsterController가 켜고 끔)
    public void SetTelegraph(bool on)
    {
        telegraphing = on;
        if (flashTimer <= 0f)
        {
            SetColor(false);
        }
    }

    void Die()
    {
        Debug.Log($"{name} Dead");

        hpBar.gameObject.SetActive(false);  // 죽으면 숨김

        animator.SetTrigger("Die");
        Sfx.Play(deathSounds, 0.9f);

        playerStats.AddExp(expReward);

        LootTable loot = GetComponent<LootTable>();
        if (loot != null)
        {
            loot.Drop();
        }

        // 시체가 길을 막거나 공격에 맞지 않게
        GetComponent<NavMeshAgent>().enabled = false;
        GetComponent<Collider>().enabled = false;

        Destroy(gameObject, destroyDelay);
    }
}