using UnityEngine;

// 횃불 불빛이 일렁이게 (밝기를 노이즈로 흔듦)
[RequireComponent(typeof(Light))]
public class TorchFlicker : MonoBehaviour
{
    public float amount = 0.35f;        // 밝기가 흔들리는 폭 (기본 밝기의 비율)
    public float speed = 6f;

    private Light torch;
    private float baseIntensity;
    private float seed;

    void Awake()
    {
        torch = GetComponent<Light>();
        baseIntensity = torch.intensity;
        seed = Random.value * 100f;     // 횃불마다 따로 흔들리게
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * speed);
        torch.intensity = baseIntensity * (1f - amount + n * amount * 2f);
    }
}
