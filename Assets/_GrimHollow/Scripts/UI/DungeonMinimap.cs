using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// 던전 미니맵: 오른쪽 위 작은 지도(플레이어 중심, 카메라 방향과 같이 돎) + M 키로 전체 지도
// 지도 그림은 시작할 때 NavMesh를 훑어서 만들고, 지나간 곳과 들어가 본 방만 드러남 (1픽셀 = 1m)
[DefaultExecutionOrder(100)]
public class DungeonMinimap : MonoBehaviour
{
    public float viewMeters = 44f;          // 작은 지도에 보이는 가로 범위(m)
    public float revealRadius = 8f;         // 걸어 다니면 이 반경이 드러남
    public float cameraYaw = 45f;           // CameraFollow.yaw 와 같게 (화면 위쪽 = 이 방향)
    public float smallSize = 260f;
    public float bigHeight = 760f;

    const int Pad = 40;                     // 작은 지도가 가장자리에서 안 깨지도록 여백

    static readonly Color Floor = new Color(0.22f, 0.27f, 0.38f, 0.92f);
    static readonly Color Edge = new Color(0.68f, 0.78f, 0.95f, 1f);
    static readonly Color BossFloor = new Color(0.42f, 0.16f, 0.16f, 0.92f);
    static readonly Color BossEdge = new Color(0.95f, 0.45f, 0.4f, 1f);
    static readonly Color TreasureFloor = new Color(0.42f, 0.34f, 0.12f, 0.92f);
    static readonly Color TreasureEdge = new Color(0.95f, 0.82f, 0.35f, 1f);

    private Transform player;
    private DungeonRoom[] rooms;
    private Texture2D texture;
    private Color32[] pixels;
    private bool[] walkable;
    private bool[] explored;
    private Color32[] floorColors;
    private Color32[] edgeColors;
    private int width, height;
    private float minX, minZ;
    private bool dirty;
    private float applyTimer;

    private RectTransform small;
    private RawImage smallImage;
    private RectTransform smallMarker;
    private TMP_Text roomLabel;
    private GameObject big;
    private RectTransform bigMap;
    private RectTransform bigMarker;
    private string lastRoomName;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        rooms = FindObjectsByType<DungeonRoom>();
        if (rooms.Length == 0)
        {
            enabled = false;
            return;
        }

