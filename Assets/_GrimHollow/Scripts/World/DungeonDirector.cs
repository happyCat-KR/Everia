using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 던전 진행: 방마다 몬스터가 미리 배치됨 → 중간보스가 열쇠를 떨어뜨림 → 열쇠로 보스 방 철문을 열고 입장 → 철문 닫힘 → 보스전 → 클리어(철문 열림)
public class DungeonDirector : MonoBehaviour
{
    [Serializable]
    public class Encounter
    {
        public string room;             // DungeonRoom.displayName
        public GameObject monster;
        public int count = 3;
    }

    enum Stage { Exploring, BossIntro, Boss, Cleared }

    public static bool HasKey;

    public DungeonGate gate;                // 보스 방 철문
    public Encounter[] encounters;
    public string miniBossRoom = "북서쪽 방";
    public GameObject miniBossPrefab;       // 열쇠를 가진 중간보스 (일반 몬스터를 키워서 씀)
    public float miniBossScale = 1.6f;
    public int miniBossHpMultiplier = 5;
    public GameObject bossPrefab;

    [Header("Boss adds (쫄몹)")]
    public GameObject addPhase1;
    public GameObject addPhase2;
    public float addInterval = 25f;
    public int addsPerBatch = 2;
    public int maxAdds = 4;

    [Header("Spawn")]
    public float roomMargin = 3f;           // 벽에서 이만큼 안쪽에 배치
    public float minDistanceFromPlayer = 7f;
    public GameObject spawnVfx;
    public AudioClip[] spawnSounds;

    private Stage stage = Stage.Exploring;
    private Transform player;
    private DungeonRoom[] rooms;
    private DungeonRoom bossRoom;
    private MonsterHealth miniBoss;
    private bool keyDropped;
    private float timer;
    private readonly List<MonsterHealth> adds = new List<MonsterHealth>();
    private MonsterHealth boss;
    private BossController bossController;
    private float addTimer;

    void Start()
    {
        HasKey = false;
        player = GameObject.FindWithTag("Player").transform;
        rooms = FindObjectsByType<DungeonRoom>();
        foreach (DungeonRoom r in rooms)
        {
            r.visited = r.kind == DungeonRoom.Kind.Start;
            if (r.kind == DungeonRoom.Kind.Boss)
            {
                bossRoom = r;
            }
        }

        gate.CloseInstant();        // 열쇠로 열 때까지 잠김

        foreach (Encounter e in encounters)
        {
            DungeonRoom room = FindRoom(e.room);
            if (room == null)
            {
                Debug.LogWarning("방을 찾을 수 없음: " + e.room);
                continue;
            }
            SpawnInRoom(room, e.monster, e.count, false, false);
        }

        DungeonRoom miniRoom = FindRoom(miniBossRoom);
        if (miniRoom != null && miniBossPrefab != null)
        {
            miniBoss = SpawnInRoom(miniRoom, miniBossPrefab, 1, false, true);
        }
    }

    DungeonRoom FindRoom(string roomName)
    {
        foreach (DungeonRoom r in rooms)
        {
            if (r.displayName == roomName)
            {
                return r;
            }
        }
        return null;
    }

    void Update()
    {
        adds.RemoveAll(m => m == null || m.isDead);

        switch (stage)
        {
            case Stage.Exploring: UpdateExploring(); break;
            case Stage.BossIntro: UpdateBossIntro(); break;
            case Stage.Boss: UpdateBoss(); break;
        }
    }

    void UpdateExploring()
    {
        // 처음 들어간 방 이름을 알려 줌 (미니맵도 이 정보를 씀)
        foreach (DungeonRoom r in rooms)
        {
            if (!r.visited && r.kind != DungeonRoom.Kind.Boss && r.Contains(player.position))
            {
                r.visited = true;
                UIBanner.Show(r.displayName, "", 1.4f);
            }
        }

        // 중간보스를 잡으면 열쇠
        if (!keyDropped && miniBoss != null && miniBoss.isDead)
        {
            keyDropped = true;
            KeyPickup.Spawn(miniBoss.transform.position);
            UIBanner.Show("열쇠가 떨어졌다", "", 1.8f);
        }

        // 보스 방에 들어서면 철문이 닫히고 보스 등장
        if (bossRoom != null && bossRoom.Contains(player.position))
        {
            bossRoom.visited = true;
            gate.Close();
            stage = Stage.BossIntro;
            timer = 3.5f;       // 정적 → 보스 등장
            UIBanner.Show("MUSHROOM KING", "던전의 주인", 2.6f);
        }
    }

