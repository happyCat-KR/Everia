using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MosterSpawner : MonoBehaviour
{
    public GameObject monsterPrefab;
    public float spawnInterval = 3f;    // 몇 초마다
    public int maxMonsters = 5;     // 최대 몇 마리까지
    public float spawnRadius = 4f;   // 스포너 주변 반경

    private List<GameObject> monsters = new List<GameObject>();
    private float spawnTimer;

    void Update()
    {
        monsters.RemoveAll(m => m == null); // 죽어서 삭제된 몬스터는 목록에서 제거

        spawnTimer += Time.deltaTime;
        if(spawnTimer >= spawnInterval && monsters.Count < maxMonsters)
        {
            spawnTimer = 0f;
            Spawn();
        }
    }
    
    void Spawn()
    {
        // 반경 안 랜덤 위치
        Vector2 random = Random.insideUnitCircle * spawnRadius;
        Vector3 point = transform.position + new Vector3(random.x, 0f, random.y);

        // NavMesh 위의 가장 가까운 점으로 보정 (벽 안이나 맵 밖에 생기지 않게)
        if(NavMesh.SamplePosition(point, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            GameObject monster = Instantiate(monsterPrefab, hit.position, Quaternion.identity);
            monsters.Add(monster);
        }
    }

    // Scene 뷰에서 스폰 범위 표시
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }

}