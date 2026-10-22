using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 메뉴 GrimHollow > Build Dungeon Room
// 열린 씬에 사각형 던전 방을 만듦: 돌바닥, 벽, 기둥, 횃불, 상자, 소품 (MedievalVillage 2m 격자 조각 사용)
public static class DungeonBuilder
{
    const string Kit = "Assets/ThirdParty/Quaternius/MedievalVillage/FBX/";
    const string Props = "Assets/ThirdParty/Quaternius/FantasyProps/Exports/FBX/";
    const string FirePrefab = "Assets/_GrimHollow/Prefabs/VFX/VFX_TorchFire.prefab";

    public const int Tiles = 15;                // 한 변 타일 수 (15 × 2m = 30m)
    const float Half = Tiles;                   // 한 변의 절반 (m)

    [MenuItem("GrimHollow/Build Dungeon Room")]
    static void Build()
    {
        GameObject old = GameObject.Find("Dungeon_Room");
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old);
        }

        GameObject root = new GameObject("Dungeon_Room");
        Undo.RegisterCreatedObjectUndo(root, "Build Dungeon");

        Transform floor = Group(root.transform, "Floor");
        Transform walls = Group(root.transform, "Walls");
        Transform pillars = Group(root.transform, "Pillars");
        Transform torches = Group(root.transform, "Torches");
        Transform decor = Group(root.transform, "Decor");
        Transform colliders = Group(root.transform, "Colliders");

        BuildFloor(floor);
        BuildWalls(walls);
        BuildPillars(pillars);
        BuildTorches(torches);
        BuildDecor(decor);
        BuildColliders(colliders);
        BuildChests(decor);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("던전 방 생성 완료");
    }

    static Transform Group(Transform parent, string name)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        return g.transform;
    }

    static GameObject Load(string path)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    static GameObject Place(string path, Transform parent, Vector3 pos, float rotY, float scale = 1f)
    {
        GameObject asset = Load(path);
        if (asset == null)
        {
            Debug.LogWarning("조각을 찾을 수 없음: " + path);
            return null;
        }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f) * asset.transform.localRotation;    // FBX 기본 회전 유지
        go.transform.localScale = asset.transform.localScale * scale;
        go.isStatic = true;
        return go;
    }

    // 돌바닥: 두 종류 타일을 섞어서 단조롭지 않게
    static void BuildFloor(Transform parent)
    {
        System.Random rnd = new System.Random(3);
        for (int x = 0; x < Tiles; x++)
        {
            for (int z = 0; z < Tiles; z++)
            {
                Place(Kit + "Floor_Brick.fbx", parent, new Vector3(-Half + 1 + x * 2, 0.01f, -Half + 1 + z * 2), 0f);
            }
        }
    }

    // 사방 벽: 4칸(8m)씩 묶어서 따로 반투명해질 수 있게 함 (카메라 쪽 벽이 플레이어를 가릴 때)
    static void BuildWalls(Transform parent)
    {
        string[] sideNames = { "South", "North", "West", "East" };
        float[] rotations = { 0f, 180f, 90f, -90f };        // 집 조립과 같은 규칙

        for (int side = 0; side < 4; side++)
        {
            int segmentCount = 0;
            Transform segment = null;
            for (int i = 0; i < Tiles; i++)
            {
                if (i % 4 == 0)
                {
                    segment = Group(parent, sideNames[side] + "_" + segmentCount++);
                    segment.gameObject.AddComponent<Occludable>();
                }

                float along = -Half + 1 + i * 2;
                Vector3 pos;
                switch (side)
                {
                    case 0: pos = new Vector3(along, 0f, -Half); break;
                    case 1: pos = new Vector3(along, 0f, Half); break;
                    case 2: pos = new Vector3(-Half, 0f, along); break;
                    default: pos = new Vector3(Half, 0f, along); break;
                }

                // 남쪽 가운데는 입구 (문 모양 벽)
                bool door = side == 0 && i == Tiles / 2;
                Place(Kit + (door ? "Wall_UnevenBrick_Door_Round" : "Wall_UnevenBrick_Straight") + ".fbx", segment, pos, rotations[side]);
            }
        }
    }

    // 기둥 4개: 엄폐물이면서 보스 돌진이 박히는 벽
    static void BuildPillars(Transform parent)
    {
        GameObject wall = Load(Kit + "Wall_UnevenBrick_Straight.fbx");
        Material brick = wall.GetComponentInChildren<Renderer>().sharedMaterials[0];

        Vector2[] spots = { new Vector2(-7f, 0f), new Vector2(7f, 0f), new Vector2(-7f, 9f), new Vector2(7f, 9f) };
        foreach (Vector2 spot in spots)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Pillar";
            pillar.transform.SetParent(parent, false);
            pillar.transform.localPosition = new Vector3(spot.x, 1.9f, spot.y);
            pillar.transform.localScale = new Vector3(1.8f, 3.8f, 1.8f);
            pillar.GetComponent<Renderer>().sharedMaterial = brick;
            pillar.isStatic = true;
            pillar.AddComponent<Occludable>();
        }
    }

    // 횃불: 벽에 달린 조각 + 불꽃 이펙트와 점광원
    static void BuildTorches(Transform parent)
    {
        GameObject fire = Load(FirePrefab);

        // 위치와 안쪽을 보는 방향 (남쪽 벽은 입구가 있어서 양옆에만)
        float[] along = { -10f, -4f, 4f, 10f };
        foreach (float a in along)
        {
            AddTorch(parent, fire, new Vector3(a, 2.0f, -Half + 0.45f), 0f);
            AddTorch(parent, fire, new Vector3(a, 2.0f, Half - 0.45f), 180f);
            AddTorch(parent, fire, new Vector3(-Half + 0.45f, 2.0f, a), 90f);
            AddTorch(parent, fire, new Vector3(Half - 0.45f, 2.0f, a), -90f);
        }
    }

    static void AddTorch(Transform parent, GameObject fire, Vector3 pos, float rotY)
    {
        Place(Props + "Torch_Metal.fbx", parent, pos, rotY);
        if (fire != null)
        {
            GameObject f = (GameObject)PrefabUtility.InstantiatePrefab(fire, parent);
            f.transform.localPosition = pos + Vector3.up * 0.45f + Quaternion.Euler(0f, rotY, 0f) * Vector3.forward * 0.15f;
        }
    }

    // 분위기 소품: 통, 상자, 쇠사슬, 철창을 구석과 벽가에
    static void BuildDecor(Transform parent)
    {
        string[] names = { "Barrel", "Crate_Wooden", "Barrel", "Cage_Small", "Chain_Coil", "Barrel", "Crate_Wooden" };
        Vector3[] spots =
        {
            new Vector3(-13f, 0f, -13f), new Vector3(-13f, 0f, -11.5f), new Vector3(-11.6f, 0f, -13.2f),
            new Vector3(13f, 0f, -13f), new Vector3(12.6f, 0f, -11.3f), new Vector3(-13f, 0f, 1f),
            new Vector3(13.2f, 0f, 3f)
        };
        for (int i = 0; i < spots.Length; i++)
        {
            Place(Props + names[i % names.Length] + ".fbx", parent, spots[i], 20f * i);
        }
    }

    // 구석 상자 2개 (나중에 E 키로 열어 당근을 얻음)
    static void BuildChests(Transform parent)
    {
        Place(Props + "Chest_Wood.fbx", parent, new Vector3(-12.3f, 0f, 12.3f), 135f).name = "Chest_NW";
        Place(Props + "Chest_Wood.fbx", parent, new Vector3(12.3f, 0f, 12.3f), -135f).name = "Chest_NE";
    }

    // 충돌: 바닥(걷기, NavMesh용), 사방 벽, 기둥, 상자
    static void BuildColliders(Transform parent)
    {
        float span = Half * 2f + 2f;

        AddBox(parent, "FloorCollider", new Vector3(0f, -0.5f, 0f), new Vector3(span, 1f, span));
        AddBox(parent, "Wall_S", new Vector3(0f, 1.5f, -Half - 0.4f), new Vector3(span, 3f, 1.2f));
        AddBox(parent, "Wall_N", new Vector3(0f, 1.5f, Half + 0.4f), new Vector3(span, 3f, 1.2f));
        AddBox(parent, "Wall_W", new Vector3(-Half - 0.4f, 1.5f, 0f), new Vector3(1.2f, 3f, span));
        AddBox(parent, "Wall_E", new Vector3(Half + 0.4f, 1.5f, 0f), new Vector3(1.2f, 3f, span));

        // 기둥은 큐브라서 이미 Collider가 있음. 상자만 따로
        AddBox(parent, "Chest_NW_Col", new Vector3(-12.3f, 0.5f, 12.3f), new Vector3(1.3f, 1f, 1.3f));
        AddBox(parent, "Chest_NE_Col", new Vector3(12.3f, 0.5f, 12.3f), new Vector3(1.3f, 1f, 1.3f));
    }

    static void AddBox(Transform parent, string name, Vector3 center, Vector3 size)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = center;
        g.AddComponent<BoxCollider>().size = size;
        g.isStatic = true;
    }
}
