using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 보스: 패턴 3개 (내려찍기, 횡베기, 돌진). 공격 전에 바닥에 범위가 차오르는 경고가 뜨고,
// 큰 패턴이 끝나면 기진맥진해서 빈틈이 생김 (이때만 데미지가 늘어남)
public class BossController : MonoBehaviour
{
    enum Pattern { Slam, Sweep, Charge }
    enum State { Idle, Chase, WindUp, Strike, Charging, Recover }

    public string bossName = "Mushroom King";
    public float wakeRange = 14f;           // 이 안에 들어오면 깨어남
    public float walkSpeed = 3.2f;
    public float betweenAttacks = 0.6f;     // 패턴 사이에 숨 돌리는 시간

    [Header("Slam (내려찍기)")]
    public int slamDamage = 30;
    public float slamRadius = 3.6f;
    public float slamForward = 2.4f;        // 보스 앞 이만큼 떨어진 곳이 중심
    public float slamWindUp = 1.1f;
    public float slamRecover = 2.2f;        // 빈틈

    [Header("Sweep (횡베기)")]
    public int sweepDamage = 20;
    public float sweepRadius = 5f;
    public float sweepAngle = 130f;
    public float sweepWindUp = 0.8f;
    public float sweepRecover = 0.9f;

    [Header("Charge (돌진)")]
    public int chargeDamage = 30;
    public float chargeWidth = 2.4f;
    public float chargeLength = 13f;
    public float chargeWindUp = 1.0f;
    public float chargeSpeed = 15f;
    public float chargeRecover = 2.4f;      // 빈틈
    public float chargeWallRecover = 3.4f;  // 벽에 박으면 더 긴 빈틈

    public float rotateSpeed = 220f;        // 경고 중 플레이어 쪽으로 도는 속도
    [Range(0f, 1f)] public float lockAt = 0.55f;   // 경고 시간의 이 지점부터 방향 고정 (이후엔 옆으로 피하면 됨)

    [Header("Effects")]
    public Material indicatorOutline;
    public Material indicatorFill;
    public GameObject slamVfx;
    public AudioClip[] slamSounds;
    public AudioClip[] swingSounds;

    public bool IsAwake => state != State.Idle;
    public string CurrentPhaseText => state.ToString();

    private NavMeshAgent agent;
    private Animator animator;
    private MonsterHealth health;
    private PlayerStats playerStats;
    private Transform target;

    private State state = State.Idle;
    private Pattern pattern;
    private Pattern lastPattern = (Pattern)(-1);
    private float timer;
    private float duration;
    private float decisionTimer;
    private bool vulnerableAfter;
    private bool resolved;
    private GroundIndicator indicator;
    private Vector3 slamCenter;
    private Vector3 chargeDirection;
    private float traveled;
    private bool chargeHit;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        health = GetComponent<MonsterHealth>();
        target = GameObject.FindWithTag("Player").transform;
        playerStats = target.GetComponent<PlayerStats>();
        agent.speed = walkSpeed;
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

        switch (state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Chase: UpdateChase(); break;
            case State.WindUp: UpdateWindUp(); break;
            case State.Strike: UpdateStrike(); break;
            case State.Charging: UpdateCharging(); break;
            case State.Recover: UpdateRecover(); break;
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
                BeginWindUp(next.Value);
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
    void BeginWindUp(Pattern next)
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
                duration = slamWindUp;
                indicator = GroundIndicator.CreateSector(slamRadius, 360f, indicatorOutline, indicatorFill);
                break;
            case Pattern.Sweep:
                duration = sweepWindUp;
                indicator = GroundIndicator.CreateSector(sweepRadius, sweepAngle, indicatorOutline, indicatorFill);
                break;
            default:
                duration = chargeWindUp;
                indicator = GroundIndicator.CreateRect(chargeWidth, chargeLength, indicatorOutline, indicatorFill);
                break;
        }
        PlaceIndicator();
    }

    void PlaceIndicator()
    {
        if (pattern == Pattern.Slam)
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
            if (pattern == Pattern.Slam)
            {
                ResolveSlam();
            }
            else
            {
                ResolveSweep();
            }
        }

        // 휘두르는 동작이 끝날 때까지 기다린 뒤 빈틈
        if (timer >= 0.9f)
        {
            StartRecover(pattern == Pattern.Slam ? slamRecover : sweepRecover, pattern == Pattern.Slam);
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

    // ───── 숨 돌리기 (빈틈) ─────
    void StartRecover(float seconds, bool vulnerable)
    {
        state = State.Recover;
        timer = 0f;
        duration = seconds;
        vulnerableAfter = vulnerable;
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
