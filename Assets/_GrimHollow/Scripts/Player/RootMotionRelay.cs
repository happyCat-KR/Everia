using UnityEngine;

// 공격 동작에서 앞으로 내딛는 움직임을 실제 이동으로 바꿈 (공격 중에만)
public class RootMotionRelay : MonoBehaviour
{
    private Animator animator;
    private CharacterController controller;
    private PlayerController player;

    public float stopDistance = 1.6f;   // 앞에 몬스터가 이 거리 안에 있으면 앞으로 안 나감

    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponentInParent<CharacterController>();
        player = GetComponentInParent<PlayerController>();
    }

    // 애니메이터가 이번 프레임에 움직인 양을 받아서 처리
    void OnAnimatorMove()
    {
        if (!player.IsAttacking || MonsterInFront())
        {
            return;
        }

        Vector3 delta = animator.deltaPosition;
        delta.y = 0f;               // 높이는 중력이 담당
        controller.Move(delta);
    }

    // 바라보는 방향 가까이에 몬스터가 있는지
    bool MonsterInFront()
    {
        Vector3 origin = player.transform.position + Vector3.up;   // 몸 중심 높이
        return Physics.SphereCast(origin, 0.4f, player.transform.forward, out _, stopDistance, player.monsterLayer);
    }
}