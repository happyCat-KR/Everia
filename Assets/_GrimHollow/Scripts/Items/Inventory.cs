using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어 가방 + 장비창: 아이템 칸, 골드, 칸별 장착 장비, 1번 키 포션
public class Inventory : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public ItemData item;
        public int count;
    }

    public int maxSlots = 8;
    public List<Slot> slots = new List<Slot>();
    public int gold;

    public ItemData quickPotion;            // 1번 키로 마실 포션
    public List<Slot> startingItems = new List<Slot>();
    public ItemData warriorStartWeapon;     // 직업별 처음 무기
    public ItemData mageStartWeapon;

    public Transform weaponSocket;          // 손의 무기 자리 (PlayerClass가 직업에 맞게 넣어 줌)
    public AudioClip[] pickupSounds;
    public AudioClip[] drinkSounds;

    public event Action Changed;            // UI 갱신용

    private readonly Dictionary<EquipSlot, ItemData> equipped = new Dictionary<EquipSlot, ItemData>();
    private PlayerStats stats;
    private PlayerController controller;
    private int baseAttackDamage;

    // 씬을 옮겨도 가방·장비를 유지하기 위한 보관함
    private static List<Slot> savedSlots;
    private static int savedGold;
    private static Dictionary<EquipSlot, ItemData> savedEquipped;

    public ItemData EquippedWeapon => GetEquipped(EquipSlot.Weapon);
    public int AttackDamage => controller != null ? controller.attackDamage : 0;

    void Start()
    {
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<PlayerController>();
        baseAttackDamage = controller.attackDamage;

        if (savedSlots != null)
        {
            slots = savedSlots;
            gold = savedGold;
            foreach (KeyValuePair<EquipSlot, ItemData> pair in savedEquipped)
            {
                SetEquipped(pair.Key, pair.Value);
            }
        }
        else
        {
            foreach (Slot s in startingItems)
            {
                Add(s.item, s.count, false);
            }

            ItemData startWeapon = PlayerClass.selectedClass == CharacterClass.Mage ? mageStartWeapon : warriorStartWeapon;
            if (startWeapon != null)
            {
                SetEquipped(EquipSlot.Weapon, startWeapon);
            }
        }

        Changed?.Invoke();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && quickPotion != null)
        {
            Use(quickPotion);
        }
    }

    // 새 게임 시작 시 이전 판 가방을 지움
    public static void ClearSaved()
    {
        savedSlots = null;
        savedEquipped = null;
        savedGold = 0;
    }

    public static void SaveForSceneChange(Inventory inventory)
    {
        savedSlots = inventory.slots;
        savedGold = inventory.gold;
        savedEquipped = new Dictionary<EquipSlot, ItemData>(inventory.equipped);
    }

    public ItemData GetEquipped(EquipSlot slot)
    {
        return equipped.TryGetValue(slot, out ItemData item) ? item : null;
    }

    public int Count(ItemData item)
    {
        Slot slot = slots.Find(s => s.item == item);
        return slot != null ? slot.count : 0;
    }

    public bool Add(ItemData item, int count = 1, bool playSound = true)
    {
        if (item.type == ItemType.Gold)
        {
            gold += item.goldAmount * count;
        }
        else
        {
            // 처음 줍는 장비이고 칸이 비어 있으면 바로 장착
            if (item.type == ItemType.Equipment && GetEquipped(item.slot) == null && item.CanUse(PlayerClass.selectedClass))
            {
                SetEquipped(item.slot, item);
            }
            else if (!AddToBag(item, count))
            {
                return false;       // 가방이 꽉 참
            }
        }

        if (playSound)
        {
            Sfx.Play(pickupSounds, 0.7f);
        }
        Changed?.Invoke();
        return true;
    }

    // 가방 칸 클릭: 포션은 마시고, 장비는 장착 (원래 끼던 건 가방으로)
    public void Use(ItemData item)
    {
        if (stats.isDead || Count(item) <= 0)
        {
            return;
        }

        if (item.type == ItemType.Consumable)
        {
            if (stats.currentHp >= stats.maxHp)
            {
                return;     // 체력이 가득이면 아깝게 쓰지 않음
            }

            stats.Heal(item.healAmount);
            Sfx.Play(drinkSounds, 0.8f);
            RemoveFromBag(item, 1);
        }
        else if (item.type == ItemType.Equipment && item.CanUse(PlayerClass.selectedClass))
        {
            RemoveFromBag(item, 1);
            ItemData previous = GetEquipped(item.slot);
            if (previous != null)
            {
                AddToBag(previous, 1);
            }
            SetEquipped(item.slot, item);
            Sfx.Play(pickupSounds, 0.5f);
        }

        Changed?.Invoke();
    }

    // 장비 칸 클릭: 벗어서 가방으로
    public void Unequip(EquipSlot slot)
    {
        ItemData item = GetEquipped(slot);
        if (item == null || slots.Count >= maxSlots)
        {
            return;
        }

        AddToBag(item, 1);
        SetEquipped(slot, null);
        Changed?.Invoke();
    }

    bool AddToBag(ItemData item, int count)
    {
        Slot slot = slots.Find(s => s.item == item);
        if (slot == null)
        {
            if (slots.Count >= maxSlots)
            {
                return false;
            }
            slot = new Slot { item = item };
            slots.Add(slot);
        }
        slot.count += count;
        return true;
    }

    void RemoveFromBag(ItemData item, int count)
    {
        Slot slot = slots.Find(s => s.item == item);
        slot.count -= count;
        if (slot.count <= 0)
        {
            slots.Remove(slot);
        }
    }

    void SetEquipped(EquipSlot slot, ItemData item)
    {
        if (item == null)
        {
            equipped.Remove(slot);
        }
        else
        {
            equipped[slot] = item;
        }

        // 공격력 = 기본 + 낀 장비 보너스 합
        int bonus = 0;
        foreach (ItemData e in equipped.Values)
        {
            bonus += e.attackBonus;
        }
        controller.attackDamage = baseAttackDamage + bonus;

        if (slot == EquipSlot.Weapon)
        {
            ShowWeapon(item);
        }
    }

    // 손에 든 모델 교체 (맨손이면 비움)
    void ShowWeapon(ItemData weapon)
    {
        if (weaponSocket == null)
        {
            return;
        }

        foreach (Transform child in weaponSocket)
        {
            Destroy(child.gameObject);
        }

        if (weapon != null && weapon.equipModel != null)
        {
            GameObject model = Instantiate(weapon.equipModel, weaponSocket);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
        }
    }
}
