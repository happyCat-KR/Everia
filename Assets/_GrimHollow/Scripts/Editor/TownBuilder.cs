using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 메뉴 GrimHollow > Build Town Buildings
// MedievalVillage 모듈 조각(2m 격자)으로 건물 프리팹을 만들고, Town 씬 그레이박스 자리에 배치
public static class TownBuilder
{
    const string Kit = "Assets/ThirdParty/Quaternius/MedievalVillage/FBX/";
    const string OutDir = "Assets/_GrimHollow/Prefabs/Buildings";

    const float WallHeight = 3f;

    // 문 조각은 기준점이 경첩 쪽이라 벽 가운데에서 이만큼 옮기고 키움 (House_Small에서 맞춘 값)
    static readonly Vector3 DoorOffset = new Vector3(0.624f, 0f, -0.182f);
    static readonly Vector3 DoorScale = new Vector3(1.13f, 1.1f, 1f);

    class Spec
    {
        public string name;         // 프리팹 이름
        public int cellsX;          // 가로 칸 수 (1칸 = 2m)
        public int cellsZ;          // 세로 칸 수
        public string wall;         // 기본 벽
        public string doorWall;     // 문 있는 벽
        public string windowWall;   // 창문 있는 벽
        public string greybox;      // 대신할 그레이박스 큐브 이름
        public float facingY;       // 문이 향할 방향 (0 = -Z, -90 = +X, 90 = -X)
    }

    static readonly Spec[] Specs =
    {
        new Spec { name = "Bld_Blacksmith", cellsX = 3, cellsZ = 3, wall = "Wall_UnevenBrick_Straight", doorWall = "Wall_UnevenBrick_Door_Flat", windowWall = "Wall_UnevenBrick_Window_Wide_Flat", greybox = "Blacksmith", facingY = -90f },
        new Spec { name = "Bld_Shop",       cellsX = 3, cellsZ = 3, wall = "Wall_Plaster_Straight",     doorWall = "Wall_Plaster_Door_Flat",     windowWall = "Wall_Plaster_Window_Wide_Flat",     greybox = "Shop",       facingY = 90f },
        new Spec { name = "Bld_Inn",        cellsX = 3, cellsZ = 4, wall = "Wall_Plaster_Straight",     doorWall = "Wall_Plaster_Door_Flat",     windowWall = "Wall_Plaster_Window_Wide_Flat",     greybox = "Inn",        facingY = -90f },
    };

