using System.Collections;
using UnityEngine;

// 던전 입구 철문: 위에서 내려와 길을 막고(Close), 보스를 잡으면 올라가서 열림(Open)
public class DungeonGate : MonoBehaviour
{
    public Transform bars;              // 움직이는 창살 (자식 전체)
    public Collider blocker;            // 닫혔을 때만 켜짐
    public float openHeight = 3.6f;     // 열렸을 때 올라가 있는 높이
    public float moveTime = 0.7f;
    public AudioClip[] sounds;

    public bool IsClosed { get; private set; }

    void Awake()
    {
        // 처음엔 열려 있음
        bars.localPosition = new Vector3(0f, openHeight, 0f);
        blocker.enabled = false;
    }

    // 연출 없이 바로 닫힌 상태로 (씬 시작 때)
    public void CloseInstant()
    {
        StopAllCoroutines();
        bars.localPosition = Vector3.zero;
        blocker.enabled = true;
        IsClosed = true;
    }

    public void Close()
    {
        Move(0f, true);
    }

    public void Open()
    {
        Move(openHeight, false);
    }

    void Move(float targetY, bool closed)
    {
        StopAllCoroutines();
        StartCoroutine(Slide(targetY, closed));
    }

    IEnumerator Slide(float targetY, bool closed)
    {
        // 닫을 때는 시작과 동시에 막고, 열 때는 다 올라간 뒤 통과 가능
        if (closed)
        {
            blocker.enabled = true;
        }
        IsClosed = closed;
        Sfx.Play(sounds, 1f);

        float startY = bars.localPosition.y;
        float t = 0f;
        while (t < moveTime)
        {
            t += Time.deltaTime;
            float k = t / moveTime;
            k = closed ? k * k : 1f - (1f - k) * (1f - k);      // 닫힐 땐 쿵 떨어지듯, 열릴 땐 부드럽게
            bars.localPosition = new Vector3(0f, Mathf.Lerp(startY, targetY, k), 0f);
            yield return null;
        }

        bars.localPosition = new Vector3(0f, targetY, 0f);
        if (closed)
        {
            CameraFollow.Shake(0.25f, 0.2f);
        }
        else
        {
            blocker.enabled = false;
        }
    }
}
