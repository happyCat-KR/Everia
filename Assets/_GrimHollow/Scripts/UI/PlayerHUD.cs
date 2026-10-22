using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    public PlayerStats playerStats;
    public Slider hpBar;
    public TMP_Text hpText;

    public Slider expBar;
    public TMP_Text levelText;
    public TMP_Text expText;

    public Slider mpBar;
    public TMP_Text mpText;

    public Slider staminaBar;           // 대시·전사 공격에 쓰는 초록 바

    public PlayerController playerController;
    public Image skillCooldownFill;     // Q 아이콘 위 어두운 덮개 (쿨타임만큼 남음)

    void Update()
    {
        hpBar.value = (float)playerStats.currentHp / playerStats.maxHp;
        hpText.text = $"{playerStats.currentHp} / {playerStats.maxHp}";
        
        expBar.value = (float)playerStats.currentExp / playerStats.ExpToNextLevel;
        levelText.text = $"Lv.{playerStats.level}";

        expText.text = $"{playerStats.currentExp} / {playerStats.ExpToNextLevel}";

        if (staminaBar != null)
        {
            staminaBar.value = playerStats.currentStamina / playerStats.maxStamina;
        }

        if (mpBar != null)
        {
            mpBar.value = playerStats.currentMp / playerStats.maxMp;
            mpText.text = $"{Mathf.FloorToInt(playerStats.currentMp)} / {playerStats.maxMp}";
        }

        if (skillCooldownFill != null)
        {
            skillCooldownFill.fillAmount = Mathf.Clamp01(playerController.SkillCooldownRatio);
        }
    }
}