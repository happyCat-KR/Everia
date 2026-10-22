using System.Collections.Generic;
using UnityEngine;

// 옷 메시를 본체 뼈대에 연결해서 같이 움직이게 함 (뼈 이름이 같아야 함)
public class OutfitBinder : MonoBehaviour
{
    public Transform rootBone;          // 본체의 root 뼈 (Armature 아래 root)
    public GameObject[] outfits;        // 입힐 옷들

    void Awake()
    {
        // 본체 뼈를 이름으로 찾을 수 있게 정리
        Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        foreach (Transform bone in rootBone.GetComponentsInChildren<Transform>())
        {
            bones[bone.name] = bone;
        }

        foreach (GameObject outfit in outfits)
        {
            foreach (SkinnedMeshRenderer smr in outfit.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                // 옷이 쓰던 뼈를 같은 이름의 본체 뼈로 교체
                Transform[] newBones = new Transform[smr.bones.Length];
                for (int i = 0; i < smr.bones.Length; i++)
                {
                    newBones[i] = bones[smr.bones[i].name];
                }
                smr.bones = newBones;
                smr.rootBone = bones[smr.rootBone.name];
            }
        }
    }
}