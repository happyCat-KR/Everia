using UnityEditor;
using UnityEditor.SceneManagement;

// 에디터에서 Play를 누르면 열린 씬과 상관없이 Title부터 시작 (실제 게임과 같은 흐름)
// 메뉴 GrimHollow > Play From Title 로 켜고 끔 (특정 씬만 테스트할 때 끄기)
[InitializeOnLoad]
public static class PlayFromTitle
{
    const string ScenePath = "Assets/_GrimHollow/Scenes/Title.unity";
    const string PrefKey = "GrimHollow.PlayFromTitle";
    const string MenuPath = "GrimHollow/Play From Title";

    static PlayFromTitle()
    {
        // 메뉴 체크 상태는 에디터 로드 후에 설정해야 반영됨
        EditorApplication.delayCall += Apply;
    }

    static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        Enabled = !Enabled;
        Apply();
    }

    static void Apply()
    {
        EditorSceneManager.playModeStartScene = Enabled ? AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) : null;
        Menu.SetChecked(MenuPath, Enabled);
    }
}