    void UpdateBossIntro()
    {
        timer -= Time.deltaTime;
        if (boss == null && timer <= 2.4f)
        {
            SpawnBoss();
        }

        if (boss != null && timer <= 0f)
        {
            boss.invulnerable = false;
            bossController.Wake();
            addTimer = addInterval;
            stage = Stage.Boss;
        }
    }

    void SpawnBoss()
    {
        Vector3 pos = SamplePoint(bossRoom.transform.position + Vector3.forward * 4f);
        GameObject go = Instantiate(bossPrefab, pos, Quaternion.LookRotation(player.position - pos));
        boss = go.GetComponent<MonsterHealth>();
        bossController = go.GetComponent<BossController>();
        bossController.autoWake = false;
        boss.invulnerable = true;       // 등장 연출 중에는 데미지를 받지 않음

        BossHealthBar bar = FindAnyObjectByType<BossHealthBar>(FindObjectsInactive.Include);
        if (bar != null)
        {
            bar.Bind(bossController, boss);
        }

        if (spawnVfx != null)
        {
            Destroy(Instantiate(spawnVfx, pos + Vector3.up * 0.1f, Quaternion.identity), 3f);
        }
        CameraFollow.Shake(0.6f, 0.8f);
        Sfx.Play(spawnSounds, 1f);
    }

    void UpdateBoss()
    {
        if (boss.isDead)
        {
            Clear();
            return;
        }

        // 쫄몹: 일정 간격으로 (2페이즈에선 오크), 너무 많아지지 않게
        addTimer -= Time.deltaTime;
        if (addTimer <= 0f && adds.Count < maxAdds)
        {
            addTimer = addInterval;
            SpawnInRoom(bossRoom, bossController.InPhase2 && addPhase2 != null ? addPhase2 : addPhase1, addsPerBatch, true, false);
        }
    }

    void Clear()
    {
        stage = Stage.Cleared;

        // 남은 쫄몹 정리
        foreach (MonsterHealth m in adds)
        {
            m.TakeDamage(9999);
        }

        gate.Open();
        UIBanner.Show("DUNGEON CLEARED", "철문이 열렸다. 입구의 포탈로 돌아가자", 3.5f);
    }

    // ───── 소환 ─────
    // chase: 처음부터 플레이어를 쫓고 놓치지 않음 (보스 쫄몹) / 아니면 가만히 있다가 가까이 오면 반응
    MonsterHealth SpawnInRoom(DungeonRoom room, GameObject prefab, int count, bool chase, bool mini)
    {
        if (prefab == null)
        {
            return null;
        }

        MonsterHealth last = null;
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = RandomPoint(room);
            Quaternion look = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            GameObject go = Instantiate(prefab, pos, look);
            MonsterHealth mh = go.GetComponent<MonsterHealth>();
            MonsterController mc = go.GetComponent<MonsterController>();
            last = mh;

            if (mini)
            {
                go.transform.localScale *= miniBossScale;
                mh.maxHp *= miniBossHpMultiplier;
                mh.currentHp = mh.maxHp;
                mh.expReward *= 4;
                mc.attackDamage = Mathf.RoundToInt(mc.attackDamage * 1.5f);
            }

            if (chase)
            {
                adds.Add(mh);
                mc.loseRange = 999f;
                mc.Aggro();
                if (spawnVfx != null)
                {
                    Destroy(Instantiate(spawnVfx, pos + Vector3.up * 0.1f, Quaternion.identity), 3f);
                }
            }
        }

        if (chase)
        {
            Sfx.Play(spawnSounds, 0.8f);
        }
        return last;
    }

    Vector3 RandomPoint(DungeonRoom room)
    {
        Bounds b = room.Bounds;
        for (int i = 0; i < 30; i++)
        {
            Vector3 p = new Vector3(
                UnityEngine.Random.Range(b.min.x + roomMargin, b.max.x - roomMargin), 0f,
                UnityEngine.Random.Range(b.min.z + roomMargin, b.max.z - roomMargin));
            Vector3 flat = p - player.position;
            flat.y = 0f;
            if (flat.magnitude >= minDistanceFromPlayer && NavMesh.SamplePosition(p, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return SamplePoint(room.transform.position);
    }

    static Vector3 SamplePoint(Vector3 p)
    {
        return NavMesh.SamplePosition(p, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : p;
    }
}
