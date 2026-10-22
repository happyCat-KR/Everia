using UnityEngine;

// 대시 방향으로 바닥에 길게 끌린 자국 (양 끝이 뾰족한 모양, 1초에 걸쳐 사라짐)
public class DashScar : MonoBehaviour
{
    private Material material;
    private Color baseColor;
    private float life;
    private float duration;

    public static void Create(Vector3 start, Vector3 direction, float length, float width, Material source, float duration = 1.1f)
    {
        GameObject go = new GameObject("DashScar");
        go.transform.SetPositionAndRotation(new Vector3(start.x, 0.1f, start.z), Quaternion.LookRotation(direction));

        Vector3[] vertices =
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(-width, 0f, length * 0.35f),
            new Vector3(0f, 0f, length),
            new Vector3(width, 0f, length * 0.35f)
        };
        Mesh mesh = new Mesh { vertices = vertices, triangles = new[] { 0, 1, 3, 1, 2, 3 } };
        mesh.RecalculateNormals();

        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.material = source;       // 복사본: 자국마다 따로 사라져야 함

        DashScar scar = go.AddComponent<DashScar>();
        scar.material = mr.material;
        scar.baseColor = scar.material.GetColor("_BaseColor");
        scar.duration = duration;
    }

    void Update()
    {
        life += Time.deltaTime;
        float t = life / duration;
        if (t >= 1f)
        {
            Destroy(material);
            Destroy(gameObject);
            return;
        }

        Color c = baseColor;
        c.a *= 1f - t;
        material.SetColor("_BaseColor", c);
    }
}
