
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Range(35f, 75f)]
    public float angle = 55f;

    public float distance = 10f;

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

        Quaternion rotation = Quaternion.Euler(angle, 0f, 0f);

        transform.rotation = rotation;
        transform.position =
            target.position - rotation * Vector3.forward * distance;
    }
}
