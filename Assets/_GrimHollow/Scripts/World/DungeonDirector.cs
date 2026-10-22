using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 던전 진행: 입장 → 철문 닫힘 → 웨이브들 → 보스 등장 → 보스전 중 쫄몹 소환 → 클리어(철문 열림)
public class DungeonDirector : MonoBehaviour
{
    [Serializable]
    public class Wave
    {
        public GameObject monsterA;
        public int countA = 3;
        public GameObject monsterB;
        public int countB;
    }

    enum Stage { WaitingEntry, Waves, BossIntro, Boss, Cleared }

    public DungeonGate gate;
    public ScenePortal exitPortal;          // 클리어 전에는 꺼 둠
    public Wave[] waves;
    public GameObject bossPrefab;
    public Vector3 bossSpawn = new Vector3(0f, 0f, 9f);

    [Header("Boss adds (쫄몹)")]
    public GameObject addPhase1;
    public GameObject addPhase2;
    public float addInterval = 25f;
    public int addsPerBatch = 2;
    public int maxAdds = 4;

    [Header("Spawn")]
    public float roomHalf = 12.5f;          // 몬스터가 나타날 수 있는 범위
    public float minDistanceFromPlayer = 7f;
    public GameObject spawnVfx;
    public AudioClip[] spawnSounds;
    public float entryZ = -9f;              // 플레이어가 이 선을 넘으면 입장으로 침

    private Stage stage = Stage.WaitingEntry;
    private Transform player;
    private int waveIndex;
    private float timer;
    private bool waveStarted;
    private readonly List<MonsterHealth> alive = new List<MonsterHealth>();
    private MonsterHealth boss;
    private BossController bossController;
    private float addTimer;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        if (exitPortal != null)
        {
            exitPortal.enabled = false;
        }
    }

    void Update()
    {
        alive.RemoveAll(m => m == null || m.isDead);

        switch (stage)
        {
            case Stage.WaitingEntry: UpdateEntry(); break;
            case Stage.Waves: UpdateWaves(); break;
            case Stage.BossIntro: UpdateBossIntro(); break;
            case Stage.Boss: UpdateBoss(); break;
        }
    }

    void UpdateEntry()
    {
        if (player.position.z < entryZ)
        {
            return;
        }

        gate.Close();
        stage = Stage.Waves;
        waveIndex = 0;
        timer = 2.0f;
        waveStarted = false;
        UIBanner.Show("던전 1층", "게이트가 닫혔다", 2.2f);
    }

    void UpdateWaves()
    {
        timer -= Time.deltaTime;

        if (!waveStarted)
        {
            if (timer <= 0f)
            {
                StartWave(waves[waveIndex]);
                waveStarted = true;
            }
            return;
        }

        // 이번 웨이브를 다 잡으면 잠깐 쉬고 다음으로
        if (alive.Count == 0)
        {
            waveIndex++;
            waveStarted = false;
            timer = 3f;

            if (waveIndex >= waves.Length)
            {
                BeginBossIntro();
            }
        }
    }

    void StartWave(Wave wave)
    {
        UIBanner.Show("WAVE " + (waveIndex + 1), waveIndex == waves.Length - 1 ? "마지막 웨이브" : "", 1.4f);
        Spawn(wave.monsterA, wave.countA);
        Spawn(wave.monsterB, wave.countB);
    }

    void BeginBossIntro()
    {
        stage = Stage.BossIntro;
        timer = 3.5f;       // 웨이브 사이 정적 → 보스 등장
        UIBanner.Show("MUSHROOM KING", "던전의 주인", 2.6f);
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
        Vector3 pos = SamplePoint(bossSpawn);
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
        if (addTimer <= 0f && alive.Count < maxAdds)
        {
            addTimer = addInterval;
            Spawn(bossController.InPhase2 && addPhase2 != null ? addPhase2 : addPhase1, addsPerBatch);
        }
    }

    void Clear()
    {
        stage = Stage.Cleared;

        // 남은 쫄몹 정리
        foreach (MonsterHealth m in alive)
        {
            m.TakeDamage(9999);
        }

        gate.Open();
        if (exitPortal != null)
        {
            exitPortal.enabled = true;
        }
        UIBanner.Show("DUNGEON CLEARED", "철문이 열렸다. 마을로 돌아가자", 3.5f);
    }

    // ───── 소환 ─────
    void Spawn(GameObject prefab, int count)
    {
        if (prefab == null)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = RandomSpawnPoint();
            GameObject go = Instantiate(prefab, pos, Quaternion.LookRotation(player.position - pos));
            MonsterHealth mh = go.GetComponent<MonsterHealth>();
            alive.Add(mh);

            // 던전에서는 처음부터 플레이어를 쫓고, 멀어져도 놓치지 않음
            MonsterController mc = go.GetComponent<MonsterController>();
            mc.loseRange = 999f;
            mc.Aggro();

            if (spawnVfx != null)
            {
                Destroy(Instantiate(spawnVfx, pos + Vector3.up * 0.1f, Quaternion.identity), 3f);
            }
        }
        Sfx.Play(spawnSounds, 0.8f);
    }

    Vector3 RandomSpawnPoint()
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 p = new Vector3(UnityEngine.Random.Range(-roomHalf, roomHalf), 0f, UnityEngine.Random.Range(-roomHalf + 4f, roomHalf));
            Vector3 flat = p - player.position;
            flat.y = 0f;
            if (flat.magnitude >= minDistanceFromPlayer && NavMesh.SamplePosition(p, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return SamplePoint(bossSpawn);
    }

    static Vector3 SamplePoint(Vector3 p)
    {
        return NavMesh.SamplePosition(p, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : p;
    }
}
