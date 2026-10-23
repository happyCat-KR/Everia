using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 메뉴 GrimHollow > Build Dungeon Room
// 열린 씬에 던전 1층(방 여러 개 + 복도)을 만듦: 돌바닥, 벽, 기둥, 횃불, 상자, 소품 (MedievalVillage 2m 격자 조각 사용)
// 맵은 아래 Rooms/Corridors 사각형 목록(타일 단위, 1타일 = 2m)만 고치면 바뀜
public static class DungeonBuilder
{
    const string Kit = "Assets/ThirdParty/Quaternius/MedievalVillage/FBX/";
    const string Props = "Assets/ThirdParty/Quaternius/FantasyProps/Exports/FBX/";
    const string FirePrefab = "Assets/_GrimHollow/Prefabs/VFX/VFX_TorchFire.prefab";

    const float Tile = 2f;

    enum Pillars { None, Corners, Colonnade, Ring, Grid, Altar, Arena }

    struct RoomDef
    {
        public string name;
        public DungeonRoom.Kind kind;
        public int x, z, w, h;      // 왼쪽 아래 타일과 크기 (타일)
        public Pillars pillars;

        public RoomDef(string name, DungeonRoom.Kind kind, int x, int z, int w, int h, Pillars pillars)
        {
            this.name = name; this.kind = kind; this.x = x; this.z = z; this.w = w; this.h = h; this.pillars = pillars;
        }
    }

    static readonly RoomDef[] Rooms =
    {
        new RoomDef("입구 방",     DungeonRoom.Kind.Start,    -6, -18, 12, 10, Pillars.Corners),
        new RoomDef("중앙 홀",     DungeonRoom.Kind.Normal,  -10,  -2, 20, 14, Pillars.Colonnade),
        new RoomDef("서쪽 방",     DungeonRoom.Kind.Normal,  -30,   0, 12, 12, Pillars.Ring),
        new RoomDef("동쪽 방",     DungeonRoom.Kind.Normal,   14,  -2, 12, 12, Pillars.Grid),
        new RoomDef("북서쪽 방",   DungeonRoom.Kind.Normal,  -30,  16, 12, 12, Pillars.Corners),
        new RoomDef("북쪽 홀",     DungeonRoom.Kind.Normal,   -8,  16, 16, 12, Pillars.Colonnade),
        new RoomDef("북동쪽 방",   DungeonRoom.Kind.Normal,   14,  16, 12, 12, Pillars.Ring),
        new RoomDef("보물 방",     DungeonRoom.Kind.Treasure, 14, -14,  8,  8, Pillars.Altar),
        new RoomDef("남서쪽 방",   DungeonRoom.Kind.Normal,  -30, -14, 12, 10, Pillars.Grid),
        new RoomDef("보스 방",     DungeonRoom.Kind.Boss,    -12,  34, 24, 18, Pillars.Arena),
    };

    // 복도 (방과 맞닿게 놓음)
    static readonly int[][] Corridors =
    {
        new[] { -1, -8, 2, 6 },     // 입구 방 → 중앙 홀
        new[] { -18, 4, 8, 2 },     // 중앙 홀 → 서쪽 방
        new[] { 10, 2, 4, 2 },      // 중앙 홀 → 동쪽 방
        new[] { -26, 12, 2, 4 },    // 서쪽 방 → 북서쪽 방
        new[] { -1, 12, 2, 4 },     // 중앙 홀 → 북쪽 홀
        new[] { 8, 20, 6, 2 },      // 북쪽 홀 → 북동쪽 방
        new[] { 19, 10, 2, 6 },     // 동쪽 방 → 북동쪽 방
        new[] { -18, 20, 10, 2 },   // 북서쪽 방 → 북쪽 홀 (고리)
        new[] { 17, -6, 2, 4 },     // 동쪽 방 → 보물 방
        new[] { -26, -4, 2, 4 },    // 서쪽 방 → 남서쪽 방
        new[] { -18, -12, 12, 2 },  // 남서쪽 방 → 입구 방 (고리)
        new[] { 0, 28, 1, 6 },      // 북쪽 홀 → 보스 방 (좁은 길, 철문이 서는 곳)
    };

