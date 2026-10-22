using UnityEngine;

// 마법사 불덩이: 앞으로 날아가다가 몬스터에 닿으면 폭발
public class Projectile : MonoBehaviour
{
    public float speed = 16f;
    public float maxDistance = 14f;
    public float hitRadius = 0.5f;
    public GameObject explosionVfx;
    public AudioClip[] explosionSounds;

    private int damage;
    private LayerMask monsterLayer;
    private LayerMask obstacleLayer;
    private float traveled;

    public void Launch(int damage, LayerMask monsterLayer, LayerMask obstacleLayer)
    {
        this.damage = damage;
        this.monsterLayer = monsterLayer;
        this.obstacleLayer = obstacleLayer;
    }

    void Update()
    {
        float step = speed * Time.deltaTime;

        // 벽에 막히면 그 자리에서 터짐
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit wall, step, obstacleLayer, QueryTriggerInteraction.Ignore))
        {
            transform.position = wall.point;
            Explode(null);
            return;
        }

        transform.position += transform.forward * step;
        traveled += step;

        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius, monsterLayer);
        foreach (Collider hit in hits)
        {
            MonsterHealth monster = hit.GetComponent<MonsterHealth>();
            if (monster != null && !monster.isDead)
            {
                Explode(monster);
                return;
            }
        }

        if (traveled >= maxDistance)
        {
            Explode(null);
        }
    }

    void Explode(MonsterHealth target)
    {
        if (target != null)
        {
            target.TakeDamage(damage);
            CameraFollow.Shake(0.12f, 0.12f);
        }

        if (explosionVfx != null)
        {
            GameObject vfx = Instantiate(explosionVfx, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }
        Sfx.Play(explosionSounds, 0.7f);
        Destroy(gameObject);
    }
}
