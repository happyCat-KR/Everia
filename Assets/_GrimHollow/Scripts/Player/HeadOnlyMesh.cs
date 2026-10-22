using System.Collections.Generic;
using UnityEngine;

// 본체 메시에서 머리(목 위)만 남기고 옷에 가려질 몸은 안 그리게 함
public class HeadOnlyMesh : MonoBehaviour
{
    public SkinnedMeshRenderer bodyRenderer;                // SuperHero_Male
    public string[] keepBones = { "Head", "neck_01" };      // 이 뼈에 붙은 부분은 전부 남김
    public float minWeight = 0.05f;                         // 이 비율 이상 남길 뼈의 영향을 받으면 남김

    // 목 아래 옷깃 안쪽이 비어 보이지 않게, 목 시작점에서 이만큼 아래까지의 피부도 남김 (더 내리면 옷을 뚫고 나옴)
    public float collarDepth = 0.1f;
    public float collarRadius = 0.13f;  // 목 중심에서 이 거리 안쪽만 (양팔·어깨는 제외)

    void Start()
    {
        Mesh mesh = Instantiate(bodyRenderer.sharedMesh);   // 원본은 건드리지 않게 복사본 사용

        // 남길 뼈 번호와 목 높이 찾기 (메시 기준 좌표)
        HashSet<int> keep = new HashSet<int>();
        Transform[] bones = bodyRenderer.bones;
        Vector3 neckPos = Vector3.zero, headPos = Vector3.up;
        for (int i = 0; i < bones.Length; i++)
        {
            if (System.Array.IndexOf(keepBones, bones[i].name) >= 0)
            {
                keep.Add(i);
            }
            if (bones[i].name == "neck_01")
            {
                neckPos = mesh.bindposes[i].inverse.GetColumn(3);
            }
            if (bones[i].name == "Head")
            {
                headPos = mesh.bindposes[i].inverse.GetColumn(3);
            }
        }

        // FBX마다 메시의 위쪽 축이 달라서(Y 또는 Z) 목→머리 방향을 위로 사용
        Vector3 up = (headPos - neckPos).normalized;

        BoneWeight[] weights = mesh.boneWeights;
        Vector3[] vertices = mesh.vertices;

        // 삼각형마다 검사해서 머리·목, 그리고 옷깃 안쪽 띠만 유지
        for (int s = 0; s < mesh.subMeshCount; s++)
        {
            int[] tris = mesh.GetTriangles(s);
            List<int> newTris = new List<int>();

            for (int t = 0; t < tris.Length; t += 3)
            {
                int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                bool isHead = IsKept(weights[a], keep) || IsKept(weights[b], keep) || IsKept(weights[c], keep);
                bool isCollar = InCollar(vertices[a], neckPos, up) && InCollar(vertices[b], neckPos, up) && InCollar(vertices[c], neckPos, up);

                if (isHead || isCollar)
                {
                    newTris.Add(a);
                    newTris.Add(b);
                    newTris.Add(c);
                }
            }
            mesh.SetTriangles(newTris, s);
        }

        bodyRenderer.sharedMesh = mesh;
    }

    // 목 시작점 아래 collarDepth, 목 중심에서 collarRadius 안쪽인지
    bool InCollar(Vector3 v, Vector3 neckPos, Vector3 up)
    {
        Vector3 offset = v - neckPos;
        float height = Vector3.Dot(offset, up);
        float side = (offset - up * height).magnitude;
        return height >= -collarDepth && side <= collarRadius;
    }

    // 정점이 받는 뼈 영향 4개 중 하나라도 남길 뼈면 true (경계 부분이 잘려 나가지 않게)
    bool IsKept(BoneWeight w, HashSet<int> keep)
    {
        return (keep.Contains(w.boneIndex0) && w.weight0 >= minWeight)
            || (keep.Contains(w.boneIndex1) && w.weight1 >= minWeight)
            || (keep.Contains(w.boneIndex2) && w.weight2 >= minWeight)
            || (keep.Contains(w.boneIndex3) && w.weight3 >= minWeight);
    }
}
