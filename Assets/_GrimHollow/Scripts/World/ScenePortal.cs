using UnityEngine;
using UnityEngine.SceneManagement;

// 플레이어가 들어오면 다른 씬으로 이동 (Is Trigger 콜라이더 필요)
public class ScenePortal : MonoBehaviour
{
    public string targetScene;      // 이동할 씬 이름 (Build Settings에 등록돼 있어야 함)
    public string targetSpawn;      // 도착 씬의 SpawnPoint id

    // 씬이 바뀌어도 남아야 하는 값 (도착 위치, 플레이어 상태)
    public static string nextSpawn;
    public static bool hasSavedStats;
    public static int savedLevel, savedExp, savedMaxHp, savedHp;

    private bool used;

    void OnTriggerEnter(Collider other)
    {
        if (used || !other.CompareTag("Player"))
        {
            return;
        }

        used = true;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        savedLevel = stats.level;
        savedExp = stats.currentExp;
        savedMaxHp = stats.maxHp;
        savedHp = stats.currentHp;
        hasSavedStats = true;

        Inventory.SaveForSceneChange(other.GetComponent<Inventory>());

        nextSpawn = targetSpawn;
        SceneManager.LoadScene(targetScene);
    }
}
