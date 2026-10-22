using UnityEngine;

// 바닥에 떨어진 아이템: 둥실거리며 돌다가, 플레이어가 가까이 오면 끌려와서 주워짐
public class ItemPickup : MonoBehaviour
{
    public ItemData item;
    public int count = 1;

    public float magnetRange = 2.5f;    // 이 안에 들어오면 플레이어 쪽으로 끌려감
    public float pickupRange = 0.8f;
    public float magnetSpeed = 9f;

    private Transform player;
    private Transform model;
    private float pickupDelay = 0.4f;   // 떨어지는 연출 동안은 안 주워짐
    private Vector3 velocity;
    private float bobOffset;

    // 몬스터가 죽을 때 호출: 살짝 튀어 오르며 떨어짐
    public static void Spawn(ItemData item, int count, Vector3 position)
    {
        GameObject go = new GameObject("Pickup_" + item.displayName);
        go.transform.position = position + Vector3.up * 0.5f;

        ItemPickup pickup = go.AddComponent<ItemPickup>();
        pickup.item = item;
        pickup.count = count;

        if (item.worldModel != null)
        {
            GameObject model = Instantiate(item.worldModel, go.transform);
            model.transform.localPosition = Vector3.zero;
            pickup.model = model.transform;
        }

        // 바닥에서 눈에 띄게 은은한 빛
        Light glow = go.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = 1.8f;
        glow.intensity = 2.5f;
        glow.color = item.type == ItemType.Weapon ? new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.85f, 0.4f);
        glow.shadows = LightShadows.None;

        Vector2 side = Random.insideUnitCircle * 2f;
        pickup.velocity = new Vector3(side.x, 4f, side.y);
        pickup.bobOffset = Random.value * 10f;
    }

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        if (pickupDelay > 0f)
        {
            // 포물선으로 떨어지기
            pickupDelay -= Time.deltaTime;
            velocity.y -= 15f * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            if (transform.position.y < 0.3f)
            {
                transform.position = new Vector3(transform.position.x, 0.3f, transform.position.z);
                velocity = Vector3.zero;
            }
            return;
        }

        // 둥실둥실 + 회전
        if (model != null)
        {
            model.localPosition = Vector3.up * (Mathf.Sin((Time.time + bobOffset) * 3f) * 0.12f);
            model.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
        }

        Vector3 target = player.position + Vector3.up * 0.5f;
        float distance = Vector3.Distance(transform.position, target);

        if (distance < pickupRange)
        {
            Inventory inventory = player.GetComponent<Inventory>();
            if (inventory.Add(item, count))
            {
                Destroy(gameObject);
            }
        }
        else if (distance < magnetRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * Time.deltaTime);
        }
    }
}
