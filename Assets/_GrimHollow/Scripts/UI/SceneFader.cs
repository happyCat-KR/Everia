using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 씬 이동: 화면이 검게 페이드 아웃 → 씬 로드 → 장소 이름(예: "던전 1층")을 보여 주고 → 페이드 인
// 씬이 갑자기 끊겨 보이지 않게 하는 용도. SceneFader.GoTo(...) 한 줄로 씀
public class SceneFader : MonoBehaviour
{
    private static SceneFader instance;

    public float fadeOutTime = 0.5f;
    public float titleHold = 1.5f;
    public float fadeInTime = 0.7f;

    private CanvasGroup blackGroup;
    private CanvasGroup titleGroup;
    private TMP_Text titleText;
    private TMP_Text subtitleText;
    private bool busy;

    public static void GoTo(string sceneName, string title = null, string subtitle = null)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("SceneFader");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneFader>();
            instance.Build();
        }

        if (instance.busy)
        {
            return;
        }
        instance.StartCoroutine(instance.Run(sceneName, title, subtitle));
    }

    void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/KoreanFont");

        // 검은 화면
        GameObject black = new GameObject("Black", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        black.transform.SetParent(transform, false);
        Stretch(black.GetComponent<RectTransform>());
        black.GetComponent<Image>().color = Color.black;
        blackGroup = black.GetComponent<CanvasGroup>();
        blackGroup.alpha = 0f;
        blackGroup.blocksRaycasts = false;

        // 장소 이름
        GameObject titleRoot = new GameObject("Title", typeof(RectTransform), typeof(CanvasGroup));
        titleRoot.transform.SetParent(transform, false);
        Stretch(titleRoot.GetComponent<RectTransform>());
        titleGroup = titleRoot.GetComponent<CanvasGroup>();
        titleGroup.alpha = 0f;
        titleGroup.blocksRaycasts = false;

        titleText = MakeText(titleRoot.transform, "Name", font, 110f, new Vector2(0f, 40f), new Color(1f, 0.93f, 0.78f));
        subtitleText = MakeText(titleRoot.transform, "Sub", font, 44f, new Vector2(0f, -70f), new Color(0.85f, 0.85f, 0.9f, 0.9f));
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static TMP_Text MakeText(Transform parent, string name, TMP_FontAsset font, float size, Vector2 pos, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1600f, 160f);
        rt.anchoredPosition = pos;

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            t.font = font;
        }
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.characterSpacing = 6f;
        t.raycastTarget = false;
        return t;
    }

    IEnumerator Run(string sceneName, string title, string subtitle)
    {
        busy = true;
        blackGroup.blocksRaycasts = true;     // 이동 중 버튼이 눌리지 않게

        yield return Fade(blackGroup, 0f, 1f, fadeOutTime);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        while (!load.isDone)
        {
            yield return null;
        }
        yield return null;      // 새 씬 첫 프레임이 그려지는 동안 검은 화면 유지

        if (!string.IsNullOrEmpty(title))
        {
            titleText.text = title;
            subtitleText.text = subtitle ?? "";
            yield return Fade(titleGroup, 0f, 1f, 0.5f);
            yield return new WaitForSecondsRealtime(titleHold);
            StartCoroutine(Fade(titleGroup, 1f, 0f, 0.5f));
        }

        yield return Fade(blackGroup, 1f, 0f, fadeInTime);
        blackGroup.blocksRaycasts = false;
        busy = false;
    }

    static IEnumerator Fade(CanvasGroup group, float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        group.alpha = to;
    }
}
