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

    void Update()
    {
        hpBar.value = (float)playerStats.currentHp / playerStats.maxHp;
        hpText.text = $"{playerStats.currentHp} / {playerStats.maxHp}";
        
        expBar.value = (float)playerStats.currentExp / playerStats.ExpToNextLevel;
        levelText.text = $"Lv.{playerStats.level}";

        expText.text = $"{playerStats.currentExp} / {playerStats.ExpToNextLevel}";
    }
}