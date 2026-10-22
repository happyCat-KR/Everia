using System.Collections.Generic;
using UnityEngine;

// 본체 메시에서 머리(목 위)만 남기고 옷에 가려질 몸은 안 그리게 함
public class HeadOnlyMesh : MonoBehaviour
{
    public SkinnedMeshRenderer bodyRenderer;                // SuperHero_Male
    public string[] keepBones = { "Head", "neck_01" };      // 이 뼈에 붙은 부분만 남김

    void Start()
    {
        Mesh mesh = Instantiate(bodyRenderer.sharedMesh);   // 원본은 건드리지 않게 복사본 사용

        // 남길 뼈 번호 찾기
        HashSet<int> keep = new HashSet<int>();
        Transform[] bones = bodyRenderer.bones;
        for (int i = 0; i < bones.Length; i++)
        {
            if (System.Array.IndexOf(keepBones, bones[i].name) >= 0)
            {
                keep.Add(i);
            }
        }

        BoneWeight[] weights = mesh.boneWeights;

        // 삼각형마다 검사해서 남길 뼈에 붙은 것만 유지
        for (int s = 0; s < mesh.subMeshCount; s++)
        {
            int[] tris = mesh.GetTriangles(s);
            List<int> newTris = new List<int>();

            for (int t = 0; t < tris.Length; t += 3)
            {
                bool isHead = keep.Contains(weights[tris[t]].boneIndex0)
                           || keep.Contains(weights[tris[t + 1]].boneIndex0)
                           || keep.Contains(weights[tris[t + 2]].boneIndex0);

                if (isHead)
                {
                    newTris.Add(tris[t]);
                    newTris.Add(tris[t + 1]);
                    newTris.Add(tris[t + 2]);
                }
            }
            mesh.SetTriangles(newTris, s);
        }

        bodyRenderer.sharedMesh = mesh;
    }
}