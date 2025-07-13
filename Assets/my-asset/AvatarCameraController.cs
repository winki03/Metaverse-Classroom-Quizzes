using UnityEngine;

public class AvatarCanvasCameraSwitcher : MonoBehaviour
{
    public Canvas avatarCanvas;        // AvatarCanvas 的 Canvas 组件
    public Camera canvasCamera;        // Canvas 专用摄像机

    private Camera mainCamera;

    void Start()
    {
        // 查找 tag 为 MainCamera 的摄像机
        mainCamera = Camera.main;
    }

    void OnEnable()
    {
        // 只有在 AvatarCanvas 激活时才执行
        if (avatarCanvas != null && canvasCamera != null)
        {
            // 关闭 Main Camera（如果不是 Canvas Camera）
            if (mainCamera != null && mainCamera != canvasCamera)
            {
                mainCamera.gameObject.SetActive(false);
                Debug.Log("Main Camera disabled.");
            }

            // 启用 Canvas Camera
            canvasCamera.gameObject.SetActive(true);
            Debug.Log("Canvas Camera enabled: " + canvasCamera.name);

            // 强制设置 Canvas 使用 Canvas Camera
            avatarCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            avatarCanvas.worldCamera = canvasCamera;
            Debug.Log("Canvas render camera set to: " + canvasCamera.name);
        }
        else
        {
            Debug.LogWarning("Canvas or CanvasCamera not assigned.");
        }
    }
}
