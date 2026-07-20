using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))]
public class UIValueTravelImage : MonoBehaviour
{
    RectTransform rectTransform;
    Action<UIValueTravelImage, bool> finishedCallback;
    Vector2 startPosition;
    Vector2 destinationPosition;
    float travelDuration;
    float waveAmplitude;
    float waveFrequency;
    float startPhase;
    float waveDirection;
    float elapsedTime;
    bool isRunning;
    bool hasFinished;

    public float Value { get; private set; }
    public Vector2 StartPosition => startPosition;
    public Vector2 DestinationPosition => destinationPosition;
    public float TravelDuration => travelDuration;
    public float WaveAmplitude => waveAmplitude;
    public float WaveFrequency => waveFrequency;
    public float StartPhase => startPhase;
    public float WaveDirection => waveDirection;

    public void Initialize(
        Vector2 start,
        Vector2 destination,
        float value,
        float duration,
        float amplitude,
        float frequency,
        float phase,
        float direction,
        Color color,
        Action<UIValueTravelImage, bool> onFinished)
    {
        rectTransform = GetComponent<RectTransform>();
        Image image = GetComponent<Image>();

        startPosition = start;
        destinationPosition = destination;
        Value = value;
        travelDuration = Mathf.Max(0.01f, duration);
        waveAmplitude = Mathf.Max(0f, amplitude);
        waveFrequency = Mathf.Max(0f, frequency);
        startPhase = phase;
        waveDirection = direction < 0f ? -1f : 1f;
        finishedCallback = onFinished;
        elapsedTime = 0f;
        hasFinished = false;
        isRunning = true;

        rectTransform.anchoredPosition = startPosition;
        image.color = color;
        image.raycastTarget = false;
        gameObject.SetActive(true);
    }

    public void Cancel()
    {
        Finish(false);
    }

    void Update()
    {
        if (!isRunning || hasFinished)
        {
            return;
        }

        elapsedTime += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsedTime / travelDuration);
        Vector2 basePosition = Vector2.LerpUnclamped(startPosition, destinationPosition, progress);
        Vector2 path = destinationPosition - startPosition;
        Vector2 perpendicular = path.sqrMagnitude > 0.0001f
            ? new Vector2(-path.y, path.x).normalized
            : Vector2.up;
        float wave = Mathf.Sin(startPhase + progress * waveFrequency * Mathf.PI * 2f);
        float fadeNearDestination = 1f - progress;

        rectTransform.anchoredPosition = basePosition +
            perpendicular * (wave * waveAmplitude * waveDirection * fadeNearDestination);

        if (progress >= 1f)
        {
            rectTransform.anchoredPosition = destinationPosition;
            Finish(true);
        }
    }

    void OnDisable()
    {
        if (isRunning && !hasFinished)
        {
            Finish(false);
        }
    }

    void OnDestroy()
    {
        if (isRunning && !hasFinished)
        {
            Finish(false);
        }
    }

    void Finish(bool arrived)
    {
        if (hasFinished)
        {
            return;
        }

        hasFinished = true;
        isRunning = false;
        Action<UIValueTravelImage, bool> callback = finishedCallback;
        finishedCallback = null;
        callback?.Invoke(this, arrived);

        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}
