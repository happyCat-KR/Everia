using UnityEngine;

// 보스 방 열쇠: 중간보스가 죽은 자리에 떨어져서 빙글빙글 돎. 가까이 가면 줍는다
public class KeyPickup : MonoBehaviour
{
    public float pickupRange = 1.8f;

    private Transform player;
    private Vector3 basePos;

    public static KeyPickup Spawn(Vector3 position)
    {
        GameObject go = new GameObject("Key_BossRoom");
        go.transform.position = position + Vector3.up * 1.1f;

        Material gold = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        gold.color = new Color(1f, 0.8f, 0.2f);
        gold.SetFloat("_Metallic", 0.8f);
        gold.SetFloat("_Smoothness", 0.7f);

        // 열쇠 모양: 고리(납작한 원통) + 줄기 + 이빨 두 개
        Part(go.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.32f, 0f), new Vector3(0.32f, 0.04f, 0.32f), Quaternion.Euler(90f, 0f, 0f), gold);
        Part(go.transform, PrimitiveType.Cube, new Vector3(0f, -0.1f, 0f), new Vector3(0.07f, 0.55f, 0.07f), Quaternion.identity, gold);
        Part(go.transform, PrimitiveType.Cube, new Vector3(0.1f, -0.28f, 0f), new Vector3(0.18f, 0.07f, 0.07f), Quaternion.identity, gold);
        Part(go.transform, PrimitiveType.Cube, new Vector3(0.08f, -0.14f, 0f), new Vector3(0.14f, 0.07f, 0.07f), Quaternion.identity, gold);

        Light glow = go.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.85f, 0.4f);
        glow.range = 5f;
        glow.intensity = 1.5f;

        go.transform.localScale = Vector3.one * 1.6f;
        return go.AddComponent<KeyPickup>();
    }

    static void Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Quaternion rot, Material mat)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = pos;
        part.transform.localRotation = rot;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = mat;
    }

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        basePos = transform.position;
    }

    void Update()
    {
        transform.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
        transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 2.5f) * 0.12f;

        Vector3 d = player.position - basePos;
        d.y = 0f;
        if (d.magnitude <= pickupRange)
        {
            DungeonDirector.HasKey = true;
            UIBanner.Show("보스 방의 열쇠를 얻었다", "북쪽 철문으로 가자", 2.4f);
            Destroy(gameObject);
        }
    }
}
