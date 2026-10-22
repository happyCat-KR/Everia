using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public float moveSpeed = 1.5f;  // 위로 떠오르는 속도
    public float lifeTime = 0.8f;   // 사라지기 까지 시간

    private TextMeshPro text;
    private Color color;
    private float timer;
    private Camera mainCamera;

    // 생성 직후 몬스터 쪽에서 호출
    public void Setup(int damage)
    {
        text = GetComponent<TextMeshPro>();
        text.text = damage.ToString();
        color = text.color;
        mainCamera = Camera.main;
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // 점점 투명하게
        timer += Time.deltaTime;
        color.a = 1f - timer / lifeTime;
        text.color = color;

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    void LateUpdate()
    {
        transform.rotation = mainCamera.transform.rotation;   // 항상 카메라를 보게 (Billboard와 같은 원리)
    }
}