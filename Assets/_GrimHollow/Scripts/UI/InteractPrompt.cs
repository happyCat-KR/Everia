using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면 아래 "[E] 상자 열기" 같은 안내 (가까이 가면 뜸). 여러 개가 겹쳐도 가장 마지막 요청만 보여 줌
public class InteractPrompt : MonoBehaviour
{
    private static InteractPrompt instance;

    private CanvasGroup group;
    private TMP_Text label;
    private Object owner;

    public static void Show(Object requester, string text)
    {
        Ensure();
        instance.owner = requester;
        instance.label.text = text;
        instance.group.alpha = 1f;
    }

    public static void Hide(Object requester)
    {
        if (instance != null && instance.owner == requester)
        {
            instance.group.alpha = 0f;
            instance.owner = null;
        }
    }

    static void Ensure()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("InteractPrompt");
        instance = go.AddComponent<InteractPrompt>();
        instance.Build();
    }

    void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject root = new GameObject("Prompt", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        root.transform.SetParent(transform, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(420f, 70f);
        rt.anchoredPosition = new Vector2(0f, 190f);
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        root.GetComponent<Image>().raycastTarget = false;
        group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        GameObject text = new GameObject("Label", typeof(RectTransform));
        text.transform.SetParent(root.transform, false);
        RectTransform trt = text.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        TextMeshProUGUI t = text.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/KoreanFont");
        if (font != null)
        {
            t.font = font;
        }
        t.fontSize = 38f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        label = t;
    }
}
