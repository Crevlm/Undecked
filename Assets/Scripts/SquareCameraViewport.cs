using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SquareCameraViewport : MonoBehaviour
{
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    private void OnValidate()
    {
        if (cam == null) cam = GetComponent<Camera>();
        Apply();
    }

    private void Update()
    {
        // To support resizing (windowed builds), keep updating.
        Apply();
    }

    private void Apply()
    {
        float targetAspect = 1f; // 1:1 square
        float windowAspect = (float)Screen.width / Screen.height;

        if (windowAspect > targetAspect)
        {
            // Window is wider than target: pillarbox (bars left/right)
            float scaleWidth = targetAspect / windowAspect;
            float xOffset = (1f - scaleWidth) * 0.5f;
            cam.rect = new Rect(xOffset, 0f, scaleWidth, 1f);
        }
        else
        {
            // Window is taller than target: letterbox (bars top/bottom)
            float scaleHeight = windowAspect / targetAspect;
            float yOffset = (1f - scaleHeight) * 0.5f;
            cam.rect = new Rect(0f, yOffset, 1f, scaleHeight);
        }
    }
}

