using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면 가운데 큰 글자 (WAVE 1, 보스 이름, DUNGEON CLEARED 등). UIBanner.Show(...)로 호출
public class UIBanner : MonoBehaviour
{
    private static UIBanner instance;

    private CanvasGroup group;
    private TMP_Text bigText;
    private TMP_Text smallText;
    private Coroutine running;

    public static void Show(string big, string small = "", float hold = 1.8f)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("UIBanner");
            instance = go.AddComponent<UIBanner>();
            instance.Build();
        }

        if (instance.running != null)
        {
            instance.StopCoroutine(instance.running);
        }
        instance.running = instance.StartCoroutine(instance.Play(big, small, hold));
    }

    void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/KoreanFont");

        GameObject root = new GameObject("Group", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(transform, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(0f, 260f);
        rt.anchoredPosition = new Vector2(0f, 180f);
        group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        // 글자 뒤 어두운 띠 (밝은 배경에서도 읽히게)
        GameObject band = new GameObject("Band", typeof(RectTransform), typeof(Image));
        band.transform.SetParent(root.transform, false);
        RectTransform brt = band.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        band.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        band.GetComponent<Image>().raycastTarget = false;

        bigText = MakeText(root.transform, "Big", font, 96f, new Vector2(0f, 25f), new Color(1f, 0.9f, 0.7f));
        smallText = MakeText(root.transform, "Small", font, 38f, new Vector2(0f, -65f), new Color(0.9f, 0.9f, 0.95f, 0.9f));
    }

    static TMP_Text MakeText(Transform parent, string name, TMP_FontAsset font, float size, Vector2 pos, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1600f, 130f);
        rt.anchoredPosition = pos;

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            t.font = font;
        }
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.characterSpacing = 8f;
        t.raycastTarget = false;
        return t;
    }

    IEnumerator Play(string big, string small, float hold)
    {
        bigText.text = big;
        smallText.text = small;

        yield return Fade(0f, 1f, 0.3f);
        yield return new WaitForSeconds(hold);
        yield return Fade(1f, 0f, 0.6f);
        running = null;
    }

    IEnumerator Fade(float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        group.alpha = to;
    }
}
