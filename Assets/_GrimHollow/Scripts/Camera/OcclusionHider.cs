using UnityEngine;

// 카메라 → 플레이어 사이를 가리는 Occludable을 숨김 (쿼터뷰에서 건물 뒤로 가도 캐릭터가 보이게)
public class OcclusionHider : MonoBehaviour
{
    public float targetHeight = 1f;     // 플레이어 몸 중심 높이
    public float padding = 0.5f;        // 시야선 주변 여유 (클수록 넓게 숨김)

    private Transform target;
    private Occludable[] occludables;

    void Start()
    {
        target = GameObject.FindWithTag("Player").transform;
        occludables = FindObjectsByType<Occludable>(FindObjectsSortMode.None);
    }

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 from = transform.position;
        Vector3 to = target.position + Vector3.up * targetHeight;
        Vector3 direction = to - from;
        float distance = direction.magnitude;
        Ray ray = new Ray(from, direction / distance);

        foreach (Occludable o in occludables)
        {
            Bounds b = o.Bounds;
            b.Expand(padding);

            // 시야선이 상자를 지나고, 그 지점이 플레이어보다 카메라 쪽이면 가리는 것
            bool blocks = b.IntersectRay(ray, out float hitDistance) && hitDistance < distance;
            o.SetHidden(blocks);
        }
    }
}
