using System;
using UnityEngine;

// 몬스터가 죽을 때 떨어뜨리는 아이템 목록 (MonsterHealth가 Die에서 호출)
public class LootTable : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public ItemData item;
        [Range(0f, 1f)] public float chance = 0.5f;
        public int minCount = 1;
        public int maxCount = 1;
    }

    public Entry[] entries;

    public void Drop()
    {
        foreach (Entry e in entries)
        {
            if (e.item != null && UnityEngine.Random.value <= e.chance)
            {
                int count = UnityEngine.Random.Range(e.minCount, e.maxCount + 1);

                // 골드는 한 덩어리, 나머지는 개수만큼 따로 떨어뜨려서 풍성하게
                if (e.item.type == ItemType.Gold)
                {
                    ItemPickup.Spawn(e.item, count, transform.position);
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        ItemPickup.Spawn(e.item, 1, transform.position);
                    }
                }
            }
        }
    }
}
