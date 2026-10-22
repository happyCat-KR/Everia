using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 카메라와 플레이어 사이를 가리면 반투명해지는 오브젝트 (건물, 큰 나무 등의 맨 위에 붙임)
public class Occludable : MonoBehaviour
{
    public float fadedAlpha = 0.25f;    // 가렸을 때 투명도 (0 = 완전히 안 보임)
    public float fadeSpeed = 4f;        // 초당 투명도 변화량 (4 = 약 0.2초에 사라짐)

    private Renderer[] renderers;
    private Material[][] opaqueMaterials;       // 원래 재질
    private Material[][] fadeMaterials;         // 반투명 복사본 (처음 가릴 때 만듦)
    private readonly List<Material> fadeList = new List<Material>();
    private readonly List<Color> fadeBaseColors = new List<Color>();

    private Bounds bounds;
    private float alpha = 1f;
    private bool usingFade;

    public Bounds Bounds => bounds;
    public bool Occluding { get; set; }    // OcclusionHider가 매 프레임 정해 줌

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        opaqueMaterials = new Material[renderers.Length][];

        // 자식 렌더러 전체를 감싸는 상자 (정적인 건물·나무라서 한 번만 계산)
        bounds = new Bounds(transform.position, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            opaqueMaterials[i] = renderers[i].sharedMaterials;
            bounds.Encapsulate(renderers[i].bounds);
        }
    }

    void Update()
    {
        float target = Occluding ? fadedAlpha : 1f;
        if (Mathf.Approximately(alpha, target))
        {
            return;
        }

        alpha = Mathf.MoveTowards(alpha, target, fadeSpeed * Time.deltaTime);

        if (alpha < 1f)
        {
            SetFadeMaterials(true);
            for (int i = 0; i < fadeList.Count; i++)
            {
                Color c = fadeBaseColors[i];
                c.a *= alpha;
                fadeList[i].SetColor("_BaseColor", c);
            }
        }
        else
        {
            SetFadeMaterials(false);    // 다 보이면 원래 불투명 재질로 (정렬 문제 없게)
        }
    }

    void SetFadeMaterials(bool fade)
    {
        if (usingFade == fade)
        {
            return;
        }

        usingFade = fade;
        if (fade && fadeMaterials == null)
        {
            BuildFadeMaterials();
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sharedMaterials = fade ? fadeMaterials[i] : opaqueMaterials[i];
        }
    }

    void BuildFadeMaterials()
    {
        Dictionary<Material, Material> copies = new Dictionary<Material, Material>();
        fadeMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] source = opaqueMaterials[i];
            fadeMaterials[i] = new Material[source.Length];

            for (int j = 0; j < source.Length; j++)
            {
                if (source[j] == null)
                {
                    continue;
                }

                if (!copies.TryGetValue(source[j], out Material copy))
                {
                    copy = MakeTransparent(source[j]);
                    copies.Add(source[j], copy);
                    fadeList.Add(copy);
                    fadeBaseColors.Add(copy.GetColor("_BaseColor"));
                }

                fadeMaterials[i][j] = copy;
            }
        }
    }

    // URP Lit 재질을 반투명(Alpha 블렌드)으로 바꾼 복사본
    static Material MakeTransparent(Material source)
    {
        Material m = new Material(source);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        return m;
    }
}
