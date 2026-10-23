using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면 아래 가운데 스킬 칸 (전사: Q 강철의 의지 / E 회전 베기 / 우클릭 돌진 베기). 쿨타임이 돌고, 버프 중에는 테두리가 빛남
public class SkillBar : MonoBehaviour
{
    class Slot
    {
        public PlayerController.SkillSlot id;
        public Image cooldown;
        public TMP_Text time;
        public Outline glow;
        public Color accent;
    }

    private PlayerController player;
    private readonly System.Collections.Generic.List<Slot> slots = new System.Collections.Generic.List<Slot>();
    private static Sprite whiteSprite;

    public static void Create(PlayerController player)
    {
        if (FindAnyObjectByType<SkillBar>() != null)
        {
            return;
        }

        // 마법사용 기존 Q 칸은 숨김 (전사는 새 스킬 바를 씀)
        PlayerHUD hud = FindAnyObjectByType<PlayerHUD>();
        if (hud != null && hud.skillCooldownFill != null)
        {
            hud.skillCooldownFill.transform.parent.gameObject.SetActive(false);
        }

        GameObject go = new GameObject("SkillBar");
        SkillBar bar = go.AddComponent<SkillBar>();
        bar.player = player;
        bar.Build();
    }

    void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/KoreanFont");

        GameObject row = new GameObject("Slots", typeof(RectTransform));
        row.transform.SetParent(transform, false);
        RectTransform rrt = row.GetComponent<RectTransform>();
        rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(0.5f, 0f);
        rrt.anchoredPosition = new Vector2(0f, 30f);
        rrt.sizeDelta = new Vector2(280f, 76f);

        AddSlot(row.transform, font, -92f, PlayerController.SkillSlot.IronWill, "Q", "강철의 의지", new Color(0.4f, 0.7f, 1f));
        AddSlot(row.transform, font, 0f, PlayerController.SkillSlot.Spin, "E", "회전 베기", new Color(1f, 0.6f, 0.25f));
        AddSlot(row.transform, font, 92f, PlayerController.SkillSlot.Charge, "R-CLICK", "돌진 베기", new Color(1f, 0.9f, 0.45f));
    }

    void AddSlot(Transform parent, TMP_FontAsset font, float x, PlayerController.SkillSlot id, string key, string label, Color accent)
    {
        GameObject root = NewUI("Slot_" + key, parent);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta = new Vector2(84f, 76f);
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.08f, 0.11f, 0.88f);
        bg.raycastTarget = false;
        Outline glow = root.AddComponent<Outline>();
        glow.effectDistance = new Vector2(3f, -3f);
        glow.effectColor = new Color(accent.r, accent.g, accent.b, 0f);

        // 위쪽 색 띠
        GameObject bar = NewUI("Accent", root.transform);
        RectTransform brt = bar.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 1f);
        brt.anchorMax = Vector2.one;
        brt.pivot = new Vector2(0.5f, 1f);
        brt.sizeDelta = new Vector2(0f, 5f);
        Image bi = bar.AddComponent<Image>();
        bi.color = accent;
        bi.raycastTarget = false;

        TMP_Text keyText = NewText("Key", root.transform, font, key, 26f, new Color(1f, 1f, 1f, 0.95f));
        keyText.rectTransform.anchorMin = new Vector2(0f, 0.35f);
        keyText.rectTransform.anchorMax = Vector2.one;
        keyText.rectTransform.offsetMin = new Vector2(2f, 0f);
        keyText.rectTransform.offsetMax = new Vector2(-2f, -6f);
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.enableAutoSizing = true;
        keyText.fontSizeMin = 14f;
        keyText.fontSizeMax = 30f;

        TMP_Text nameText = NewText("Name", root.transform, font, label, 16f, new Color(0.85f, 0.9f, 1f, 0.9f));
        nameText.rectTransform.anchorMin = Vector2.zero;
        nameText.rectTransform.anchorMax = new Vector2(1f, 0.35f);
        nameText.rectTransform.offsetMin = new Vector2(2f, 2f);
        nameText.rectTransform.offsetMax = new Vector2(-2f, 0f);
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 10f;
        nameText.fontSizeMax = 17f;

        // 쿨타임 덮개 (시계 방향으로 줄어듦)
        GameObject cd = NewUI("Cooldown", root.transform);
        RectTransform crt = cd.GetComponent<RectTransform>();
        crt.anchorMin = Vector2.zero;
        crt.anchorMax = Vector2.one;
        crt.offsetMin = crt.offsetMax = Vector2.zero;
        Image ci = cd.AddComponent<Image>();
        ci.sprite = WhiteSprite();
        ci.type = Image.Type.Filled;
        ci.fillMethod = Image.FillMethod.Radial360;
        ci.fillOrigin = (int)Image.Origin360.Top;
        ci.fillClockwise = false;
        ci.color = new Color(0f, 0f, 0f, 0.68f);
        ci.fillAmount = 0f;
        ci.raycastTarget = false;

        TMP_Text timeText = NewText("Time", root.transform, font, "", 32f, Color.white);
        timeText.rectTransform.anchorMin = Vector2.zero;
        timeText.rectTransform.anchorMax = Vector2.one;
        timeText.rectTransform.offsetMin = timeText.rectTransform.offsetMax = Vector2.zero;
        timeText.alignment = TextAlignmentOptions.Center;

        slots.Add(new Slot { id = id, cooldown = ci, time = timeText, glow = glow, accent = accent });
    }

    void Update()
    {
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        foreach (Slot s in slots)
        {
            float remaining = player.CooldownRemaining(s.id);
            float total = Mathf.Max(0.01f, player.CooldownTotal(s.id));
            s.cooldown.fillAmount = Mathf.Clamp01(remaining / total);
            s.time.text = remaining > 0.05f ? (remaining >= 10f ? Mathf.CeilToInt(remaining).ToString() : remaining.ToString("0.0")) : "";

            bool active = s.id == PlayerController.SkillSlot.IronWill && player.IronWillActive;
            float pulse = active ? 0.65f + 0.35f * Mathf.Sin(Time.time * 6f) : 0f;
            s.glow.effectColor = new Color(s.accent.r, s.accent.g, s.accent.b, pulse);
        }
    }

    static GameObject NewUI(string name, Transform parent)
    {
        GameObject g = new GameObject(name, typeof(RectTransform));
        g.transform.SetParent(parent, false);
        return g;
    }

    static TMP_Text NewText(string name, Transform parent, TMP_FontAsset font, string text, float size, Color color)
    {
        GameObject g = NewUI(name, parent);
        TextMeshProUGUI t = g.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            t.font = font;
        }
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    static Sprite WhiteSprite()
    {
        if (whiteSprite == null)
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] px = new Color[16];
            for (int i = 0; i < px.Length; i++)
            {
                px[i] = Color.white;
            }
            tex.SetPixels(px);
            tex.Apply();
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }
        return whiteSprite;
    }
}
