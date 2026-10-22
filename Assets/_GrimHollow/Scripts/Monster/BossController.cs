using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 보스: 1페이즈 패턴 3개(내려찍기, 횡베기, 돌진) + HP 50%에서 2페이즈(포효, 연속 내려찍기, 포자 폭발).
// 공격 전에 바닥에 범위가 차오르는 경고가 뜨고, 큰 패턴이 끝나면 기진맥진해서 빈틈이 생김 (이때만 데미지가 늘어남)
public class BossController : MonoBehaviour
{
    enum Pattern { Slam, Sweep, Charge, Combo, Spores }
    enum State { Idle, Chase, WindUp, Strike, Charging, Recover, Roar, Cast }

    public string bossName = "Mushroom King";
    public float wakeRange = 14f;           // 이 안에 들어오면 깨어남
    public float walkSpeed = 3.2f;
    public float betweenAttacks = 0.4f;     // 패턴 사이에 숨 돌리는 시간

    [Header("Slam (내려찍기)")]
    public int slamDamage = 34;
    public float slamRadius = 3.6f;
    public float slamForward = 2.4f;        // 보스 앞 이만큼 떨어진 곳이 중심
    public float slamWindUp = 0.9f;
    public float slamRecover = 1.8f;        // 빈틈

    [Header("Sweep (횡베기)")]
    public int sweepDamage = 24;
    public float sweepRadius = 5f;
    public float sweepAngle = 130f;
    public float sweepWindUp = 0.65f;
    public float sweepRecover = 0.8f;

    [Header("Charge (돌진)")]
    public int chargeDamage = 34;
    public float chargeWidth = 2.4f;
    public float chargeLength = 13f;
    public float chargeWindUp = 0.85f;
    public float chargeSpeed = 15f;
    public float chargeRecover = 2.0f;      // 빈틈
    public float chargeWallRecover = 3.2f;  // 벽에 박으면 더 긴 빈틈

    [Header("Phase 2 (HP 50%)")]
    public float phase2Threshold = 0.5f;
    public float timeScale2 = 0.8f;         // 2페이즈에서 경고·빈틈 시간이 이 배율로 짧아짐
    public float moveBoost2 = 1.25f;
    public float roarTime = 2.2f;
    public float shockwaveRadius = 8f;
    public float shockwavePush = 14f;
    public int shockwaveDamage = 10;
    public float growScale = 1.15f;
    public Color rageTint = new Color(0.9f, 0.1f, 0.05f);

    [Header("Combo (연속 내려찍기)")]
    public int comboSlams = 3;
    public float comboWindUp = 0.65f;
    public float comboStepSpeed = 4.5f;     // 경고 중 플레이어 쪽으로 다가오는 속도

    [Header("Spores (포자 폭발)")]
    public int sporeCount = 6;
    public float sporeRadius = 2.3f;
    public float sporeInterval = 0.4f;
    public float sporeFill = 1.0f;
    public int sporeDamage = 24;
    public float sporeJitter = 2.5f;        // 플레이어 위치에서 이만큼 어긋나게

    public float rotateSpeed = 220f;        // 경고 중 플레이어 쪽으로 도는 속도
    [Range(0f, 1f)] public float lockAt = 0.55f;   // 경고 시간의 이 지점부터 방향 고정 (이후엔 옆으로 피하면 됨)

    [Header("Effects")]
    public Material indicatorOutline;
    public Material indicatorFill;
    public GameObject slamVfx;
    public GameObject sporeVfx;
    public GameObject shockwaveVfx;
    public AudioClip[] slamSounds;
    public AudioClip[] swingSounds;
    public AudioClip[] roarSounds;

    public bool IsAwake => state != State.Idle;
    public bool InPhase2 => phase2;

    private NavMeshAgent agent;
    private Animator animator;
    private MonsterHealth health;
    private PlayerStats playerStats;
    private PlayerController playerController;
    private Transform target;

    private State state = State.Idle;
    private Pattern pattern;
    private Pattern lastPattern = (Pattern)(-1);
    private float timer;
    private float duration;
    private float decisionTimer;
    private bool resolved;
    private GroundIndicator indicator;
    private Vector3 slamCenter;
    private Vector3 chargeDirection;
    private float traveled;
    private bool chargeHit;

    private bool phase2;
    private bool shockwaveDone;
    private Vector3 baseScale;
    private int comboIndex;
    private int sporesSpawned;

