
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

    // 시야 당김 (대시 속도감): 잠깐 넓어졌다가 돌아옴
    private static float kickAmount;
    private static float kickTime;
    private static float kickDuration;
    private Camera cam;
    private float baseFov;

    public static void Kick(float fovAdd, float duration)
    {
        kickAmount = fovAdd;
        kickDuration = duration;
        kickTime = duration;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        baseFov = cam.fieldOfView;
        kickTime = 0f;      // 씬이 바뀔 때 이전 값이 남지 않게

        // 비어 있으면 Player 태그로 찾기 → 어느 씬에 넣어도 바로 동작
        if (target == null)
        {
            target = GameObject.FindWithTag("Player").transform;
        }
    }

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

        // 시야 당김: 시작하자마자 넓어지고 부드럽게 돌아옴
        if (kickTime > 0f)
        {
            kickTime -= Time.deltaTime;
            float t = Mathf.Clamp01(kickTime / kickDuration);
            cam.fieldOfView = baseFov + kickAmount * t * t;
        }
        else if (cam.fieldOfView != baseFov)
        {
            cam.fieldOfView = baseFov;
        }
    }
}
