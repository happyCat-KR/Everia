using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 장비창 + 가방(I 키), HUD의 골드·포션 개수
public class InventoryUI : MonoBehaviour
{
    [Serializable]
    public class EquipSlotView
    {
        public EquipSlot slot;
        public Button button;
        public Image icon;
    }

    public Inventory inventory;
    public PlayerStats playerStats;

    public GameObject panel;            // I 키로 열고 닫는 창
    public Button[] bagButtons;
    public Image[] bagIcons;
    public TMP_Text[] bagCounts;
    public EquipSlotView[] equipSlots;
    public TMP_Text statsText;          // 레벨, 공격력, 체력
    public TMP_Text tooltipText;        // 마지막으로 가리킨 아이템 설명

    public TMP_Text goldText;           // HUD
    public TMP_Text potionCountText;    // HUD (1번 칸)

    public static bool IsOpen { get; private set; }     // 열려 있으면 클릭 공격을 막음

    void Start()
    {
        inventory.Changed += Refresh;

        for (int i = 0; i < bagButtons.Length; i++)
        {
            int index = i;      // 람다가 반복 변수를 잡지 않게 복사
            bagButtons[i].onClick.AddListener(() => OnBagClicked(index));
        }
        foreach (EquipSlotView view in equipSlots)
        {
            EquipSlot slot = view.slot;
            view.button.onClick.AddListener(() => inventory.Unequip(slot));
        }

        SetOpen(false);
        Refresh();
    }

    void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.Changed -= Refresh;
        }
        IsOpen = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            SetOpen(!panel.activeSelf);
        }

        // 체력·레벨은 전투 중에 바뀌니 열려 있을 때 계속 갱신
        if (IsOpen)
        {
            statsText.text = $"Lv.{playerStats.level}\nATK  {inventory.AttackDamage}\nHP  {playerStats.currentHp} / {playerStats.maxHp}";
        }
    }

    void SetOpen(bool open)
    {
        panel.SetActive(open);
        IsOpen = open;
    }

    void OnBagClicked(int index)
    {
        if (index >= inventory.slots.Count)
        {
            return;
        }

        ItemData item = inventory.slots[index].item;
        if (item.type == ItemType.Equipment && !item.CanUse(PlayerClass.selectedClass))
        {
            tooltipText.text = $"{item.displayName}: {item.usableBy} only";
            return;
        }

        inventory.Use(item);
    }

    void Refresh()
    {
        for (int i = 0; i < bagIcons.Length; i++)
        {
            bool has = i < inventory.slots.Count;
            bagIcons[i].enabled = has;
            bagCounts[i].text = has && inventory.slots[i].count > 1 ? inventory.slots[i].count.ToString() : "";
            if (has)
            {
                bagIcons[i].sprite = inventory.slots[i].item.icon;
            }
        }

        foreach (EquipSlotView view in equipSlots)
        {
            ItemData item = inventory.GetEquipped(view.slot);
            view.icon.enabled = item != null;
            if (item != null)
            {
                view.icon.sprite = item.icon;
            }
        }

        ItemData weapon = inventory.EquippedWeapon;
        tooltipText.text = weapon != null ? $"{weapon.displayName}  (ATK +{weapon.attackBonus})" : "No weapon";
        goldText.text = $"{inventory.gold} G";
        potionCountText.text = inventory.quickPotion != null ? inventory.Count(inventory.quickPotion).ToString() : "0";
    }
}
