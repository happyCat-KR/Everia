using UnityEngine;

// 대시 느낌: 발밑 먼지, 바닥에 끌린 자국, 뒤로 남는 바람 줄무늬, 화면 당김
public class DashEffects : MonoBehaviour
{
    public GameObject dustPrefab;           // 먼지 (Hovl 먼지 재활용)
    public Material scarMaterial;
    public TrailRenderer[] streaks;         // 몸 양옆에서 뒤로 남는 흰 줄
    public float dustScale = 0.45f;
    public float puffInterval = 0.05f;
    public float scarWidth = 0.35f;
    public float fovKick = 7f;

    private bool active;
    private float puffTimer;

    // 대시를 시작할 때 (방향과 거리)
    public void Begin(Vector3 direction, float distance)
    {
        active = true;
        puffTimer = 0f;

        SpawnDust(1.3f);
        DashScar.Create(transform.position, direction, distance + 0.6f, scarWidth, scarMaterial);
        CameraFollow.Kick(fovKick, 0.25f);

        foreach (TrailRenderer streak in streaks)
        {
            streak.Clear();
            streak.emitting = true;
        }
    }

    // 대시가 끝날 때
    public void End()
    {
        if (!active)
        {
            return;
        }

        active = false;
        SpawnDust(1.1f);
        foreach (TrailRenderer streak in streaks)
        {
            streak.emitting = false;
        }
    }

    void Update()
    {
        if (!active)
        {
            return;
        }

        // 달리는 동안 발밑에서 작은 먼지가 계속 일어남
        puffTimer -= Time.deltaTime;
        if (puffTimer <= 0f)
        {
            puffTimer = puffInterval;
            SpawnDust(0.5f);
        }
    }

    void SpawnDust(float sizeMultiplier)
    {
        if (dustPrefab == null)
        {
            return;
        }

        GameObject dust = Instantiate(dustPrefab, transform.position + Vector3.up * 0.1f, Quaternion.identity);
        dust.transform.localScale *= dustScale * sizeMultiplier;
        Destroy(dust, 2f);
    }
}
