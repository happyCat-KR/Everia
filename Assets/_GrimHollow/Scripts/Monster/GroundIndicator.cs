using UnityEngine;

// 바닥에 깔리는 공격 범위 경고 (원, 부채꼴, 직사각형). 바깥 윤곽은 고정이고 안쪽이 시간에 따라 차오름
public class GroundIndicator : MonoBehaviour
{
    private Transform fill;
    private bool lengthFill;        // true면 앞쪽으로 길어지며 차오름 (돌진), false면 중심에서 커짐 (원·부채꼴)

    // 원 또는 부채꼴 (angle 360 = 원). 중심이 기준점
    public static GroundIndicator CreateSector(float radius, float angle, Material outline, Material fillMat)
    {
        return Create(SectorMesh(radius, angle), outline, fillMat, false);
    }

    // 직사각형: 기준점에서 앞(+Z)으로 length, 좌우 width
    public static GroundIndicator CreateRect(float width, float length, Material outline, Material fillMat)
    {
        return Create(RectMesh(width, length), outline, fillMat, true);
    }

    static GroundIndicator Create(Mesh mesh, Material outline, Material fillMat, bool lengthFill)
    {
        GameObject go = new GameObject("GroundIndicator");
        GroundIndicator indicator = go.AddComponent<GroundIndicator>();
        indicator.lengthFill = lengthFill;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = outline;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(go.transform, false);
        fillGo.transform.localPosition = Vector3.up * 0.01f;      // 윤곽 위에 겹침
        fillGo.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer fr = fillGo.AddComponent<MeshRenderer>();
        fr.sharedMaterial = fillMat;
        fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fr.receiveShadows = false;
        indicator.fill = fillGo.transform;
        indicator.SetProgress(0f);
        return indicator;
    }

    public void Place(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(new Vector3(position.x, 0.12f, position.z), rotation);
    }

    // 0 → 1: 안쪽이 차오름 (1이 되는 순간 공격이 나감)
    public void SetProgress(float t)
    {
        t = Mathf.Clamp01(t);
        fill.localScale = lengthFill ? new Vector3(1f, 1f, Mathf.Max(t, 0.001f)) : new Vector3(Mathf.Max(t, 0.001f), 1f, Mathf.Max(t, 0.001f));
    }

    static Mesh SectorMesh(float radius, float angle)
    {
        int segments = Mathf.Max(8, Mathf.CeilToInt(angle / 6f));
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];
        vertices[0] = Vector3.zero;

        float start = -angle * 0.5f;
        for (int i = 0; i <= segments; i++)
        {
            float a = (start + angle * i / segments) * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius;
        }
        for (int i = 0; i < segments; i++)
        {
            // 위에서 봤을 때 시계 방향이어야 위쪽 면이 보임
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 2;
            triangles[i * 3 + 2] = i + 1;
        }

        Mesh mesh = new Mesh { vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        return mesh;
    }

    static Mesh RectMesh(float width, float length)
    {
        float h = width * 0.5f;
        Vector3[] vertices = { new Vector3(-h, 0, 0), new Vector3(h, 0, 0), new Vector3(h, 0, length), new Vector3(-h, 0, length) };
        int[] triangles = { 0, 3, 2, 0, 2, 1 };
        Mesh mesh = new Mesh { vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        return mesh;
    }
}
