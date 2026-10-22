using UnityEngine;

// 포자 폭발: 바닥에 원이 차오르다가 터짐 (2페이즈 보스 패턴, 한 번 만들면 혼자 정리됨)
public class SporeBlast : MonoBehaviour
{
    private GroundIndicator indicator;
    private Vector3 center;
    private float radius;
    private float delay;
    private float fillTime;
    private int damage;
    private float timer;
    private GameObject vfx;
    private AudioClip[] sounds;
    private PlayerStats player;

    public static void Spawn(Vector3 position, float radius, float delay, float fillTime, int damage,
                             Material outline, Material fill, GameObject vfx, AudioClip[] sounds, PlayerStats player)
    {
        GameObject go = new GameObject("SporeBlast");
        SporeBlast blast = go.AddComponent<SporeBlast>();
        blast.center = new Vector3(position.x, 0f, position.z);
        blast.radius = radius;
        blast.delay = delay;
        blast.fillTime = fillTime;
        blast.damage = damage;
        blast.vfx = vfx;
        blast.sounds = sounds;
        blast.player = player;

        // 시간차로 터지게 하려고, 경고도 delay 뒤에 나타남
        blast.indicator = GroundIndicator.CreateSector(radius, 360f, outline, fill);
        blast.indicator.Place(blast.center, Quaternion.identity);
        blast.indicator.gameObject.SetActive(false);
    }

    private bool exploded;

    void Update()
    {
        if (exploded)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer < delay)
        {
            return;
        }

        indicator.gameObject.SetActive(true);
        float t = (timer - delay) / fillTime;
        indicator.SetProgress(t);

        if (t >= 1f)
        {
            Explode();
        }
    }

    void Explode()
    {
        exploded = true;

        if (vfx != null)
        {
            GameObject fx = Instantiate(vfx, center + Vector3.up * 0.1f, Quaternion.identity);
            Destroy(fx, 3f);
        }
        Sfx.Play(sounds, 0.7f);
        CameraFollow.Shake(0.18f, 0.15f);

        Vector3 d = player.transform.position - center;
        d.y = 0f;
        if (d.magnitude <= radius)
        {
            player.TakeDamage(damage);
        }

        Destroy(indicator.gameObject);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (indicator != null)
        {
            Destroy(indicator.gameObject);
        }
    }
}
