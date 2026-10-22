using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어 가방: 아이템 칸, 골드, 장착 무기, 1번 키 포션
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
    public ItemData equippedWeapon;

    public ItemData quickPotion;            // 1번 키로 마실 포션
    public List<Slot> startingItems = new List<Slot>();

    public Transform weaponSocket;          // 손의 무기 자리 (hand_r 아래)
    public AudioClip[] pickupSounds;
    public AudioClip[] drinkSounds;

    public event Action Changed;            // UI 갱신용

    private PlayerStats stats;
    private PlayerController controller;
    private int baseAttackDamage;

    // 씬을 옮겨도 가방을 유지하기 위한 보관함
    private static List<Slot> savedSlots;
    private static int savedGold;
    private static ItemData savedWeapon;

    void Start()
    {
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<PlayerController>();
        baseAttackDamage = controller.attackDamage;

        if (savedSlots != null)
        {
            slots = savedSlots;
            gold = savedGold;
            if (savedWeapon != null)
            {
                Equip(savedWeapon);
            }
        }
        else
        {
            foreach (Slot s in startingItems)
            {
                Add(s.item, s.count, false);
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

    public static void SaveForSceneChange(Inventory inventory)
    {
        savedSlots = inventory.slots;
        savedGold = inventory.gold;
        savedWeapon = inventory.equippedWeapon;
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
            Slot slot = slots.Find(s => s.item == item);
            if (slot == null)
            {
                if (slots.Count >= maxSlots)
                {
                    return false;       // 가방이 꽉 참
                }
                slot = new Slot { item = item };
                slots.Add(slot);
            }
            slot.count += count;

            // 무기는 줍자마자 더 센 쪽으로 장착
            if (item.type == ItemType.Weapon && (equippedWeapon == null || item.attackBonus > equippedWeapon.attackBonus))
            {
                Equip(item);
            }
        }

        if (playSound)
        {
            Sfx.Play(pickupSounds, 0.7f);
        }
        Changed?.Invoke();
        return true;
    }

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
            Remove(item, 1);
        }
        else if (item.type == ItemType.Weapon)
        {
            Equip(item);
        }
    }

    void Remove(ItemData item, int count)
    {
        Slot slot = slots.Find(s => s.item == item);
        slot.count -= count;
        if (slot.count <= 0)
        {
            slots.Remove(slot);
        }
        Changed?.Invoke();
    }

    void Equip(ItemData weapon)
    {
        equippedWeapon = weapon;
        controller.attackDamage = baseAttackDamage + weapon.attackBonus;

        // 손에 든 모델 교체
        if (weaponSocket != null && weapon.equipModel != null)
        {
            foreach (Transform child in weaponSocket)
            {
                Destroy(child.gameObject);
            }
            GameObject model = Instantiate(weapon.equipModel, weaponSocket);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = weapon.equipModel.transform.localRotation;
        }

        Changed?.Invoke();
    }
}
