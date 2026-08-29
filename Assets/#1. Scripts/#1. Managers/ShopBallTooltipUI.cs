using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ShopBallTooltipUI : MonoBehaviour
{
    const string TooltipObjectName = "ToolTip";
    const string BallNameObjectName = "Text | BallName";
    const string BallDescriptionObjectName = "Text | BallDescription";

    [SerializeField, Min(0f)] float cursorOffset = 18f;
    [SerializeField, Min(0f)] float screenPadding = 8f;

    Canvas canvas;
    RectTransform canvasRect;
    RectTransform tooltipRect;
    GameObject tooltipObject;
    TMP_Text ballNameText;
    TMP_Text ballDescriptionText;
    StateManager stateManager;
    Component currentSource;
    bool isVisible;

    public static ShopBallTooltipUI GetOrCreate(Canvas preferredCanvas)
    {
        if (preferredCanvas == null)
        {
            return null;
        }

        ShopBallTooltipUI controller = preferredCanvas.GetComponent<ShopBallTooltipUI>();
        if (controller != null)
        {
            controller.Initialize(preferredCanvas);
            return controller;
        }

        RectTransform tooltip = FindTooltip(preferredCanvas);
        if (tooltip == null)
        {
            Debug.LogWarning($"[ShopBallTooltipUI] '{TooltipObjectName}' was not found under Canvas.", preferredCanvas);
            return null;
        }

        controller = preferredCanvas.gameObject.AddComponent<ShopBallTooltipUI>();
        controller.Initialize(preferredCanvas, tooltip);
        return controller;
    }

    void OnEnable()
    {
        BindStateManager();
    }

    void OnDisable()
    {
        UnbindStateManager();
        HideImmediate();
    }

    void Update()
    {
        if (!isVisible)
        {
            return;
        }

        if (stateManager == null)
        {
            BindStateManager();
        }

        if (stateManager == null || stateManager.CurrentState != GameState.Shop)
        {
            HideImmediate();
            return;
        }

        UpdatePosition(Input.mousePosition);
    }

    public void Show(BallDataSO ballData, ShopBallTooltipTrigger source)
    {
        if (ballData == null || source == null)
        {
            Hide(source);
            return;
        }

        Show(ballData.DisplayName, ballData.Description, source);
    }

    public void Show(string title, string description, Component source)
    {
        if (source == null)
        {
            Hide(null);
            return;
        }

        Initialize(canvas != null ? canvas : source.GetComponentInParent<Canvas>());
        BindStateManager();

        if (stateManager == null || stateManager.CurrentState != GameState.Shop || tooltipObject == null)
        {
            Hide(source);
            return;
        }

        currentSource = source;
        if (ballNameText != null)
        {
            ballNameText.text = title ?? string.Empty;
        }

        if (ballDescriptionText != null)
        {
            ballDescriptionText.text = DescriptionTextFormatter.AddSentenceLineBreaks(description);
        }

        tooltipObject.SetActive(true);
        tooltipRect.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        isVisible = true;
        UpdatePosition(Input.mousePosition);
    }

    public void Hide(Component source)
    {
        if (source != null && currentSource != source)
        {
            return;
        }

        HideImmediate();
    }

    void Initialize(Canvas targetCanvas, RectTransform knownTooltip = null)
    {
        if (targetCanvas == null)
        {
            return;
        }

        canvas = targetCanvas;
        canvasRect = canvas.transform as RectTransform;
        tooltipRect = knownTooltip != null ? knownTooltip : tooltipRect;
        if (tooltipRect == null)
        {
            tooltipRect = FindTooltip(canvas);
        }

        if (tooltipRect == null)
        {
            return;
        }

        tooltipObject = tooltipRect.gameObject;
        TMP_Text[] texts = tooltipObject.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == BallNameObjectName)
            {
                ballNameText = texts[i];
            }
            else if (texts[i].name == BallDescriptionObjectName)
            {
                ballDescriptionText = texts[i];
            }
        }

        Graphic[] graphics = tooltipObject.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = false;
        }

        if (!isVisible)
        {
            tooltipObject.SetActive(false);
        }
    }

    void BindStateManager()
    {
        StateManager foundStateManager = stateManager;
        if (foundStateManager == null)
        {
            foundStateManager = FindFirstObjectByType<StateManager>(FindObjectsInactive.Include);
        }

        if (foundStateManager == stateManager)
        {
            return;
        }

        UnbindStateManager();
        stateManager = foundStateManager;
        if (stateManager != null)
        {
            stateManager.StateChanged += HandleStateChanged;
        }
    }

    void UnbindStateManager()
    {
        if (stateManager != null)
        {
            stateManager.StateChanged -= HandleStateChanged;
            stateManager = null;
        }
    }

    void HandleStateChanged(GameState previousState, GameState nextState)
    {
        if (nextState != GameState.Shop)
        {
            HideImmediate();
        }
    }

    void HideImmediate()
    {
        isVisible = false;
        currentSource = null;
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
    }

    void UpdatePosition(Vector2 screenPoint)
    {
        if (tooltipRect == null || canvasRect == null)
        {
            return;
        }

        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, eventCamera, out Vector2 cursorLocal))
        {
            return;
        }

        Rect canvasBounds = canvasRect.rect;
        float width = tooltipRect.rect.width;
        float height = tooltipRect.rect.height;
        float scaleFactor = Mathf.Max(0.0001f, canvas.scaleFactor);
        float localOffset = cursorOffset / scaleFactor;
        float localPadding = screenPadding / scaleFactor;

        float pivotX = cursorLocal.x + localOffset + width * tooltipRect.pivot.x;
        float tooltipRight = pivotX + width * (1f - tooltipRect.pivot.x);
        if (tooltipRight > canvasBounds.xMax - localPadding)
        {
            pivotX = cursorLocal.x - localOffset - width * (1f - tooltipRect.pivot.x);
        }

        float minX = canvasBounds.xMin + localPadding + width * tooltipRect.pivot.x;
        float maxX = canvasBounds.xMax - localPadding - width * (1f - tooltipRect.pivot.x);
        float pivotY = cursorLocal.y + height * (tooltipRect.pivot.y - 0.5f);
        float minY = canvasBounds.yMin + localPadding + height * tooltipRect.pivot.y;
        float maxY = canvasBounds.yMax - localPadding - height * (1f - tooltipRect.pivot.y);

        if (minX <= maxX)
        {
            pivotX = Mathf.Clamp(pivotX, minX, maxX);
        }

        if (minY <= maxY)
        {
            pivotY = Mathf.Clamp(pivotY, minY, maxY);
        }

        Vector3 localPosition = tooltipRect.localPosition;
        tooltipRect.localPosition = new Vector3(pivotX, pivotY, localPosition.z);
    }

    static RectTransform FindTooltip(Canvas targetCanvas)
    {
        RectTransform[] rectTransforms = targetCanvas.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rectTransforms.Length; i++)
        {
            if (rectTransforms[i].name == TooltipObjectName)
            {
                return rectTransforms[i];
            }
        }

        return null;
    }
}

public static class DescriptionTextFormatter
{
    static readonly Regex SentenceBoundary = new Regex(
        @"(?<=[.!?。！？])[ \t]+(?=\S)",
        RegexOptions.Compiled);

    public static string AddSentenceLineBreaks(string text)
    {
        return string.IsNullOrEmpty(text)
            ? text ?? string.Empty
            : SentenceBoundary.Replace(text, "\n");
    }
}
