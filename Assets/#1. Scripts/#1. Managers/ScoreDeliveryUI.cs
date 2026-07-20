using System.Collections;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreDeliveryUI : MonoBehaviour
{
    enum DeliveryType
    {
        Chips,
        Multiplier,
        Damage
    }

    sealed class DeliveryRecord
    {
        public DeliveryType Type;
        public int Generation;
        public Action Arrived;
    }

    [Header("Required References")]
    [SerializeField] Canvas canvas;
    [SerializeField] RectTransform travelRoot;
    [SerializeField] UIValueTravelImage travelImagePrefab;
    [SerializeField] Camera worldCamera;
    [SerializeField] DamageUI damageUI;

    [Header("Score Destinations")]
    [SerializeField] RectTransform chipsTarget;
    [SerializeField] RectTransform multiplierTarget;

    [Header("Damage Route")]
    [SerializeField] RectTransform damageSource;
    [SerializeField] Image enemyHpFillImage;
    [SerializeField] TMP_Text enemyHpText;

    [Header("Travel Durations")]
    [SerializeField, Min(0.01f)] float chipsImageTravelDuration = 0.6f;
    [SerializeField, Min(0.01f)] float multiplierImageTravelDuration = 0.7f;
    [SerializeField, Min(0.01f)] float damageImageTravelDuration = 0.8f;

    [Header("Sin Wave")]
    [SerializeField, Min(0f)] float minimumWaveAmplitude = 20f;
    [SerializeField, Min(0f)] float maximumWaveAmplitude = 70f;
    [SerializeField, Min(0f)] float minimumWaveFrequency = 0.75f;
    [SerializeField, Min(0f)] float maximumWaveFrequency = 2f;
    [SerializeField] bool randomizeStartPhase = true;
    [SerializeField] bool randomizeWaveDirection = true;

    [Header("Image Colors")]
    [SerializeField] Color chipsImageColor = new Color(1f, 0.82f, 0.15f, 1f);
    [SerializeField] Color multiplierImageColor = new Color(0.35f, 0.9f, 1f, 1f);
    [SerializeField] Color damageImageColor = new Color(1f, 0.25f, 0.25f, 1f);

    [Header("Damage Distribution")]
    [SerializeField, Min(1)] int damagePerImage = 30;
    [SerializeField, Range(1, 100)] int maximumDamageImageCount = 100;
    [SerializeField, Min(0.01f)] float damageImageSpawnInterval = 0.03f;

    [Header("Enemy HP Lerp")]
    [SerializeField, Min(0.01f)] float enemyHpLerpDuration = 0.45f;

    readonly Dictionary<UIValueTravelImage, DeliveryRecord> activeImages =
        new Dictionary<UIValueTravelImage, DeliveryRecord>();

    int generation;
    int pendingScoreImageCount;
    int pendingChipsImageCount;
    int pendingMultiplierImageCount;
    int pendingDamageImageCount;
    int expectedDamage;
    int appliedDamage;
    float displayedEnemyHealth;
    float targetEnemyHealth;
    float maximumEnemyHealth = 1f;
    float enemyHealthUnitsPerSecond;
    bool enemyHealthLerpComplete = true;
    bool damageSpawnComplete = true;
    bool warnedAboutMissingReferences;
    Coroutine damageSpawnCoroutine;

    public int PendingScoreImageCount => pendingScoreImageCount;
    public int PendingChipsImageCount => pendingChipsImageCount;
    public int PendingMultiplierImageCount => pendingMultiplierImageCount;
    public int PendingDamageImageCount => pendingDamageImageCount;
    public bool IsScoreDeliveryComplete => pendingScoreImageCount == 0 &&
        (damageUI == null || damageUI.IsScoreLerpComplete);
    public bool IsDamageDeliveryComplete => pendingDamageImageCount == 0 &&
        damageSpawnComplete && appliedDamage == expectedDamage && enemyHealthLerpComplete &&
        (damageUI == null || damageUI.IsFinalDamageSpendComplete);

    void Awake()
    {
        ValidateSettings();
    }

    void Update()
    {
        UpdateEnemyHealthPresentation();
    }

    void OnDisable()
    {
        CancelAllDeliveries();
    }

    void OnValidate()
    {
        ValidateSettings();
    }

    public bool TryQueueChips(int amount, Vector3 worldPosition, Action onArrived)
    {
        return TryStartWorldDelivery(
            DeliveryType.Chips,
            amount,
            worldPosition,
            chipsTarget,
            chipsImageTravelDuration,
            chipsImageColor,
            onArrived);
    }

    public bool TryQueueMultiplier(float amount, Vector3 worldPosition, Action onArrived)
    {
        return TryStartWorldDelivery(
            DeliveryType.Multiplier,
            amount,
            worldPosition,
            multiplierTarget,
            multiplierImageTravelDuration,
            multiplierImageColor,
            onArrived);
    }

    public bool BeginDamageDelivery(int finalDamage, int currentHealth, int maximumHealth, Action<int> applyDamage)
    {
        if (!CanCreateImage() || damageSource == null || enemyHpFillImage == null || applyDamage == null)
        {
            WarnMissingReferences("Damage delivery");
            return false;
        }

        CancelDamageDeliveries();
        expectedDamage = Mathf.Max(0, finalDamage);
        appliedDamage = 0;
        SetEnemyHealthImmediate(currentHealth, maximumHealth);

        int safeDamagePerImage = Mathf.Max(1, damagePerImage);
        int safeMaximumCount = Mathf.Clamp(maximumDamageImageCount, 1, 100);
        int imageCount = Mathf.Clamp(Mathf.CeilToInt(expectedDamage / (float)safeDamagePerImage), 1, safeMaximumCount);
        int baseDamage = expectedDamage / imageCount;
        int remainder = expectedDamage % imageCount;

        if (!TryGetLocalPoint(damageSource, out Vector2 start) ||
            !TryGetLocalPoint(enemyHpFillImage.rectTransform, out Vector2 destination))
        {
            WarnMissingReferences("Damage route coordinate conversion");
            return false;
        }

        int[] distributedDamage = new int[imageCount];
        for (int i = 0; i < imageCount; i++)
        {
            distributedDamage[i] = baseDamage + (i < remainder ? 1 : 0);
        }

        damageSpawnComplete = false;
        int deliveryGeneration = generation;
        damageSpawnCoroutine = StartCoroutine(SpawnDamageImagesRoutine(
            distributedDamage,
            start,
            destination,
            deliveryGeneration,
            applyDamage));
        return true;
    }

    public void SetEnemyHealthTarget(int currentHealth, int maximumHealth)
    {
        maximumEnemyHealth = Mathf.Max(1, maximumHealth);
        float newTarget = Mathf.Clamp(currentHealth, 0, maximumHealth);
        float distance = Mathf.Abs(displayedEnemyHealth - newTarget);
        float requiredSpeed = distance / Mathf.Max(0.01f, enemyHpLerpDuration);
        enemyHealthUnitsPerSecond = Mathf.Max(enemyHealthUnitsPerSecond, requiredSpeed);
        targetEnemyHealth = newTarget;
        enemyHealthLerpComplete = Mathf.Approximately(displayedEnemyHealth, targetEnemyHealth);
        RefreshEnemyHealthText();
    }

    public void SetEnemyHealthImmediate(int currentHealth, int maximumHealth)
    {
        maximumEnemyHealth = Mathf.Max(1, maximumHealth);
        displayedEnemyHealth = Mathf.Clamp(currentHealth, 0, maximumHealth);
        targetEnemyHealth = displayedEnemyHealth;
        enemyHealthUnitsPerSecond = 0f;
        enemyHealthLerpComplete = true;
        RefreshEnemyHealthVisuals();
    }

    public void CancelAllDeliveries()
    {
        generation++;
        if (damageSpawnCoroutine != null)
        {
            StopCoroutine(damageSpawnCoroutine);
            damageSpawnCoroutine = null;
        }

        UIValueTravelImage[] images = new UIValueTravelImage[activeImages.Count];
        activeImages.Keys.CopyTo(images, 0);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null)
            {
                images[i].Cancel();
            }
        }

        activeImages.Clear();
        pendingScoreImageCount = 0;
        pendingChipsImageCount = 0;
        pendingMultiplierImageCount = 0;
        pendingDamageImageCount = 0;
        expectedDamage = 0;
        appliedDamage = 0;
        damageSpawnComplete = true;
        enemyHealthUnitsPerSecond = 0f;
        enemyHealthLerpComplete = true;
        damageUI?.CancelFinalDamagePresentation();
    }

    void CancelDamageDeliveries()
    {
        generation++;
        if (damageSpawnCoroutine != null)
        {
            StopCoroutine(damageSpawnCoroutine);
            damageSpawnCoroutine = null;
        }

        List<UIValueTravelImage> damageImages = new List<UIValueTravelImage>();
        foreach (KeyValuePair<UIValueTravelImage, DeliveryRecord> pair in activeImages)
        {
            if (pair.Value.Type == DeliveryType.Damage)
            {
                damageImages.Add(pair.Key);
            }
        }

        for (int i = 0; i < damageImages.Count; i++)
        {
            if (damageImages[i] != null)
            {
                damageImages[i].Cancel();
            }
        }

        pendingDamageImageCount = 0;
        expectedDamage = 0;
        appliedDamage = 0;
        damageSpawnComplete = true;
    }

    IEnumerator SpawnDamageImagesRoutine(
        int[] distributedDamage,
        Vector2 start,
        Vector2 destination,
        int deliveryGeneration,
        Action<int> applyDamage)
    {
        float spawnInterval = Mathf.Max(0.01f, damageImageSpawnInterval);
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(spawnInterval);

        for (int i = 0; i < distributedDamage.Length; i++)
        {
            if (deliveryGeneration != generation)
            {
                yield break;
            }

            int capturedDamage = distributedDamage[i];
            bool started = StartDelivery(
                DeliveryType.Damage,
                capturedDamage,
                start,
                destination,
                damageImageTravelDuration,
                damageImageColor,
                () =>
                {
                    applyDamage(capturedDamage);
                    appliedDamage += capturedDamage;
                });

            if (!started)
            {
                Debug.LogWarning("[ScoreDeliveryUI] Damage image spawning stopped because an image could not be created.", this);
                damageSpawnComplete = true;
                damageSpawnCoroutine = null;
                yield break;
            }

            damageUI?.SpendFinalDamage(capturedDamage, spawnInterval);

            if (i < distributedDamage.Length - 1)
            {
                yield return wait;
            }
        }

        damageSpawnComplete = true;
        damageSpawnCoroutine = null;
    }

    bool TryStartWorldDelivery(
        DeliveryType type,
        float value,
        Vector3 worldPosition,
        RectTransform destinationTransform,
        float baseDuration,
        Color color,
        Action onArrived)
    {
        if (!CanCreateImage() || destinationTransform == null)
        {
            WarnMissingReferences($"{type} delivery");
            return false;
        }

        if (!TryWorldToLocalPoint(worldPosition, out Vector2 start) ||
            !TryGetLocalPoint(destinationTransform, out Vector2 destination))
        {
            WarnMissingReferences($"{type} coordinate conversion");
            return false;
        }

        return StartDelivery(type, value, start, destination, baseDuration, color, onArrived);
    }

    bool StartDelivery(
        DeliveryType type,
        float value,
        Vector2 start,
        Vector2 destination,
        float baseDuration,
        Color color,
        Action onArrived)
    {
        UIValueTravelImage image = Instantiate(travelImagePrefab, travelRoot, false);
        if (image == null)
        {
            return false;
        }

        RectTransform imageRect = image.GetComponent<RectTransform>();
        imageRect.anchorMin = travelRoot.pivot;
        imageRect.anchorMax = travelRoot.pivot;
        image.transform.SetAsLastSibling();
        DeliveryRecord record = new DeliveryRecord
        {
            Type = type,
            Generation = generation,
            Arrived = onArrived
        };
        activeImages.Add(image, record);

        if (type == DeliveryType.Damage)
        {
            pendingDamageImageCount++;
        }
        else
        {
            pendingScoreImageCount++;
            if (type == DeliveryType.Chips)
            {
                pendingChipsImageCount++;
            }
            else
            {
                pendingMultiplierImageCount++;
            }
        }

        GetOrderedRange(minimumWaveAmplitude, maximumWaveAmplitude, out float minAmplitude, out float maxAmplitude);
        GetOrderedRange(minimumWaveFrequency, maximumWaveFrequency, out float minFrequency, out float maxFrequency);
        float duration = Mathf.Max(0.01f, baseDuration) * UnityEngine.Random.Range(0.8f, 1.2f);
        float amplitude = UnityEngine.Random.Range(minAmplitude, maxAmplitude);
        float frequency = UnityEngine.Random.Range(minFrequency, maxFrequency);
        float phase = randomizeStartPhase ? UnityEngine.Random.Range(0f, Mathf.PI * 2f) : 0f;
        float direction = !randomizeWaveDirection || UnityEngine.Random.value >= 0.5f ? 1f : -1f;

        image.Initialize(
            start,
            destination,
            value,
            duration,
            amplitude,
            frequency,
            phase,
            direction,
            color,
            HandleImageFinished);
        return true;
    }

    void HandleImageFinished(UIValueTravelImage image, bool arrived)
    {
        if (image == null || !activeImages.TryGetValue(image, out DeliveryRecord record))
        {
            return;
        }

        activeImages.Remove(image);
        if (record.Type == DeliveryType.Damage)
        {
            pendingDamageImageCount = Mathf.Max(0, pendingDamageImageCount - 1);
        }
        else
        {
            pendingScoreImageCount = Mathf.Max(0, pendingScoreImageCount - 1);
            if (record.Type == DeliveryType.Chips)
            {
                pendingChipsImageCount = Mathf.Max(0, pendingChipsImageCount - 1);
            }
            else
            {
                pendingMultiplierImageCount = Mathf.Max(0, pendingMultiplierImageCount - 1);
            }
        }

        if (arrived && record.Generation == generation)
        {
            record.Arrived?.Invoke();
        }
    }

    void UpdateEnemyHealthPresentation()
    {
        if (enemyHealthLerpComplete)
        {
            return;
        }

        float speed = Mathf.Max(0.01f, enemyHealthUnitsPerSecond);
        displayedEnemyHealth = Mathf.MoveTowards(
            displayedEnemyHealth,
            targetEnemyHealth,
            speed * Time.unscaledDeltaTime);

        if (Mathf.Approximately(displayedEnemyHealth, targetEnemyHealth))
        {
            displayedEnemyHealth = targetEnemyHealth;
            enemyHealthUnitsPerSecond = 0f;
            enemyHealthLerpComplete = true;
        }

        RefreshEnemyHealthVisuals();
    }

    void RefreshEnemyHealthVisuals()
    {
        if (enemyHpFillImage != null)
        {
            enemyHpFillImage.fillAmount = Mathf.Clamp01(displayedEnemyHealth / Mathf.Max(1f, maximumEnemyHealth));
        }

        RefreshEnemyHealthText();
    }

    void RefreshEnemyHealthText()
    {
        if (enemyHpText != null)
        {
            enemyHpText.text = $"{Mathf.RoundToInt(targetEnemyHealth)} / {Mathf.RoundToInt(maximumEnemyHealth)}";
        }
    }

    bool TryWorldToLocalPoint(Vector3 worldPosition, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (canvas == null || travelRoot == null)
        {
            return false;
        }

        Camera sourceCamera = worldCamera;
        if (sourceCamera == null)
        {
            sourceCamera = Camera.main;
        }

        if (sourceCamera == null)
        {
            return false;
        }

        Vector2 screenPoint = sourceCamera.WorldToScreenPoint(worldPosition);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            travelRoot,
            screenPoint,
            GetCanvasCamera(),
            out localPoint);
    }

    bool TryGetLocalPoint(RectTransform source, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (source == null || travelRoot == null)
        {
            return false;
        }

        Vector3 sourceWorldCenter = source.TransformPoint(source.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(), sourceWorldCenter);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            travelRoot,
            screenPoint,
            GetCanvasCamera(),
            out localPoint);
    }

    Camera GetCanvasCamera()
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    bool CanCreateImage()
    {
        return canvas != null && travelRoot != null && travelImagePrefab != null;
    }

    void WarnMissingReferences(string context)
    {
        if (warnedAboutMissingReferences)
        {
            return;
        }

        warnedAboutMissingReferences = true;
        Debug.LogWarning(
            $"[ScoreDeliveryUI] {context} cannot play. Connect Canvas, Travel Root, UI Value Travel Image prefab, route targets, and the world camera in the Inspector.",
            this);
    }

    void ValidateSettings()
    {
        chipsImageTravelDuration = Mathf.Max(0.01f, chipsImageTravelDuration);
        multiplierImageTravelDuration = Mathf.Max(0.01f, multiplierImageTravelDuration);
        damageImageTravelDuration = Mathf.Max(0.01f, damageImageTravelDuration);
        minimumWaveAmplitude = Mathf.Max(0f, minimumWaveAmplitude);
        maximumWaveAmplitude = Mathf.Max(0f, maximumWaveAmplitude);
        minimumWaveFrequency = Mathf.Max(0f, minimumWaveFrequency);
        maximumWaveFrequency = Mathf.Max(0f, maximumWaveFrequency);
        damagePerImage = Mathf.Max(1, damagePerImage);
        maximumDamageImageCount = Mathf.Clamp(maximumDamageImageCount, 1, 100);
        damageImageSpawnInterval = Mathf.Max(0.01f, damageImageSpawnInterval);
        enemyHpLerpDuration = Mathf.Max(0.01f, enemyHpLerpDuration);
    }

    static void GetOrderedRange(float first, float second, out float minimum, out float maximum)
    {
        minimum = Mathf.Min(first, second);
        maximum = Mathf.Max(first, second);
    }
}
