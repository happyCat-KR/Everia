using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;        // 앞으로 갈 때 (뛰기)
    public float gravity = -9.81f;

    private CharacterController controller;
    private Animator animator;
    private Camera mainCamera;
    private float verticalVelocity;

    public float nextAttackPoint = 0.9f;    // 동작이 몇 % 진행되면 다음 공격 가능 (0.9 = 90%)
    private float attackTimer;

    public int attackDamage = 10;
    public float attackRange = 2f;     // 판정 반경
    public float attackAngle = 120f;   // 부채꼴 각도
    public float hitDelay = 0.3f;      // 클릭 후 데미지가 들어가는 시점 (검 휘두르는 순간)
    public LayerMask monsterLayer;

    private float hitTimer;

    private int comboStep;                  // 다음에 나갈 타 (0 = 1타, 1 = 2타, 2 = 3타)
    public float comboResetTime = 0.5f;     // 공격이 끝나고 이 시간 안에 안 누르면 1타부터

    public float attackBlendTime = 0.15f;   // 지금 자세 -> 공격 시작 자세로 섞이는 시간
    private float comboResetTimer;

    public LayerMask obstacleLayer;     // 공격을 막는 벽

    public bool IsAttacking => !IsDashing && (attackTimer > 0f || IsAttackAnimPlaying());    // 공격 모션 중인지

    public float turnSpeed = 720f;     // 초당 회전 각도 (720 = 0.25초에 반 바퀴)

    public float attackTurnSpeed = 1440f;   // 공격할 때 마우스 쪽으로 도는 속도 (1440 = 0.125초에 반 바퀴)
    private Quaternion attackRotation;      // 공격할 때 바라볼 방향

    public float dashSpeed = 14f;           // 대시 속도
    public float dashTime = 0.4f;           // 대시 지속 시간 (이동 + 무적)
    public float dashCooldown = 0.4f;       // 대시 끝나고 다시 쓰기까지
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;

    public bool IsDashing => dashTimer > 0f;    // 대시 중 = 무적

    public AudioClip[] swingSounds;         // 칼 휘두르기
    public AudioClip[] dashSounds;          // 대시
    public AudioClip[] footstepSounds;      // 발소리
    public float footstepInterval = 0.32f;  // 발소리 간격 (달리기 속도에 맞춤)
    private float footstepTimer;

    // Q 스킬: 주변 광역 베기
    public int skillDamage = 25;
    public float skillRadius = 4f;
    public float skillCooldown = 5f;
    public float skillHitDelay = 0.2f;      // 휘두르는 순간에 데미지
    public GameObject skillVfx;             // 바닥 원형 베기 이펙트
    public GameObject skillDustVfx;         // 흙먼지
    public AudioClip[] skillSounds;
    private float skillCooldownTimer;
    private float skillHitTimer;

    public float SkillCooldownRatio => skillCooldownTimer / skillCooldown;    // HUD용 (1 = 방금 씀, 0 = 사용 가능)

    // 마법사: 근접 판정 대신 불덩이를 쏨 (PlayerClass가 직업에 맞게 설정)
    public bool isCaster;
    public Projectile projectilePrefab;
    public Transform castPoint;             // 불덩이가 나가는 위치 (지팡이 끝 등)

    string AttackStateName => isCaster ? "Cast" : "Attack" + (comboStep + 1);

    public float attackCooldown;            // 공격 사이 최소 간격 (마법사 연사 제한, 0 = 동작 길이만큼)
    public int attackManaCost;              // 공격 1회 마나
    public int skillManaCost;               // Q 1회 마나
    private float attackCooldownTimer;
    private PlayerStats stats;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        stats = GetComponent<PlayerStats>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        // 1. 이동 입력: WASD (화면 기준 → W는 항상 화면 위쪽)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Quaternion cameraYaw = Quaternion.Euler(0f, mainCamera.transform.eulerAngles.y, 0f);
        Vector3 moveDirection = cameraYaw * new Vector3(horizontal, 0f, vertical).normalized;

        // 대시: Space → 이동 방향(없으면 바라보는 방향)으로 빠르게. 공격 중이어도 끊고 나감
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space) && !IsDashing && dashCooldownTimer <= 0f)
        {
            dashDirection = moveDirection != Vector3.zero ? moveDirection : transform.forward;
            transform.rotation = Quaternion.LookRotation(dashDirection);
            animator.CrossFadeInFixedTime("Dash", 0.05f, 1, 0f);
            Sfx.Play(dashSounds, 0.8f);

            dashTimer = dashTime;
            dashCooldownTimer = dashTime + dashCooldown;
            attackTimer = 0f;       // 공격 취소
            hitTimer = 0f;          // 예약된 데미지 취소
            comboStep = 0;          // 다음 공격은 1타부터
        }

        if (IsDashing)
        {
            dashTimer -= Time.deltaTime;

            Vector3 dashVelocity = dashDirection * dashSpeed;
            dashVelocity.y = verticalVelocity;      // 땅에 붙어 있게
            controller.Move(dashVelocity * Time.deltaTime);
            return;     // 대시 중에는 공격·이동·회전 처리 안 함
        }

        // 2. 공격: 좌클릭 → 그 순간 마우스 쪽으로 돌아서 휘두름
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        // Q 스킬: 쿨타임이 끝났으면 일반 공격을 끊고 주변 전체를 벰
        if (skillCooldownTimer > 0f)
        {
            skillCooldownTimer -= Time.deltaTime;
        }

        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Q) && skillCooldownTimer <= 0f && stats.SpendMp(skillManaCost))
        {
            attackRotation = transform.rotation;
            FaceMouse();
            animator.CrossFadeInFixedTime(isCaster ? "Cast" : "Attack2", 0.05f, 1, 0f);
            Sfx.Play(skillSounds, 0.9f);

            skillCooldownTimer = skillCooldown;
            skillHitTimer = skillHitDelay;
            attackTimer = attackBlendTime + 0.05f;
            hitTimer = 0f;          // 일반 공격 데미지 예약 취소
            comboStep = 0;
        }

        if (skillHitTimer > 0f)
        {
            skillHitTimer -= Time.deltaTime;
            if (skillHitTimer <= 0f)
            {
                CastSkill();
            }
        }

        // 공격 중엔 콤보 유지, 끝나면 대기 시간 감소
        if (IsAttacking)
        {
            comboResetTimer = comboResetTime;
        }
        else if (comboResetTimer > 0f)
        {
            comboResetTimer -= Time.deltaTime;
        }

        if (Input.GetMouseButton(0) && !IsAttacking && !InventoryUI.IsOpen && attackCooldownTimer <= 0f && stats.SpendMp(attackManaCost))
        {
            attackCooldownTimer = attackCooldown;

            // 한참 쉬었다 치면 1타부터
            if (comboResetTimer <= 0f)
            {
                comboStep = 0;
            }

            attackRotation = transform.rotation;    // 마우스 방향을 못 구하면 지금 방향 유지

            FaceMouse();
            animator.CrossFadeInFixedTime(AttackStateName, attackBlendTime, 1, 0f);   // 지금 자세에서 부드럽게 넘어감
            Sfx.Play(swingSounds, 0.7f);
            comboStep = isCaster ? 0 : (comboStep + 1) % 2;    // 0 → 1 → 0 (1타 → 2타 반복), 마법사는 한 동작

            attackTimer = attackBlendTime + 0.05f;     // 애니메이터가 공격 상태로 넘어갈 때까지 잠깐 잠금
            hitTimer = hitDelay;
        }

        // 공격 중에는 제자리에 멈춤
        if (IsAttacking)
        {
            moveDirection = Vector3.zero;
        }

        // 걷는 중이면 일정 간격으로 발소리
        if (moveDirection != Vector3.zero && controller.isGrounded)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                Sfx.Play(footstepSounds, 0.35f);
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;     // 멈췄다 다시 걸으면 바로 한 발
        }

        // 3. 회전: 평소엔 이동 방향, 공격 중엔 마우스 방향으로 빠르게 돌기
        if (IsAttacking)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, attackRotation, attackTurnSpeed * Time.deltaTime);
        }
        else if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // 휘두르는 타이밍에 데미지
        if (hitTimer > 0f)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0f)
            {
                if (isCaster)
                {
                    ShootProjectile();
                }
                else
                {
                    DealDamage();
                }
            }
        }

        // 4. 캐릭터 기준 방향으로 변환 → 애니메이션
        Vector3 localMove = transform.InverseTransformDirection(moveDirection);
        animator.SetFloat("MoveX", localMove.x, 0.1f, Time.deltaTime);
        animator.SetFloat("MoveZ", localMove.z, 0.1f, Time.deltaTime);

        // 5. 중력
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    // UpperBody 레이어에서 공격 동작이 아직 재생 중인지
    bool IsAttackAnimPlaying()
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(1);
        bool inAttack = state.IsName("Attack1") || state.IsName("Attack2") || state.IsName("Attack3") || state.IsName("Cast");
        return inAttack && state.normalizedTime < nextAttackPoint;
    }

    void FaceMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 lookDirection = ray.GetPoint(enter) - transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.01f)
            {
                attackRotation = Quaternion.LookRotation(lookDirection);   // 바로 돌지 않고 목표만 정함
            }
        }
    }

    void DealDamage()
    {
        Vector3 center = transform.position + Vector3.up;   // 몸 중심 높이
        Collider[] hits = Physics.OverlapSphere(center, attackRange, monsterLayer);

        MonsterHealth target = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            MonsterHealth monster = hit.GetComponent<MonsterHealth>();
            if (monster == null || monster.isDead)
            {
                continue;
            }

            Vector3 toMonster = hit.transform.position - transform.position;
            toMonster.y = 0f;

            // 부채꼴 밖이면 제외
            if (Vector3.Angle(transform.forward, toMonster) > attackAngle * 0.5f)
            {
                continue;
            }

            // 사이에 벽이 있으면 제외  ← 여기 추가
            if (Physics.Linecast(center, hit.bounds.center, obstacleLayer))
            {
                continue;
            }

            // 가장 가까운 한 마리
            float distance = toMonster.magnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                target = monster;
            }
        }

        if (target != null)
        {
            target.TakeDamage(attackDamage);
            CameraFollow.Shake(0.12f, 0.12f);
        }
    }

    // 바라보는 방향으로 불덩이 발사
    void ShootProjectile()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        Vector3 start = castPoint != null ? castPoint.position : transform.position + Vector3.up * 1.2f + transform.forward * 0.6f;
        start.y = transform.position.y + 1.1f;      // 몬스터 몸 높이로 날아가게
        Projectile p = Instantiate(projectilePrefab, start, Quaternion.LookRotation(attackRotation * Vector3.forward));
        p.Launch(attackDamage, monsterLayer, ~monsterLayer.value);     // 몬스터 말고 부딪히는 건 전부 막힘 (나무, 바위, 집)
    }

    // 주변 모든 몬스터에게 데미지 (넉백은 MonsterHealth가 처리)
    void CastSkill()
    {
        Vector3 feet = transform.position + Vector3.up * 0.05f;
        SpawnVfx(skillVfx, feet);
        SpawnVfx(skillDustVfx, feet);
        CameraFollow.Shake(0.3f, 0.25f);

        Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up, skillRadius, monsterLayer);
        foreach (Collider hit in hits)
        {
            MonsterHealth monster = hit.GetComponent<MonsterHealth>();
            if (monster != null && !monster.isDead)
            {
                monster.TakeDamage(skillDamage, true);      // 스킬은 강한 공격: 경직 + 넉백
            }
        }
    }

    void SpawnVfx(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
        {
            return;
        }

        GameObject vfx = Instantiate(prefab, position, Quaternion.identity);
        Destroy(vfx, 3f);
    }

    // Scene 뷰에서 Player 선택 시 판정 범위 표시
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = transform.position + Vector3.up;
        Gizmos.DrawWireSphere(center, attackRange);

        Vector3 left = Quaternion.Euler(0f, -attackAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, attackAngle * 0.5f, 0f) * transform.forward;
        Gizmos.DrawLine(center, center + left * attackRange);
        Gizmos.DrawLine(center, center + right * attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, skillRadius);
    }
}