using UnityEngine;

// 효과음 재생 도우미: 여러 클립 중 하나를 골라 2D로 재생 (쿼터뷰라 거리 감쇠 없이 또렷하게)
public static class Sfx
{
    private static AudioSource source;

    public static void Play(AudioClip[] clips, float volume = 1f)
    {
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        if (source == null)
        {
            GameObject go = new GameObject("Sfx");
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
        }

        source.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);
    }
}
