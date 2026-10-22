using UnityEngine;

public enum ItemType
{
    Consumable,     // 포션 등 (사용하면 사라짐)
    Equipment,      // 장비 (장비창 칸에 낌)
    Gold            // 골드 (가방에 안 들어가고 숫자만 늘어남)
}

public enum EquipSlot
{
    None,
    Weapon,
    OffHand,
    Head,
    Body,
    Feet
}

public enum ClassRestriction
{
    Any,
    Warrior,
    Mage
}

// 아이템 한 종류의 정보 (Project 창 우클릭 → Create → Everia → Item)
[CreateAssetMenu(menuName = "Everia/Item")]
public class ItemData : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    public ItemType type;
    public GameObject worldModel;       // 바닥에 떨어졌을 때 보이는 모델

    public int healAmount;              // 소비템: 회복량

    public EquipSlot slot;              // 장비: 끼는 칸
    public ClassRestriction usableBy;   // 장비: 쓸 수 있는 직업
    public int attackBonus;             // 장비: 공격력 증가
    public GameObject equipModel;       // 무기: 손에 들 모델

    public int goldAmount = 1;          // 골드: 1개당 금액

    public bool CanUse(CharacterClass characterClass)
    {
        return usableBy == ClassRestriction.Any
            || (usableBy == ClassRestriction.Warrior && characterClass == CharacterClass.Warrior)
            || (usableBy == ClassRestriction.Mage && characterClass == CharacterClass.Mage);
    }
}
