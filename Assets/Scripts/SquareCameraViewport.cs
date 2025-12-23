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

    private void Update()
    {
        // Keep it correct if the browser resizes / changes devicePixelRatio.
        Apply();
    }

    private void Apply()
    {
        const float targetAspect = 1f; // 1:1
        float windowAspect = (float)Screen.width / Screen.height;

        if (windowAspect > targetAspect)
        {
            // Wider than square: pillarbox
            float scaleWidth = targetAspect / windowAspect;
            float xOffset = (1f - scaleWidth) * 0.5f;
            cam.rect = new Rect(xOffset, 0f, scaleWidth, 1f);
        }
        else
        {
            // Taller than square: letterbox
            float scaleHeight = windowAspect / targetAspect;
            float yOffset = (1f - scaleHeight) * 0.5f;
            cam.rect = new Rect(0f, yOffset, 1f, scaleHeight);
        }
    }
}
