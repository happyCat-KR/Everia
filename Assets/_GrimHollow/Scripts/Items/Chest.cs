using UnityEngine;

// 상자: 가까이 가서 E 키를 누르면 열리고 안의 아이템이 튀어나옴 (한 번만)
public class Chest : MonoBehaviour
{
    public ItemData[] contents;
    public int[] counts;
    public float range = 2.4f;
    public GameObject openVfx;
    public AudioClip[] openSounds;
    public string prompt = "[E]  상자 열기";

    private Transform player;
    private bool opened;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        if (opened)
        {
            return;
        }

        Vector3 d = player.position - transform.position;
        d.y = 0f;
        if (d.magnitude > range)
        {
            InteractPrompt.Hide(this);
            return;
        }

        InteractPrompt.Show(this, prompt);
        if (Input.GetKeyDown(KeyCode.E))
        {
            Open();
        }
    }

    void Open()
    {
        opened = true;
        InteractPrompt.Hide(this);
        Sfx.Play(openSounds, 0.9f);

        if (openVfx != null)
        {
            Destroy(Instantiate(openVfx, transform.position + Vector3.up * 0.8f, Quaternion.identity), 2f);
        }

        for (int i = 0; i < contents.Length; i++)
        {
            for (int n = 0; n < counts[i]; n++)
            {
                ItemPickup.Spawn(contents[i], 1, transform.position + Vector3.up * 0.5f);
            }
        }

        // 열린 상자는 살짝 어둡게 (이미 비었다는 표시)
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.material.color = new Color(0.55f, 0.55f, 0.55f);
        }
    }

    void OnDestroy()
    {
        InteractPrompt.Hide(this);
    }
}