    [MenuItem("GrimHollow/Build Town Buildings")]
    static void BuildAll()
    {
        if (!AssetDatabase.IsValidFolder(OutDir))
        {
            AssetDatabase.CreateFolder("Assets/_GrimHollow/Prefabs", "Buildings");
        }

        GameObject greyboxRoot = GameObject.Find("Greybox");
        GameObject buildingsRoot = GameObject.Find("Buildings");
        if (greyboxRoot != null && buildingsRoot == null)
        {
            buildingsRoot = new GameObject("Buildings");
            Undo.RegisterCreatedObjectUndo(buildingsRoot, "Build Town Buildings");
        }

        foreach (Spec spec in Specs)
        {
            GameObject prefab = BuildPrefab(spec);

            if (greyboxRoot != null)
            {
                PlaceInScene(spec, prefab, greyboxRoot.transform, buildingsRoot.transform);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("건물 생성 완료: " + OutDir);
    }

    static GameObject BuildPrefab(Spec spec)
    {
        GameObject root = new GameObject(spec.name);
        float halfX = spec.cellsX;      // 칸 수 × 2m ÷ 2
        float halfZ = spec.cellsZ;

        // 바닥
        for (int i = 0; i < spec.cellsX; i++)
        {
            for (int j = 0; j < spec.cellsZ; j++)
            {
                Add(root.transform, "Floor_WoodDark", new Vector3(-halfX + 1 + i * 2, 0f, -halfZ + 1 + j * 2), 0f);
            }
        }

        // 앞 (-Z): 가운데 칸에 문
        int doorIndex = (spec.cellsX - 1) / 2;
        for (int i = 0; i < spec.cellsX; i++)
        {
            Vector3 pos = new Vector3(-halfX + 1 + i * 2, 0f, -halfZ);
            if (i == doorIndex)
            {
                Add(root.transform, spec.doorWall, pos, 0f);
                Add(root.transform, "Door_1_Flat", pos + DoorOffset, 0f, DoorScale);
            }
            else
            {
                Add(root.transform, spec.wall, pos, 0f);
            }
        }

        // 뒤 (+Z)
        for (int i = 0; i < spec.cellsX; i++)
        {
            AddWallOrWindow(root.transform, spec, i == spec.cellsX / 2, new Vector3(-halfX + 1 + i * 2, 0f, halfZ), 180f);
        }

        // 왼쪽 (-X), 오른쪽 (+X)
        for (int j = 0; j < spec.cellsZ; j++)
        {
            float z = -halfZ + 1 + j * 2;
            bool window = j == spec.cellsZ / 2;
            AddWallOrWindow(root.transform, spec, window, new Vector3(-halfX, 0f, z), 90f);
            AddWallOrWindow(root.transform, spec, window, new Vector3(halfX, 0f, z), -90f);
        }

        // 모서리 기둥
        Add(root.transform, "Corner_Exterior_Wood", new Vector3(-halfX, 0f, -halfZ), 0f);
        Add(root.transform, "Corner_Exterior_Wood", new Vector3(halfX, 0f, -halfZ), 0f);
        Add(root.transform, "Corner_Exterior_Wood", new Vector3(-halfX, 0f, halfZ), 0f);
        Add(root.transform, "Corner_Exterior_Wood", new Vector3(halfX, 0f, halfZ), 0f);

        // 지붕: Roof_RoundTiles_(가로m)x(세로m)
        Add(root.transform, "Roof_RoundTiles_" + (spec.cellsX * 2) + "x" + (spec.cellsZ * 2), new Vector3(0f, WallHeight, 0f), 0f);

        // 지붕 앞뒤 삼각형 벽 (없으면 지붕 속이 뚫려 보임)
        string gable = "Roof_Front_Brick" + (spec.cellsX * 2);
        Add(root.transform, gable, new Vector3(0f, WallHeight, -halfZ), 0f);
        Add(root.transform, gable, new Vector3(0f, WallHeight, halfZ), 180f);

        ApplyBlueRoof(root);

        // 조각에 충돌이 없어서 건물 전체를 막는 상자 하나 (안에는 못 들어감)
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, WallHeight * 0.5f, 0f);
        box.size = new Vector3(halfX * 2, WallHeight, halfZ * 2);

        string path = OutDir + "/" + spec.name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // 레퍼런스처럼 파란 지붕: 기와 재질(MI_RoundTiles)만 파란 복사본으로 교체
    const string BlueRoofPath = "Assets/_GrimHollow/Materials/Town/M_Roof_Blue.mat";

    [MenuItem("GrimHollow/Apply Blue Roof To Selection")]
    static void ApplyBlueRoofToSelection()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            ApplyBlueRoof(go);
        }
    }

    static void ApplyBlueRoof(GameObject root)
    {
        Material blue = AssetDatabase.LoadAssetAtPath<Material>(BlueRoofPath);
        if (blue == null)
        {
            return;
        }

        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null && mats[i].name == "MI_RoundTiles")
                {
                    mats[i] = blue;
                    changed = true;
                }
            }

            if (changed)
            {
                Undo.RecordObject(r, "Blue Roof");
                r.sharedMaterials = mats;
            }
        }
    }

    static void AddWallOrWindow(Transform parent, Spec spec, bool window, Vector3 pos, float rotY)
    {
        if (window)
        {
            Add(parent, spec.windowWall, pos, rotY);
            Add(parent, "Window_Wide_Flat1", pos, rotY);
        }
        else
        {
            Add(parent, spec.wall, pos, rotY);
        }
    }

    static void Add(Transform parent, string piece, Vector3 pos, float rotY, Vector3? scaleMul = null)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + piece + ".fbx");
        if (asset == null)
        {
            Debug.LogWarning("조각을 찾을 수 없음: " + piece);
            return;
        }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f) * asset.transform.localRotation;   // FBX 기본 회전(-90 X) 유지
        if (scaleMul.HasValue)
        {
            go.transform.localScale = Vector3.Scale(asset.transform.localScale, scaleMul.Value);
        }
    }

    // 그레이박스 큐브를 끄고 그 자리에 건물 배치 (다시 실행하면 예전 건물은 지우고 새로)
    static void PlaceInScene(Spec spec, GameObject prefab, Transform greyboxRoot, Transform buildingsRoot)
    {
        Transform cube = greyboxRoot.Find(spec.greybox);
        if (cube == null)
        {
            Debug.LogWarning("그레이박스를 찾을 수 없음: " + spec.greybox);
            return;
        }

        Transform old = buildingsRoot.Find(spec.name);
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old.gameObject);
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, buildingsRoot);
        Undo.RegisterCreatedObjectUndo(instance, "Build Town Buildings");
        instance.transform.position = new Vector3(cube.position.x, 0f, cube.position.z);
        instance.transform.rotation = Quaternion.Euler(0f, spec.facingY, 0f);

        Undo.RecordObject(cube.gameObject, "Build Town Buildings");
        cube.gameObject.SetActive(false);
    }
}
