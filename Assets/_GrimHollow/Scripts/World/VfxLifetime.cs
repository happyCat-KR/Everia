using UnityEngine;

// 반복(loop) 재생되는 이펙트를 정해진 시간만 보여 주고 자연스럽게 사라지게 함 (새 파티클 멈춤 → 남은 파티클이 사라진 뒤 삭제)
public class VfxLifetime : MonoBehaviour
{
    public float life = 1f;
    public float tail = 2.5f;       // 방출을 멈춘 뒤 파티클이 사라질 때까지 기다리는 시간

    private float timer;
    private bool stopped;

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float life, Transform parent = null)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject go = Instantiate(prefab, position, rotation, parent);
        go.AddComponent<VfxLifetime>().life = life;
        return go;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (!stopped && timer >= life)
        {
            stopped = true;
            foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>())
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
            Destroy(gameObject, tail);
        }
    }
}
