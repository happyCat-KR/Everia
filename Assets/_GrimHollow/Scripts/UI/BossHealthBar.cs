using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면 위쪽 보스 체력 바: 보스가 깨어나면 나타나고, 빈틈일 땐 푸르게 변하고, 죽으면 사라짐
public class BossHealthBar : MonoBehaviour
{
    public BossController boss;
    public MonsterHealth health;
    public CanvasGroup group;
    public Slider slider;
    public Image fill;
    public TMP_Text nameText;

    public Color normalColor = new Color(0.8f, 0.12f, 0.1f);
    public Color vulnerableColor = new Color(0.45f, 0.75f, 1f);
    public float fadeSpeed = 3f;

    private float deadTimer;

    void Start()
    {
        nameText.text = boss.bossName.ToUpper();
        group.alpha = 0f;
    }

    void Update()
    {
        if (boss == null)
        {
            group.alpha = 0f;
            return;
        }

        bool show = boss.IsAwake && !health.isDead;
        if (health.isDead)
        {
            // 죽은 뒤 잠깐 보였다가 사라짐
            deadTimer += Time.deltaTime;
            show = deadTimer < 1.5f;
        }

        group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, fadeSpeed * Time.deltaTime);
        slider.value = (float)health.currentHp / health.maxHp;
        fill.color = health.IsVulnerable ? vulnerableColor : normalColor;
    }
}