    // 입구 방 남쪽 벽의 출구 타일 (문 모양 벽)
    static readonly Vector2Int ExitTile = new Vector2Int(0, -18);

    // 상자를 놓을 방 이름 (같은 방에 여러 개면 구석을 번갈아)
    static readonly string[] ChestRooms = { "보물 방", "보물 방", "북서쪽 방", "북동쪽 방", "서쪽 방", "남서쪽 방", "동쪽 방" };

    static readonly int[] DirX = { 0, 0, -1, 1 };       // 남, 북, 서, 동
    static readonly int[] DirZ = { -1, 1, 0, 0 };
    static readonly float[] DirRot = { 0f, 180f, 90f, -90f };   // 집 조립과 같은 규칙

    [MenuItem("GrimHollow/Build Dungeon Room")]
    static void Build()
    {
        Chest chestTemplate = null;
        GameObject old = GameObject.Find("Dungeon_Room");
        if (old != null)
        {
            chestTemplate = old.GetComponentInChildren<Chest>();
            if (chestTemplate != null)
            {
                // 지우기 전에 설정(내용물, 이펙트, 소리)을 복사해 둠
                GameObject keep = new GameObject("ChestTemplate");
                keep.hideFlags = HideFlags.HideAndDontSave;
                Chest c = keep.AddComponent<Chest>();
                c.contents = chestTemplate.contents;
                c.counts = chestTemplate.counts;
                c.openVfx = chestTemplate.openVfx;
                c.openSounds = chestTemplate.openSounds;
                c.range = chestTemplate.range;
                c.prompt = chestTemplate.prompt;
                chestTemplate = c;
            }
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
        Transform rooms = Group(root.transform, "Rooms");

        HashSet<Vector2Int> tiles = CollectTiles();

        BuildFloor(floor, tiles);
        BuildWalls(walls, torches, colliders, tiles);
        BuildFloorColliders(colliders);
        BuildPillars(pillars);
        BuildRoomMarkers(rooms);
        BuildDecor(decor);
        BuildChests(decor, colliders, chestTemplate);

        if (chestTemplate != null)
        {
            Object.DestroyImmediate(chestTemplate.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("던전 1층 생성 완료: 타일 " + tiles.Count + "개");
    }

    // ───── 도우미 ─────
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

    static Vector3 TileCenter(int tx, int tz)
    {
        return new Vector3(tx * Tile + Tile * 0.5f, 0f, tz * Tile + Tile * 0.5f);
    }

    static Vector3 RectCenter(int x, int z, int w, int h)
    {
        return new Vector3((x + w * 0.5f) * Tile, 0f, (z + h * 0.5f) * Tile);
    }

    static HashSet<Vector2Int> CollectTiles()
    {
        HashSet<Vector2Int> tiles = new HashSet<Vector2Int>();
        foreach (RoomDef r in Rooms)
        {
            AddRect(tiles, r.x, r.z, r.w, r.h);
        }
        foreach (int[] c in Corridors)
        {
            AddRect(tiles, c[0], c[1], c[2], c[3]);
        }
        return tiles;
    }

    static void AddRect(HashSet<Vector2Int> tiles, int x, int z, int w, int h)
    {
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                tiles.Add(new Vector2Int(x + i, z + j));
            }
        }
    }

    // ───── 바닥 ─────
    static void BuildFloor(Transform parent, HashSet<Vector2Int> tiles)
    {
        foreach (Vector2Int t in tiles)
        {
            Vector3 c = TileCenter(t.x, t.y);
            Place(Kit + "Floor_Brick.fbx", parent, new Vector3(c.x, 0.01f, c.z), 0f);
        }
    }

    // ───── 벽 + 벽 충돌 + 횃불 ─────
    // 바닥 타일 옆이 바닥이 아니면 그 변에 벽. 같은 방향으로 이어진 벽 4칸씩 묶어서 따로 반투명해질 수 있게 함
    static void BuildWalls(Transform parent, Transform torchParent, Transform colliderParent, HashSet<Vector2Int> tiles)
    {
        GameObject fire = Load(FirePrefab);
        string[] sideNames = { "South", "North", "West", "East" };

        for (int dir = 0; dir < 4; dir++)
        {
            // 이 방향에 벽이 필요한 타일. 남/북은 같은 z끼리 x순, 서/동은 같은 x끼리 z순으로 훑음
            List<Vector2Int> edges = new List<Vector2Int>();
            foreach (Vector2Int t in tiles)
            {
                if (!tiles.Contains(new Vector2Int(t.x + DirX[dir], t.y + DirZ[dir])))
                {
                    edges.Add(t);
                }
            }

            bool horizontal = dir < 2;      // 남/북 벽은 x 방향으로 늘어섬
            edges.Sort((a, b) =>
            {
                int lineA = horizontal ? a.y : a.x, lineB = horizontal ? b.y : b.x;
                if (lineA != lineB) return lineA.CompareTo(lineB);
                return (horizontal ? a.x : a.y).CompareTo(horizontal ? b.x : b.y);
            });

            Transform segment = null;
            int segmentCount = 0, runLength = 0, prevLine = int.MinValue, prevAlong = int.MinValue;
            foreach (Vector2Int t in edges)
            {
                int line = horizontal ? t.y : t.x;
                int along = horizontal ? t.x : t.y;

                bool continuesRun = line == prevLine && along == prevAlong + 1;
                if (!continuesRun || runLength >= 4)
                {
                    segment = Group(parent, sideNames[dir] + "_" + segmentCount++);
                    segment.gameObject.AddComponent<Occludable>();
                    runLength = 0;
                }
                runLength++;
                prevLine = line;
                prevAlong = along;

                Vector3 c = TileCenter(t.x, t.y);
                Vector3 pos = c + new Vector3(DirX[dir], 0f, DirZ[dir]) * (Tile * 0.5f);
                bool door = dir == 0 && t == ExitTile;
                Place(Kit + (door ? "Wall_UnevenBrick_Door_Round" : "Wall_UnevenBrick_Straight") + ".fbx", segment, pos, DirRot[dir]);

                // 충돌: 벽 바깥쪽으로 두껍게
                Vector3 outward = new Vector3(DirX[dir], 0f, DirZ[dir]);
                Vector3 size = horizontal ? new Vector3(Tile + 0.4f, 3f, 1.2f) : new Vector3(1.2f, 3f, Tile + 0.4f);
                AddBox(colliderParent, "Wall_" + sideNames[dir], pos + outward * 0.4f + Vector3.up * 1.5f, size);

                // 횃불: 8칸에 하나꼴 (점광원이 너무 많아지지 않게)
                if (!door && (t.x * 7 + t.y * 13 + dir * 5) % 8 == 0)
                {
                    AddTorch(torchParent, fire, pos - outward * 0.45f + Vector3.up * 2.0f, DirRot[dir]);
                }
            }
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

    // 바닥 충돌(걷기, NavMesh용): 방과 복도마다 상자 하나
    static void BuildFloorColliders(Transform parent)
    {
        foreach (RoomDef r in Rooms)
        {
            AddBox(parent, "FloorCollider", RectCenter(r.x, r.z, r.w, r.h) + Vector3.down * 0.5f, new Vector3(r.w * Tile, 1f, r.h * Tile));
        }
        foreach (int[] c in Corridors)
        {
            AddBox(parent, "FloorCollider", RectCenter(c[0], c[1], c[2], c[3]) + Vector3.down * 0.5f, new Vector3(c[2] * Tile, 1f, c[3] * Tile));
        }
    }

    // ───── 기둥: 엄폐물이면서 보스 돌진이 박히는 벽. 방마다 배치 모양이 다름 ─────
    static void BuildPillars(Transform parent)
    {
        GameObject wall = Load(Kit + "Wall_UnevenBrick_Straight.fbx");
        Material brick = wall.GetComponentInChildren<Renderer>().sharedMaterials[0];

        foreach (RoomDef r in Rooms)
        {
            Vector3 c = RectCenter(r.x, r.z, r.w, r.h);
            float hx = r.w * Tile * 0.5f, hz = r.h * Tile * 0.5f;
            List<Vector2> spots = new List<Vector2>();

            switch (r.pillars)
            {
                case Pillars.Corners:       // 네 구석 안쪽
                    foreach (int sx in new[] { -1, 1 })
                        foreach (int sz in new[] { -1, 1 })
                            spots.Add(new Vector2(c.x + sx * (hx - 4.5f), c.z + sz * (hz - 4.5f)));
                    break;

                case Pillars.Colonnade:     // 양옆으로 늘어선 기둥 줄 (긴 방향을 따라)
                    for (float z = c.z - hz + 3.5f; z <= c.z + hz - 3f; z += 5f)
                    {
                        spots.Add(new Vector2(c.x - hx * 0.5f, z));
                        spots.Add(new Vector2(c.x + hx * 0.5f, z));
                    }
                    break;

                case Pillars.Ring:          // 가운데를 둘러싼 네 개 + 중앙 하나
                    foreach (int sx in new[] { -1, 1 })
                        foreach (int sz in new[] { -1, 1 })
                            spots.Add(new Vector2(c.x + sx * hx * 0.5f, c.z + sz * hz * 0.5f));
                    spots.Add(new Vector2(c.x, c.z));
                    break;

                case Pillars.Grid:          // 바둑판 (엇갈린 3-2-3)
                    foreach (int i in new[] { -1, 0, 1 })
                        foreach (int j in new[] { -1, 0, 1 })
                            if ((i + j) % 2 == 0)
                                spots.Add(new Vector2(c.x + i * hx * 0.5f, c.z + j * hz * 0.5f));
                    break;

                case Pillars.Altar:         // 제단: 가운데 가까이 네 개
                    foreach (int sx in new[] { -1, 1 })
                        foreach (int sz in new[] { -1, 1 })
                            spots.Add(new Vector2(c.x + sx * 2.5f, c.z + sz * 2.5f));
                    break;

                case Pillars.Arena:         // 보스 방: 큰 원 위에 8개 + 바깥 구석 4개
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4f + Mathf.PI / 8f;
                        spots.Add(new Vector2(c.x + Mathf.Cos(a) * 11f, c.z + Mathf.Sin(a) * 9f));
                    }
                    foreach (int sx in new[] { -1, 1 })
                        foreach (int sz in new[] { -1, 1 })
                            spots.Add(new Vector2(c.x + sx * (hx - 3.5f), c.z + sz * (hz - 3.5f)));
                    break;
            }

            foreach (Vector2 spot in spots)
            {
                BuildPillar(parent, brick, spot);
            }
        }
    }

    // 기둥 하나: 받침 + 몸통 + 머리 (큐브 3개, 충돌은 몸통만)
    static void BuildPillar(Transform parent, Material brick, Vector2 spot)
    {
        GameObject pillar = new GameObject("Pillar");
        pillar.transform.SetParent(parent, false);
        pillar.transform.localPosition = new Vector3(spot.x, 0f, spot.y);
        pillar.isStatic = true;
        pillar.AddComponent<Occludable>();

        PillarPart(pillar.transform, brick, "Shaft", new Vector3(0f, 1.9f, 0f), new Vector3(1.6f, 3.8f, 1.6f), true);
        PillarPart(pillar.transform, brick, "Base", new Vector3(0f, 0.25f, 0f), new Vector3(2.2f, 0.5f, 2.2f), false);
        PillarPart(pillar.transform, brick, "Cap", new Vector3(0f, 3.65f, 0f), new Vector3(2.2f, 0.5f, 2.2f), false);
    }

    static void PillarPart(Transform parent, Material brick, string name, Vector3 pos, Vector3 scale, bool collide)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = pos;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = brick;
        part.isStatic = true;
        if (!collide)
        {
            Object.DestroyImmediate(part.GetComponent<Collider>());
        }
    }

    // ───── 방 표시 (미니맵, 몬스터 배치용) ─────
    static void BuildRoomMarkers(Transform parent)
    {
        foreach (RoomDef r in Rooms)
        {
            GameObject g = new GameObject("Room_" + r.name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = RectCenter(r.x, r.z, r.w, r.h);
            DungeonRoom room = g.AddComponent<DungeonRoom>();
            room.displayName = r.name;
            room.kind = r.kind;
            room.size = new Vector2(r.w * Tile, r.h * Tile);
        }
    }

    // ───── 분위기 소품: 통, 상자, 쇠사슬, 철창을 방 구석에 ─────
    static void BuildDecor(Transform parent)
    {
        string[] names = { "Barrel", "Crate_Wooden", "Barrel", "Cage_Small", "Chain_Coil", "Barrel", "Crate_Wooden" };
        System.Random rnd = new System.Random(7);
        int n = 0;
        foreach (RoomDef r in Rooms)
        {
            if (r.kind == DungeonRoom.Kind.Boss)
            {
                continue;
            }

            float x0 = r.x * Tile + 1.2f, x1 = (r.x + r.w) * Tile - 1.2f;
            float z0 = r.z * Tile + 1.2f, z1 = (r.z + r.h) * Tile - 1.2f;
            Vector2[] corners = { new Vector2(x0, z0), new Vector2(x1, z0), new Vector2(x0, z1), new Vector2(x1, z1) };

            foreach (Vector2 corner in corners)
            {
                if (rnd.NextDouble() < 0.55)
                {
                    continue;       // 구석이 전부 차지 않게
                }

                // 복도 입구를 막지 않도록 구석 안쪽으로만
                Place(Props + names[n % names.Length] + ".fbx", parent, new Vector3(corner.x, 0f, corner.y), (float)(rnd.NextDouble() * 360.0));
                n++;
            }
        }
    }

    // 방 구석 상자 (E 키로 열어 포션을 얻음). 설정은 이전 상자에서 복사
    static void BuildChests(Transform parent, Transform colliderParent, Chest template)
    {
        Dictionary<string, int> used = new Dictionary<string, int>();
        foreach (string roomName in ChestRooms)
        {
            RoomDef? found = null;
            foreach (RoomDef r in Rooms)
            {
                if (r.name == roomName) found = r;
            }
            if (found == null)
            {
                continue;
            }

            RoomDef room = found.Value;
            used.TryGetValue(roomName, out int index);
            used[roomName] = index + 1;

            // 북쪽 벽 아래 두 구석을 번갈아
            float x = index % 2 == 0 ? room.x * Tile + 1.5f : (room.x + room.w) * Tile - 1.5f;
            float z = (room.z + room.h) * Tile - 1.5f;
            Vector3 pos = new Vector3(x, 0f, z);
            Vector3 center = RectCenter(room.x, room.z, room.w, room.h);
            float rotY = Quaternion.LookRotation(center - pos).eulerAngles.y;

            GameObject chest = Place(Props + "Chest_Wood.fbx", parent, pos, rotY);
            if (chest == null)
            {
                continue;
            }
            chest.name = "Chest_" + roomName + "_" + index;
            AddBox(colliderParent, chest.name + "_Col", pos + Vector3.up * 0.5f, new Vector3(1.3f, 1f, 1.3f));

            if (template != null)
            {
                Chest c = chest.AddComponent<Chest>();
                c.contents = template.contents;
                c.counts = template.counts;
                c.openVfx = template.openVfx;
                c.openSounds = template.openSounds;
                c.range = template.range;
                c.prompt = template.prompt;
            }
        }
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
