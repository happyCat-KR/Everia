using UnityEngine;

// 포탈로 이 씬에 들어왔을 때 플레이어가 놓일 자리
// PlayerStats.Start(HP 초기화)보다 늦게 실행돼야 이어받은 HP가 덮어써지지 않음
[DefaultExecutionOrder(100)]
public class SpawnPoint : MonoBehaviour
{
    public string id;

    void Start()
    {
        if (ScenePortal.nextSpawn != id)
        {
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");

        // CharacterController가 켜져 있으면 위치를 바로 못 바꿔서 잠깐 끔
        CharacterController controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.SetPositionAndRotation(transform.position, transform.rotation);
        controller.enabled = true;

        // 이전 씬의 레벨, 경험치, HP 이어받기
        if (ScenePortal.hasSavedStats)
        {
            PlayerStats stats = player.GetComponent<PlayerStats>();
            stats.level = ScenePortal.savedLevel;
            stats.currentExp = ScenePortal.savedExp;
            stats.maxHp = ScenePortal.savedMaxHp;
            stats.currentHp = ScenePortal.savedHp;
        }

        ScenePortal.nextSpawn = null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.5f);
        Gizmos.DrawRay(transform.position + Vector3.up, transform.forward);
    }
}
