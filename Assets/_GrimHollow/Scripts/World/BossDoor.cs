using UnityEngine;

// 보스 방 철문 앞 상호작용: 열쇠가 있으면 [E]로 열고, 없으면 잠겨 있다고 알려 줌
[RequireComponent(typeof(DungeonGate))]
public class BossDoor : MonoBehaviour
{
    public float range = 3.5f;
    public string lockedPrompt = "잠겨 있다 — 열쇠가 필요하다";
    public string openPrompt = "[E]  문 열기";

    private DungeonGate gate;
    private Transform player;
    private bool opened;

    void Start()
    {
        gate = GetComponent<DungeonGate>();
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

        if (!DungeonDirector.HasKey)
        {
            InteractPrompt.Show(this, lockedPrompt);
            return;
        }

        InteractPrompt.Show(this, openPrompt);
        if (Input.GetKeyDown(KeyCode.E))
        {
            opened = true;
            InteractPrompt.Hide(this);
            gate.Open();
        }
    }

    void OnDestroy()
    {
        InteractPrompt.Hide(this);
    }
}
