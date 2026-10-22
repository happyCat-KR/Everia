using UnityEngine;

// 카메라와 플레이어 사이를 가리면 숨겨질 오브젝트 (건물, 큰 나무 등의 맨 위에 붙임)
public class Occludable : MonoBehaviour
{
    private Renderer[] renderers;
    private Bounds bounds;
    private bool hidden;

    public Bounds Bounds => bounds;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();

        // 자식 렌더러 전체를 감싸는 상자 (정적인 건물·나무라서 한 번만 계산)
        bounds = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }
    }

    public void SetHidden(bool value)
    {
        if (hidden == value)
        {
            return;
        }

        hidden = value;

        // 안 보이게 하되 그림자는 남김 → 갑자기 사라져도 바닥 그림자로 위치를 알 수 있음
        UnityEngine.Rendering.ShadowCastingMode mode = value
            ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly
            : UnityEngine.Rendering.ShadowCastingMode.On;

        foreach (Renderer r in renderers)
        {
            r.shadowCastingMode = mode;
        }
    }
}
