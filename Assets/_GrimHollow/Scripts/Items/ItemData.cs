using UnityEngine;

public enum ItemType
{
    Consumable,     // 포션 등 (사용하면 사라짐)
    Weapon,         // 장비 (주우면 바로 장착)
    Gold            // 골드 (가방에 안 들어가고 숫자만 늘어남)
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
    public int attackBonus;             // 무기: 공격력 증가
    public GameObject equipModel;       // 무기: 손에 들 모델
    public int goldAmount = 1;          // 골드: 1개당 금액
}
