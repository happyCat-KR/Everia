using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 가방 창(I 키) + HUD의 골드·포션 개수
public class InventoryUI : MonoBehaviour
{
    public Inventory inventory;

    public GameObject panel;            // I 키로 열고 닫는 창
    public Image[] slotIcons;
    public TMP_Text[] slotCounts;
    public TMP_Text weaponText;

    public TMP_Text goldText;           // HUD
    public TMP_Text potionCountText;    // HUD (1번 칸)

    void Start()
    {
        inventory.Changed += Refresh;
        panel.SetActive(false);
        Refresh();
    }

    void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.Changed -= Refresh;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            panel.SetActive(!panel.activeSelf);
        }
    }

    void Refresh()
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            bool has = i < inventory.slots.Count;
            slotIcons[i].enabled = has;
            slotCounts[i].text = has && inventory.slots[i].count > 1 ? inventory.slots[i].count.ToString() : "";
            if (has)
            {
                slotIcons[i].sprite = inventory.slots[i].item.icon;
            }
        }

        weaponText.text = inventory.equippedWeapon != null
            ? $"{inventory.equippedWeapon.displayName}  (ATK +{inventory.equippedWeapon.attackBonus})"
            : "Basic Sword";
        goldText.text = $"{inventory.gold} G";
        potionCountText.text = inventory.quickPotion != null ? inventory.Count(inventory.quickPotion).ToString() : "0";
    }
}
