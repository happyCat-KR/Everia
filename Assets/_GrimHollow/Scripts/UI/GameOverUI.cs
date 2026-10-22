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
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}