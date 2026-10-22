
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Range(35f, 75f)]
    public float angle = 55f;

    public float distance = 10f;

    public float yaw = 45f;     // 내려다보는 방향 (45 = 대각선, 건물 두 면이 같이 보임)

    void Start()
    {
        // 비어 있으면 Player 태그로 찾기 → 어느 씬에 넣어도 바로 동작
        if (target == null)
        {
            target = GameObject.FindWithTag("Player").transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(angle, yaw, 0f);

        transform.rotation = rotation;
        transform.position =
            target.position - rotation * Vector3.forward * distance;
    }
}