    // 2페이즈에서는 경고·빈틈이 짧아짐
    float Tempo => phase2 ? timeScale2 : 1f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        health = GetComponent<MonsterHealth>();
        target = GameObject.FindWithTag("Player").transform;
        playerStats = target.GetComponent<PlayerStats>();
        playerController = target.GetComponent<PlayerController>();
        agent.speed = walkSpeed;
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (health.isDead)
        {
            ClearIndicator();
            return;
        }

        if (playerStats.isDead)
        {
            agent.ResetPath();
            animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            ClearIndicator();
            return;
        }

        // HP가 절반 아래로 내려가면 (공격 중이 아닐 때) 포효하며 2페이즈
        if (!phase2 && health.currentHp <= health.maxHp * phase2Threshold
            && (state == State.Chase || state == State.Recover || state == State.Idle))
        {
            BeginRoar();
        }

        switch (state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Chase: UpdateChase(); break;
            case State.WindUp: UpdateWindUp(); break;
            case State.Strike: UpdateStrike(); break;
            case State.Charging: UpdateCharging(); break;
            case State.Recover: UpdateRecover(); break;
            case State.Roar: UpdateRoar(); break;
            case State.Cast: UpdateCast(); break;
        }
    }

    float FlatDistance(Vector3 point)
    {
        Vector3 d = point - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    void FaceTarget(float maxDegrees)
    {
        Vector3 d = target.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), maxDegrees);
        }
    }

    // ───── 대기 / 추적 ─────
    void UpdateIdle()
    {
        animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
        // 가까이 오거나 맞으면 깨어남
        if (FlatDistance(target.position) <= wakeRange || health.currentHp < health.maxHp)
        {
            state = State.Chase;
            decisionTimer = 0.8f;
        }
    }

    void UpdateChase()
    {
        decisionTimer -= Time.deltaTime;
        agent.speed = walkSpeed;
        agent.stoppingDistance = 2.5f;
        agent.SetDestination(target.position);
        animator.SetFloat("Speed", agent.velocity.magnitude / walkSpeed, 0.1f, Time.deltaTime);

        if (decisionTimer <= 0f)
        {
            Pattern? next = PickPattern();
            if (next.HasValue)
            {
                if (next.Value == Pattern.Spores)
                {
                    BeginSpores();
                }
                else
                {
                    BeginWindUp(next.Value, false);
                }
            }
        }
    }

    // 거리에 맞는 패턴 중 하나 (직전과 같은 패턴은 피함)
    Pattern? PickPattern()
    {
        float d = FlatDistance(target.position);
        List<Pattern> options = new List<Pattern>();
        if (d <= slamForward + slamRadius) options.Add(Pattern.Slam);
        if (d <= sweepRadius + 0.5f) options.Add(Pattern.Sweep);
        if (d >= 6f && d <= chargeLength + 2f) options.Add(Pattern.Charge);
        if (phase2)
        {
            if (d <= slamForward + slamRadius + 3f) options.Add(Pattern.Combo);
            if (d <= 15f) options.Add(Pattern.Spores);
        }

        if (options.Count > 1)
        {
            options.Remove(lastPattern);
        }
        if (options.Count == 0)
        {
            return null;
        }
        return options[Random.Range(0, options.Count)];
    }

    // ───── 경고 ─────
    void BeginWindUp(Pattern next, bool continuingCombo)
    {
        pattern = next;
        state = State.WindUp;
        timer = 0f;
        agent.ResetPath();
        animator.SetFloat("Speed", 0f);
        health.SetTelegraph(true);

        switch (pattern)
        {
            case Pattern.Slam:
                duration = slamWindUp * Tempo;
                indicator = GroundIndicator.CreateSector(slamRadius, 360f, indicatorOutline, indicatorFill);
                break;
            case Pattern.Combo:
                if (!continuingCombo)
                {
                    comboIndex = 0;
                }
                duration = comboWindUp * Tempo;
                indicator = GroundIndicator.CreateSector(slamRadius, 360f, indicatorOutline, indicatorFill);
                break;
            case Pattern.Sweep:
                duration = sweepWindUp * Tempo;
                indicator = GroundIndicator.CreateSector(sweepRadius, sweepAngle, indicatorOutline, indicatorFill);
                break;
            default:
                duration = chargeWindUp * Tempo;
                indicator = GroundIndicator.CreateRect(chargeWidth, chargeLength, indicatorOutline, indicatorFill);
                break;
        }
        PlaceIndicator();
    }

    bool IsSlamLike => pattern == Pattern.Slam || pattern == Pattern.Combo;

    void PlaceIndicator()
    {
        if (IsSlamLike)
        {
            slamCenter = transform.position + transform.forward * slamForward;
            indicator.Place(slamCenter, Quaternion.identity);
        }
        else
        {
            indicator.Place(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        }
    }

    void UpdateWindUp()
    {
        timer += Time.deltaTime;
        float t = timer / duration;

        // 앞부분에는 플레이어를 따라 돌고, 이후엔 방향 고정
        if (t < lockAt)
        {
            FaceTarget(rotateSpeed * Time.deltaTime);

            // 연속 내려찍기: 경고 중에 한 걸음씩 다가옴
            if (pattern == Pattern.Combo && FlatDistance(target.position) > 2.2f)
            {
                agent.Move(transform.forward * comboStepSpeed * Time.deltaTime);
            }
            PlaceIndicator();
        }
        indicator.SetProgress(t);

        if (timer >= duration)
        {
            ClearIndicator();
            health.SetTelegraph(false);
            BeginStrike();
        }
    }

    // ───── 공격 ─────
    void BeginStrike()
    {
        lastPattern = pattern;
        timer = 0f;
        resolved = false;

        if (pattern == Pattern.Charge)
        {
            state = State.Charging;
            chargeDirection = transform.forward;
            traveled = 0f;
            chargeHit = false;
            animator.speed = 2f;
            animator.SetFloat("Speed", 1f);
            Sfx.Play(swingSounds, 0.8f);
        }
        else
        {
            state = State.Strike;
            animator.SetTrigger("Attack");
            Sfx.Play(swingSounds, 0.8f);
        }
    }

    void UpdateStrike()
    {
        timer += Time.deltaTime;
        if (!resolved && timer >= 0.3f)
        {
            resolved = true;
            if (IsSlamLike)
            {
                ResolveSlam();
            }
            else
            {
                ResolveSweep();
            }
        }

        // 연속 내려찍기: 마지막 타가 아니면 바로 다음 경고로
        if (pattern == Pattern.Combo && comboIndex < comboSlams - 1)
        {
            if (timer >= 0.5f)
            {
                comboIndex++;
                BeginWindUp(Pattern.Combo, true);
            }
            return;
        }

        // 휘두르는 동작이 끝날 때까지 기다린 뒤 빈틈
        if (timer >= 0.9f)
        {
            bool bigPattern = IsSlamLike;
            StartRecover(bigPattern ? slamRecover : sweepRecover, bigPattern);
        }
    }

    void ResolveSlam()
    {
        if (slamVfx != null)
        {
            GameObject vfx = Instantiate(slamVfx, new Vector3(slamCenter.x, 0.1f, slamCenter.z), Quaternion.identity);
            Destroy(vfx, 3f);
        }
        Sfx.Play(slamSounds, 1f);
        CameraFollow.Shake(0.4f, 0.3f);

        Vector3 d = target.position - slamCenter;
        d.y = 0f;
        if (d.magnitude <= slamRadius)
        {
            playerStats.TakeDamage(slamDamage);
        }
    }

    void ResolveSweep()
    {
        CameraFollow.Shake(0.25f, 0.2f);

        Vector3 d = target.position - transform.position;
        d.y = 0f;
        if (d.magnitude <= sweepRadius && Vector3.Angle(transform.forward, d) <= sweepAngle * 0.5f)
        {
            playerStats.TakeDamage(sweepDamage);
        }
    }

    void UpdateCharging()
    {
        float step = chargeSpeed * Time.deltaTime;
        Vector3 before = transform.position;
        agent.Move(chargeDirection * step);

        // NavMesh 가장자리나 나무·바위에 막혀서 덜 움직였으면 박은 것
        float moved = Vector3.Distance(before, transform.position);
        traveled += moved;

        if (!chargeHit && FlatDistance(target.position) <= 1.9f)
        {
            chargeHit = true;
            playerStats.TakeDamage(chargeDamage);
            CameraFollow.Shake(0.35f, 0.25f);
        }

        if (moved < step * 0.5f)
        {
            animator.speed = 1f;
            CameraFollow.Shake(0.5f, 0.4f);
            Sfx.Play(slamSounds, 1f);
            StartRecover(chargeWallRecover, true);
        }
        else if (traveled >= chargeLength)
        {
            animator.speed = 1f;
            StartRecover(chargeRecover, true);
        }
    }

    // ───── 포자 폭발 (2페이즈) ─────
    // 제자리에서 포자를 뿌리는 동안은 빈틈 (계속 움직여서 피해야 하는 대신 때릴 수 있음)
    void BeginSpores()
    {
        pattern = Pattern.Spores;
        lastPattern = pattern;
        state = State.Cast;
        timer = 0f;
        sporesSpawned = 0;
        duration = (sporeCount - 1) * sporeInterval + sporeFill + 0.4f;
        agent.ResetPath();
        animator.SetFloat("Speed", 0f);
        animator.SetTrigger("Attack");
        Sfx.Play(roarSounds, 0.7f);
        health.SetVulnerable(true);
    }

    void UpdateCast()
    {
        timer += Time.deltaTime;
        FaceTarget(rotateSpeed * Time.deltaTime);

        while (sporesSpawned < sporeCount && timer >= sporesSpawned * sporeInterval)
        {
            // 첫 폭발은 플레이어 바로 위, 이후는 주변으로 조금씩 어긋나게
            Vector2 jitter = sporesSpawned == 0 ? Vector2.zero : Random.insideUnitCircle * sporeJitter;
            Vector3 pos = target.position + new Vector3(jitter.x, 0f, jitter.y);
            SporeBlast.Spawn(pos, sporeRadius, 0f, sporeFill, sporeDamage, indicatorOutline, indicatorFill, sporeVfx, slamSounds, playerStats);
            sporesSpawned++;
        }

        if (timer >= duration)
        {
            health.SetVulnerable(false);
            StartRecover(0.8f, false);
        }
    }

    // ───── 포효 (2페이즈 시작) ─────
    void BeginRoar()
    {
        phase2 = true;
        ClearIndicator();
        state = State.Roar;
        timer = 0f;
        shockwaveDone = false;
        agent.ResetPath();
        animator.speed = 1f;
        animator.SetFloat("Speed", 0f);
        animator.SetTrigger("Attack");
        health.SetTelegraph(false);
        health.SetVulnerable(false);
        health.invulnerable = true;
        Sfx.Play(roarSounds, 1f);
    }

    void UpdateRoar()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / roarTime);
        transform.localScale = Vector3.Lerp(baseScale, baseScale * growScale, t);

        // 포효 중간에 충격파: 가까이 있으면 크게 밀려남 (대시 중이면 맞지 않음)
        if (!shockwaveDone && timer >= roarTime * 0.45f)
        {
            shockwaveDone = true;
            if (shockwaveVfx != null)
            {
                GameObject vfx = Instantiate(shockwaveVfx, new Vector3(transform.position.x, 0.1f, transform.position.z), Quaternion.identity);
                Destroy(vfx, 3f);
            }
            Sfx.Play(slamSounds, 1f);
            CameraFollow.Shake(0.7f, 0.6f);

            Vector3 away = target.position - transform.position;
            away.y = 0f;
            if (away.magnitude <= shockwaveRadius)
            {
                if (!playerController.IsDashing)
                {
                    playerStats.TakeDamage(shockwaveDamage);
                    playerController.Push(away.normalized * shockwavePush);
                }
            }
        }

        if (timer >= roarTime)
        {
            health.invulnerable = false;
            health.EnterRage(rageTint, 0.45f);
            walkSpeed *= moveBoost2;
            chargeSpeed *= 1.15f;
            state = State.Chase;
            decisionTimer = 0.6f;
        }
    }

    // ───── 숨 돌리기 (빈틈) ─────
    void StartRecover(float seconds, bool vulnerable)
    {
        state = State.Recover;
        timer = 0f;
        duration = seconds * Tempo;
        animator.SetFloat("Speed", 0f);
        health.SetVulnerable(vulnerable);
    }

    void UpdateRecover()
    {
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            health.SetVulnerable(false);
            state = State.Chase;
            decisionTimer = betweenAttacks;
        }
    }

    void ClearIndicator()
    {
        if (indicator != null)
        {
            Destroy(indicator.gameObject);
            indicator = null;
        }
    }

    void OnDestroy()
    {
        ClearIndicator();
    }
}
