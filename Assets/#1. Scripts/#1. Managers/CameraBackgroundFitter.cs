using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CameraBackgroundFitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Camera targetCamera;
    [SerializeField] SpriteRenderer backgroundRenderer;

    [Header("Fit")]
    [SerializeField, Min(0f), Tooltip("Extra viewport coverage added on every axis. 0.05 means 5 percent overscan.")]
    float overscan = 0.05f;
    [SerializeField, Tooltip("Scales uniformly and crops the excess instead of stretching the sprite.")]
    bool preserveAspect = true;
    [SerializeField, Tooltip("Keeps the background centered on the camera while the Battle Grid moves the camera.")]
    bool followCameraPosition = true;

    void Reset()
    {
        backgroundRenderer = GetComponent<SpriteRenderer>();
        targetCamera = Camera.main;
    }

    void Awake()
    {
        CacheReferences();
        FitToCamera();
    }

    void OnEnable()
    {
        CacheReferences();
        FitToCamera();
    }

    void LateUpdate()
    {
        FitToCamera();
    }

    [ContextMenu("Fit Background To Camera")]
    public void FitToCamera()
    {
        CacheReferences();
        if (targetCamera == null || !targetCamera.orthographic ||
            backgroundRenderer == null || backgroundRenderer.sprite == null)
        {
            return;
        }

        float coverage = 1f + Mathf.Max(0f, overscan);
        float targetHeight = targetCamera.orthographicSize * 2f * coverage;
        float targetWidth = targetHeight * targetCamera.aspect;
        Vector2 spriteSize = backgroundRenderer.sprite.bounds.size;
        if (spriteSize.x <= Mathf.Epsilon || spriteSize.y <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        float parentScaleX = Mathf.Max(Mathf.Abs(parentScale.x), Mathf.Epsilon);
        float parentScaleY = Mathf.Max(Mathf.Abs(parentScale.y), Mathf.Epsilon);
        Vector3 localScale = transform.localScale;

        if (preserveAspect)
        {
            float requiredWorldScale = Mathf.Max(targetWidth / spriteSize.x, targetHeight / spriteSize.y);
            localScale.x = requiredWorldScale / parentScaleX;
            localScale.y = requiredWorldScale / parentScaleY;
        }
        else
        {
            localScale.x = targetWidth / spriteSize.x / parentScaleX;
            localScale.y = targetHeight / spriteSize.y / parentScaleY;
        }

        transform.localScale = localScale;

        if (followCameraPosition)
        {
            Vector3 position = transform.position;
            position.x = targetCamera.transform.position.x;
            position.y = targetCamera.transform.position.y;
            transform.position = position;
        }
    }

    void CacheReferences()
    {
        if (backgroundRenderer == null)
        {
            backgroundRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }
}
