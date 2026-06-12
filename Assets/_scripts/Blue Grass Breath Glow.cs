using UnityEngine;
using UnityEngine.Rendering.Universal; // 引入 2D Light 命名空间

public class BreathingGlow : MonoBehaviour
{
    [Header("组件引用")]
    public SpriteRenderer spriteRenderer;
    public Light2D environmentLight;

    [Header("呼吸参数")]
    public float breatheSpeed = 2f;      // 呼吸速度
    public float minIntensity = 0.2f;    // 最暗的值
    public float maxIntensity = 1.0f;    // 最亮的值

    [Header("材质 Shader 参数名")]
    public string emissionColorName = "_EmissionColor"; // 你 Shader 里发光颜色的变量名
    [ColorUsage(true, true)] // 开启 HDR 拾色器
    public Color baseGlowColor = Color.cyan;

    private MaterialPropertyBlock mpb; // 性能核心：防止每次修改材质都生成新实例

    // 【新增】：用于打破同步的随机偏移量
    private float randomTimeOffset;
    void Start()
    {
        // 如果没有手动拖拽，自动获取身上的组件
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (environmentLight == null) environmentLight = GetComponentInChildren<Light2D>();

        mpb = new MaterialPropertyBlock();
        // 【新增】：每个物体随机获得一个 0 到 100 之间的时间偏移
        randomTimeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // 【修改】：把 randomTimeOffset 加到 Time.time 里面
        float sinValue = (Mathf.Sin((Time.time + randomTimeOffset) * breatheSpeed) + 1f) / 2f;

        float currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, sinValue);

        if (environmentLight != null) environmentLight.intensity = currentIntensity;

        if (spriteRenderer != null)
        {
            spriteRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(emissionColorName, baseGlowColor * currentIntensity);
            spriteRenderer.SetPropertyBlock(mpb);
        }
    }
    public void ChangeColor(Color newcolor)
    {
        baseGlowColor = newcolor;
    }
}