using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 시작 화면: 직업·성별을 고르면 미리보기 캐릭터가 바뀌고, 시작을 누르면 마을로
public class TitleMenu : MonoBehaviour
{
    public PlayerClass preview;             // 미리보기용 캐릭터 (게임 기능은 꺼 둠)
    public float previewTurnSpeed = 25f;    // 천천히 회전
    public string firstScene = "Town";

    public Button warriorButton, mageButton, maleButton, femaleButton, startButton;
    public Color selectedColor = new Color(1f, 0.75f, 0.3f);
    public Color normalColor = new Color(0.2f, 0.18f, 0.16f, 0.9f);

    void Start()
    {
        warriorButton.onClick.AddListener(() => Select(CharacterClass.Warrior, PlayerClass.selectedGender));
        mageButton.onClick.AddListener(() => Select(CharacterClass.Mage, PlayerClass.selectedGender));
        maleButton.onClick.AddListener(() => Select(PlayerClass.selectedClass, CharacterGender.Male));
        femaleButton.onClick.AddListener(() => Select(PlayerClass.selectedClass, CharacterGender.Female));
        startButton.onClick.AddListener(StartGame);

        Select(PlayerClass.selectedClass, PlayerClass.selectedGender);
    }

    void Update()
    {
        preview.transform.Rotate(Vector3.up, previewTurnSpeed * Time.deltaTime);
    }

    void Select(CharacterClass characterClass, CharacterGender gender)
    {
        PlayerClass.selectedClass = characterClass;
        PlayerClass.selectedGender = gender;
        preview.Apply(characterClass, gender);

        Highlight(warriorButton, characterClass == CharacterClass.Warrior);
        Highlight(mageButton, characterClass == CharacterClass.Mage);
        Highlight(maleButton, gender == CharacterGender.Male);
        Highlight(femaleButton, gender == CharacterGender.Female);
    }

    void Highlight(Button button, bool on)
    {
        button.GetComponent<Image>().color = on ? selectedColor : normalColor;
    }

    void StartGame()
    {
        // 새 게임: 이전 판의 씬 이동 정보는 지움
        ScenePortal.nextSpawn = null;
        ScenePortal.hasSavedStats = false;
        Inventory.ClearSaved();
        SceneManager.LoadScene(firstScene);
    }
}
