using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    // 몬스터 회전이 끝난 뒤에 덮어써야 해서 LateUpdate
    void LateUpdate()
    {
        transform.rotation = mainCamera.transform.rotation;
    }
}