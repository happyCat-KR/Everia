using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public PlayerStats playerStats;
    public GameObject gameOverPanel;
    public float showDelay = 2f;    // 쓰러지는 모션을 보여줄 시간

    private float deadTimer;

    void Start()
    {
        gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if(playerStats.isDead && !gameOverPanel.activeSelf)
        {
            deadTimer += Time.deltaTime;
            if(deadTimer >= showDelay)
            {
                gameOverPanel.SetActive(true);
            }
        }
    }

    // 버튼에서 호출
    // 죽으면 마을에서 처음부터 (레벨·가방도 초기화: 던전을 한 번에 못 깨는 도전 구조)
    public void Restart()
    {
        ScenePortal.nextSpawn = null;
        ScenePortal.hasSavedStats = false;
        Inventory.ClearSaved();
        SceneFader.GoTo("Town", "마을", "다시 도전");
    }
}