        BuildTexture();
        BuildUI();
        Reveal(player.position, revealRadius);
        Apply();
    }

    // ───── 지도 그림 만들기 ─────
    void BuildTexture()
    {
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (DungeonRoom r in rooms)
        {
            Bounds b = r.Bounds;
            min = Vector2.Min(min, new Vector2(b.min.x, b.min.z));
            max = Vector2.Max(max, new Vector2(b.max.x, b.max.z));
        }

        minX = Mathf.Floor(min.x) - Pad;
        minZ = Mathf.Floor(min.y) - Pad;
        width = Mathf.CeilToInt(max.x - min.x) + Pad * 2;
        height = Mathf.CeilToInt(max.y - min.y) + Pad * 2;

        int count = width * height;
        walkable = new bool[count];
        explored = new bool[count];
        pixels = new Color32[count];
        floorColors = new Color32[count];
        edgeColors = new Color32[count];

        // 걸을 수 있는 곳 (NavMesh는 벽에서 반지름만큼 떨어져 있어서 넉넉히 잡음)
        for (int y = Pad; y < height - Pad; y++)
        {
            for (int x = Pad; x < width - Pad; x++)
            {
                Vector3 p = new Vector3(minX + x + 0.5f, 0f, minZ + y + 0.5f);
                walkable[y * width + x] = NavMesh.SamplePosition(p, out NavMeshHit hit, 1.1f, NavMesh.AllAreas);
            }
        }

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int i = y * width + x;
                if (!walkable[i])
                {
                    continue;
                }

                Color floor = Floor, edge = Edge;
                DungeonRoom room = RoomAt(minX + x + 0.5f, minZ + y + 0.5f);
                if (room != null && room.kind == DungeonRoom.Kind.Boss)
                {
                    floor = BossFloor; edge = BossEdge;
                }
                else if (room != null && room.kind == DungeonRoom.Kind.Treasure)
                {
                    floor = TreasureFloor; edge = TreasureEdge;
                }

                bool isEdge = !walkable[i - 1] || !walkable[i + 1] || !walkable[i - width] || !walkable[i + width];
                floorColors[i] = isEdge ? edge : floor;
            }
        }

        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
    }

    DungeonRoom RoomAt(float x, float z)
    {
        Vector3 p = new Vector3(x, 0f, z);
        foreach (DungeonRoom r in rooms)
        {
            if (r.Contains(p))
            {
                return r;
            }
        }
        return null;
    }

    // ───── UI ─────
    void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 작은 지도: 사각 틀 안에서 큰 그림이 플레이어를 따라 움직이고 카메라 방향에 맞춰 돎
        GameObject frame = NewUI("Minimap", transform);
        small = frame.GetComponent<RectTransform>();
        small.anchorMin = small.anchorMax = small.pivot = new Vector2(1f, 1f);
        small.sizeDelta = new Vector2(smallSize, smallSize);
        small.anchoredPosition = new Vector2(-30f, -30f);
        Image bg = frame.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.5f);
        bg.raycastTarget = false;
        frame.AddComponent<RectMask2D>();

        GameObject inner = NewUI("Map", frame.transform);
        RectTransform irt = inner.GetComponent<RectTransform>();
        float innerSize = smallSize * 1.5f;     // 45도 돌려도 모서리가 비지 않게 (√2 이상)
        irt.sizeDelta = new Vector2(innerSize, innerSize);
        irt.anchoredPosition = Vector2.zero;
        irt.localRotation = Quaternion.Euler(0f, 0f, cameraYaw);
        smallImage = inner.AddComponent<RawImage>();
        smallImage.texture = texture;
        smallImage.raycastTarget = false;

        smallMarker = NewMarker(frame.transform);

        // 방 이름
        GameObject label = NewUI("RoomLabel", transform);
        RectTransform lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = lrt.anchorMax = lrt.pivot = new Vector2(1f, 1f);
        lrt.sizeDelta = new Vector2(smallSize, 40f);
        lrt.anchoredPosition = new Vector2(-30f, -30f - smallSize - 4f);
        roomLabel = label.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/KoreanFont");
        if (font != null)
        {
            roomLabel.font = font;
        }
        roomLabel.fontSize = 28f;
        roomLabel.alignment = TextAlignmentOptions.Center;
        roomLabel.color = new Color(1f, 1f, 1f, 0.9f);
        roomLabel.raycastTarget = false;

        // 전체 지도 (M)
        big = NewUI("BigMap", transform);
        RectTransform brt = big.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        Image dim = big.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.72f);
        dim.raycastTarget = false;

        float dungeonW = width - Pad * 2, dungeonH = height - Pad * 2;
        float scale = bigHeight / dungeonH;
        GameObject map = NewUI("Map", big.transform);
        bigMap = map.GetComponent<RectTransform>();
        bigMap.sizeDelta = new Vector2(dungeonW * scale, dungeonH * scale);
        bigMap.anchoredPosition = Vector2.zero;
        RawImage bigImage = map.AddComponent<RawImage>();
        bigImage.texture = texture;
        bigImage.uvRect = new Rect((float)Pad / width, (float)Pad / height, dungeonW / width, dungeonH / height);
        bigImage.raycastTarget = false;
        bigMarker = NewMarker(map.transform);
        bigMarker.anchorMin = bigMarker.anchorMax = Vector2.zero;

        GameObject title = NewUI("Title", big.transform);
        RectTransform trt = title.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.sizeDelta = new Vector2(900f, 60f);
        trt.anchoredPosition = new Vector2(0f, -24f);
        TextMeshProUGUI t = title.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            t.font = font;
        }
        t.text = "던전 1층    [M] 닫기";
        t.fontSize = 40f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;

        big.SetActive(false);
    }

    static GameObject NewUI(string name, Transform parent)
    {
        GameObject g = new GameObject(name, typeof(RectTransform));
        g.transform.SetParent(parent, false);
        return g.GetComponent<RectTransform>().gameObject;
    }

    // 플레이어 표시: 점 + 바라보는 쪽을 가리키는 짧은 막대
    static RectTransform NewMarker(Transform parent)
    {
        GameObject root = NewUI("PlayerMarker", parent);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = Vector2.zero;

        GameObject dot = NewUI("Dot", root.transform);
        dot.GetComponent<RectTransform>().sizeDelta = new Vector2(12f, 12f);
        Image di = dot.AddComponent<Image>();
        di.color = new Color(1f, 0.95f, 0.4f);
        di.raycastTarget = false;

        GameObject nose = NewUI("Nose", root.transform);
        RectTransform nrt = nose.GetComponent<RectTransform>();
        nrt.sizeDelta = new Vector2(5f, 12f);
        nrt.anchoredPosition = new Vector2(0f, 11f);
        Image ni = nose.AddComponent<Image>();
        ni.color = new Color(1f, 0.95f, 0.4f);
        ni.raycastTarget = false;
        return rt;
    }

    // ───── 매 프레임 ─────
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            big.SetActive(!big.activeSelf);
            small.gameObject.SetActive(!big.activeSelf);
            roomLabel.gameObject.SetActive(!big.activeSelf);
        }

        Vector3 pos = player.position;
        Reveal(pos, revealRadius);

        // 들어가 본 방은 통째로 드러냄
        foreach (DungeonRoom r in rooms)
        {
            if (r.visited && !r.revealedOnMap)
            {
                r.revealedOnMap = true;
                RevealRoom(r);
            }
        }

        applyTimer -= Time.deltaTime;
        if (dirty && applyTimer <= 0f)
        {
            applyTimer = 0.1f;
            Apply();
        }

        // 작은 지도: 플레이어가 한가운데
        float u = (pos.x - minX) / width, v = (pos.z - minZ) / height;
        float innerMeters = viewMeters * 1.5f;
        smallImage.uvRect = new Rect(u - innerMeters / width * 0.5f, v - innerMeters / height * 0.5f, innerMeters / width, innerMeters / height);

        float yaw = player.eulerAngles.y;
        smallMarker.localRotation = Quaternion.Euler(0f, 0f, -(yaw - cameraYaw));

        if (big.activeSelf)
        {
            float dungeonW = width - Pad * 2, dungeonH = height - Pad * 2;
            bigMarker.anchoredPosition = new Vector2((pos.x - minX - Pad) / dungeonW * bigMap.sizeDelta.x, (pos.z - minZ - Pad) / dungeonH * bigMap.sizeDelta.y);
            bigMarker.localRotation = Quaternion.Euler(0f, 0f, -yaw);
        }

        UpdateRoomLabel(pos);
    }

    void UpdateRoomLabel(Vector3 pos)
    {
        string name = "복도";
        foreach (DungeonRoom r in rooms)
        {
            if (r.Contains(pos))
            {
                name = r.displayName;
                break;
            }
        }

        if (name != lastRoomName)
        {
            lastRoomName = name;
            roomLabel.text = name;
        }
    }

    // ───── 드러내기 ─────
    void Reveal(Vector3 world, float radius)
    {
        int cx = Mathf.FloorToInt(world.x - minX), cy = Mathf.FloorToInt(world.z - minZ);
        int r = Mathf.CeilToInt(radius);
        for (int y = cy - r; y <= cy + r; y++)
        {
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || y < 0 || x >= width || y >= height)
                {
                    continue;
                }

                int i = y * width + x;
                if (!explored[i] && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius)
                {
                    explored[i] = true;
                    dirty = true;
                }
            }
        }
    }

    void RevealRoom(DungeonRoom room)
    {
        Bounds b = room.Bounds;
        int x0 = Mathf.FloorToInt(b.min.x - minX) - 1, x1 = Mathf.CeilToInt(b.max.x - minX) + 1;
        int y0 = Mathf.FloorToInt(b.min.z - minZ) - 1, y1 = Mathf.CeilToInt(b.max.z - minZ) + 1;
        for (int y = Mathf.Max(0, y0); y < Mathf.Min(height, y1); y++)
        {
            for (int x = Mathf.Max(0, x0); x < Mathf.Min(width, x1); x++)
            {
                explored[y * width + x] = true;
            }
        }
        dirty = true;
    }

    void Apply()
    {
        dirty = false;
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = explored[i] && walkable[i] ? floorColors[i] : new Color32(0, 0, 0, 0);
        }
        texture.SetPixels32(pixels);
        texture.Apply(false);
    }

    void OnDestroy()
    {
        if (texture != null)
        {
            Destroy(texture);
        }
    }
}
