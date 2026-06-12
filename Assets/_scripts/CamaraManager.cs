using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("摄像机引用")]
    [SerializeField] private CinemachineCamera virtualCamera;

    [Header("缩放平滑设置")]
    [Tooltip("缩放达到目标值所需的近似时间，数值越小越快")]
    [SerializeField] private float smoothTime = 0.25f;

    private float defaultSize = 7f;
    private float targetSize;
    private float currentVelocity = 0f; // 供 SmoothDamp 内部使用的平滑速度

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (virtualCamera != null)
        {
            defaultSize = virtualCamera.Lens.OrthographicSize;
            // 初始目标设为默认大小
            targetSize = defaultSize;
        }
    }

    private void Update()
    {
        if (virtualCamera == null) return;

        // 🌟 核心逻辑：每帧平滑逼近目标大小
        float currentSize = virtualCamera.Lens.OrthographicSize;

        // 使用 SmoothDamp 计算平滑后的数值
        float newSize = Mathf.SmoothDamp(
            currentSize,
            targetSize,
            ref currentVelocity,
            smoothTime
        );

        // 应用回摄像机 (注意：Unity 6 中 Lens 是属性，需要整体重新赋值或修改赋值)
        LensSettings lens = virtualCamera.Lens;
        lens.OrthographicSize = newSize;
        virtualCamera.Lens = lens;
    }

    // 现在这个方法只是改变“目标值”，剩下的交给 Update 去平滑移动
    public void SetCameraSize(float size)
    {
        targetSize = size;
    }

    public void ResetCameraSize()
    {
        targetSize = defaultSize;
    }
}