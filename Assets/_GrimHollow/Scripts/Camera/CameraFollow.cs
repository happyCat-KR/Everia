
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Range(35f, 75f)]
    public float angle = 55f;

    public float distance = 10f;

    public float yaw = 45f;     // 내려다보는 방향 (45 = 대각선, 건물 두 면이 같이 보임)

    // 화면 흔들림 (타격감)
    private static float shakeStrength;
    private static float shakeTime;
    private static float shakeDuration;

    public static void Shake(float strength, float duration)
    {
        // 더 센 흔들림이 오면 덮어씀
        if (strength >= shakeStrength * (shakeTime / Mathf.Max(shakeDuration, 0.0001f)))
        {
            shakeStrength = strength;
            shakeDuration = duration;
            shakeTime = duration;
        }
    }

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

        if (shakeTime > 0f)
        {
            shakeTime -= Time.deltaTime;
            float fade = Mathf.Clamp01(shakeTime / shakeDuration);     // 점점 약해짐
            transform.position += Random.insideUnitSphere * shakeStrength * fade;
        }
    }
}
