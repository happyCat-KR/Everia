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

    private static AudioSource[] exclusiveSources;
    private static SfxFader[] faders;
    private static int exclusiveIndex;

    // 한 번에 하나만 들리게: 새 소리는 다른 소스에서 시작하고, 앞 소리는 끊지 않고 짧게 줄여서 없앰 (뚝 끊으면 "탁" 하는 잡음이 남)
    // maxSeconds 안에 끝나게 잘라서 겹침도 막음
    public static void PlayExclusive(AudioClip[] clips, float volume, float maxSeconds)
    {
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        if (exclusiveSources == null || exclusiveSources[0] == null)
        {
            exclusiveSources = new AudioSource[2];
            faders = new SfxFader[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject go = new GameObject("SfxExclusive" + i);
                Object.DontDestroyOnLoad(go);
                exclusiveSources[i] = go.AddComponent<AudioSource>();
                exclusiveSources[i].playOnAwake = false;
                AudioLowPassFilter softener = go.AddComponent<AudioLowPassFilter>();     // 칼 소리의 치지직거리는 고음을 눌러 줌
                softener.cutoffFrequency = 4500f;
                faders[i] = go.AddComponent<SfxFader>();
            }
        }

        faders[exclusiveIndex].FadeOutNow(0.08f);       // 앞 소리는 짧게 줄여서 없앰
        exclusiveIndex = 1 - exclusiveIndex;

        AudioSource next = exclusiveSources[exclusiveIndex];
        next.Stop();
        next.clip = clips[Random.Range(0, clips.Length)];
        next.volume = volume;
        next.time = 0f;
        next.Play();
        faders[exclusiveIndex].Begin(next, volume, maxSeconds);
    }
}

// 재생 중인 소리를 정해진 시간에 끝나도록 마지막 구간에서 줄여 끊음
public class SfxFader : MonoBehaviour
{
    private AudioSource source;
    private float volume;
    private float endTime;
    private float fadeTime = 0.12f;

    public void Begin(AudioSource target, float targetVolume, float maxSeconds)
    {
        source = target;
        volume = targetVolume;
        fadeTime = 0.12f;
        endTime = Time.unscaledTime + maxSeconds;
    }

    // 지금부터 time 초 동안 줄이고 끝냄 (이미 더 빨리 끝날 예정이면 그대로)
    public void FadeOutNow(float time)
    {
        if (source == null || !source.isPlaying)
        {
            return;
        }

        float newEnd = Time.unscaledTime + time;
        if (newEnd < endTime)
        {
            endTime = newEnd;
            fadeTime = time;
            volume = source.volume;
        }
    }

    void Update()
    {
        if (source == null || !source.isPlaying)
        {
            return;
        }

        float left = endTime - Time.unscaledTime;
        if (left <= 0f)
        {
            source.Stop();
        }
        else if (left < fadeTime)
        {
            source.volume = volume * (left / fadeTime);
        }
    }
}
