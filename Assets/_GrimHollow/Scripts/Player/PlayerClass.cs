using System;
using UnityEngine;

public enum CharacterClass { Warrior, Mage }
public enum CharacterGender { Male, Female }

// 시작 화면에서 고른 직업·성별에 맞게 외형과 공격 방식을 바꿈
// 외형 4개는 Player > Visual 아래에 미리 들어 있고, 고른 것 하나만 켬
[DefaultExecutionOrder(-50)]    // PlayerController·Inventory의 Start보다 먼저 외형을 정해야 함
public class PlayerClass : MonoBehaviour
{
    public static CharacterClass selectedClass = CharacterClass.Warrior;
    public static CharacterGender selectedGender = CharacterGender.Male;

    [Serializable]
    public class Look
    {
        public CharacterClass characterClass;
        public CharacterGender gender;
        public GameObject visual;           // 몸 + 옷 + 머리 + 무기
        public Transform weaponSocket;
    }

    [Serializable]
    public class ClassSettings
    {
        public bool isCaster;
        public float hitDelay = 0.3f;       // 클릭 후 데미지(또는 발사) 시점
        public int skillDamage = 25;
        public float skillRadius = 4f;
        public GameObject skillVfx;
        public GameObject skillDustVfx;
        public AudioClip[] attackSounds;
        public AudioClip[] skillSounds;
    }

    public Look[] looks;
    public ClassSettings warrior;
    public ClassSettings mage;
    public Projectile fireball;

    public bool applyOnAwake = true;        // 시작 화면 미리보기는 직접 Apply를 부름

    void Awake()
    {
        if (applyOnAwake)
        {
            Apply(selectedClass, selectedGender);
        }
    }

    public void Apply(CharacterClass characterClass, CharacterGender gender)
    {
        Look chosen = null;
        foreach (Look look in looks)
        {
            bool match = look.characterClass == characterClass && look.gender == gender;
            look.visual.SetActive(match);
            if (match)
            {
                chosen = look;
            }
        }

        Inventory inventory = GetComponent<Inventory>();
        if (inventory != null && chosen != null)
        {
            inventory.weaponSocket = chosen.weaponSocket;
        }

        PlayerController controller = GetComponent<PlayerController>();
        if (controller == null)
        {
            return;
        }

        ClassSettings s = characterClass == CharacterClass.Mage ? mage : warrior;
        controller.isCaster = s.isCaster;
        controller.hitDelay = s.hitDelay;
        controller.skillDamage = s.skillDamage;
        controller.skillRadius = s.skillRadius;
        controller.skillVfx = s.skillVfx;
        controller.skillDustVfx = s.skillDustVfx;
        controller.swingSounds = s.attackSounds;
        controller.skillSounds = s.skillSounds;
        controller.projectilePrefab = fireball;
    }
}
