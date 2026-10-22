using UnityEngine;
using UnityEngine.AI;

public class MonsterController : MonoBehaviour
{
    private Transform target;
    public float detectionRange = 5f;
    public float loseRange = 12f;
    private bool isChasing;

    public float attackRange = 1.5f;
    public float moveSpeed = 2f;

    public int attackDamage = 10;
    public float attackCooldown = 1f;
    public float hitDelay = 0.4f;       // 공격 시작 후 데미지가 들어가는 시점 (도끼 내려치는 순간)

    private NavMeshAgent agent;
    private Animator animator;
    private MonsterHealth health;
    private PlayerStats playerStats;

    private float attackTimer;
    private float hitTimer;

    public float stunTime = 0.4f;       // 맞으면 멈추는 시간 (경직)
    public float knockbackForce = 4f;   // 밀려나는 세기
    private float stunTimer;
    private Vector3 knockback;

    void Start()
    {
        target = GameObject.FindWithTag("Player").transform;

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        health = GetComponent<MonsterHealth>();
        playerStats = target.GetComponent<PlayerStats>();

        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange * 0.9f;   // 너무 바짝 붙지 않게
        attackTimer = attackCooldown;   // 사거리 들어오면 바로 첫 공격
    }

    void Update()
    {
        // 죽었으면 아무것도 안 함
        if (health.isDead)
        {
            return;
        }

        // 플레이어가 죽었으면 멈추고 대기  ← 여기 추가
        if (playerStats.isDead)
        {
            agent.ResetPath();
            animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            return;
        }

         // 경직 중: 밀려나기만 하고 아무것도 안 함
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            agent.Move(knockback * Time.deltaTime);
            knockback = Vector3.Lerp(knockback, Vector3.zero, 10f * Time.deltaTime);   // 점점 느려짐
            animator.SetFloat("Speed", 0f);
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        // 추적 상태 전환
        if (!isChasing && distance <= detectionRange)
        {
            isChasing = true;
        }
        else if (isChasing && distance > loseRange)
        {
            isChasing = false;
        }

        if (distance <= attackRange)
        {
            agent.ResetPath();

            // 멈춰 있는 동안에도 플레이어를 바라보게
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            attackTimer += Time.deltaTime;

            // 이전 모션이 끝나고 Locomotion으로 돌아왔을 때만 공격 시작
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool canAttack = state.IsName("Locomotion") && !animator.IsInTransition(0);

            if (attackTimer >= attackCooldown && canAttack)
            {
                animator.SetTrigger("Attack");
                attackTimer = 0f;
                hitTimer = hitDelay;
            }
        }
        else if (isChasing)
        {
            agent.SetDestination(target.position);
        }
        else
        {
            agent.ResetPath();
        }

        // 휘두르는 타이밍에 데미지 (그 사이 사거리 밖으로 피하면 안 맞음)
        if (hitTimer > 0f)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0f)
            {
                float hitDistance = Vector3.Distance(transform.position, target.position);
                if (hitDistance <= attackRange + 0.3f)
                {
                    playerStats.TakeDamage(attackDamage);
                }
            }
        }

        // 이동 속도 → 애니메이션 (0 = Idle, 1 = Walk)
        float speedPercent = agent.velocity.magnitude / moveSpeed;
        animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);
    }

    public void Aggro()
    {
        isChasing = true;
    }

    // 맞았을 때: 공격 취소 + 멈춤 + 뒤로 밀림
    public void Stagger(Vector3 direction)
    {
        stunTimer = stunTime;
        hitTimer = 0f;          // 휘두르던 공격 취소 (데미지 안 들어감)
        attackTimer = 0f;       // 풀리자마자 바로 공격하지 않게
        agent.ResetPath();

        direction.y = 0f;
        knockback = direction.normalized * knockbackForce;
    }
}