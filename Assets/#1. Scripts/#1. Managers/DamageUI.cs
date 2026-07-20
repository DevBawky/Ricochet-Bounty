using System.Globalization;
using TMPro;
using UnityEngine;

// Displays score targets without restarting its interpolation when more deliveries arrive.
public class DamageUI : MonoBehaviour
{
    [Header("UI Text")]
    [SerializeField] TextMeshProUGUI chipsText;
    [SerializeField] TextMeshProUGUI multiplierText;
    [SerializeField] TextMeshProUGUI finalScoreText;

    [Header("References")]
    [SerializeField] DamageManager damageManager;

    [Header("Score Text Lerp")]
    [SerializeField, Min(0.01f)] float scoreTextLerpDuration = 0.35f;

    [Header("Final Damage Presentation")]
    [SerializeField, Min(0.01f)] float finalDamageCountUpDuration = 0.5f;
    [SerializeField, Min(0f)] float finalDamageParticleDelay = 1f;

    float displayedChips;
    float targetChips;
    float displayedMultiplier;
    float targetMultiplier;
    float chipsUnitsPerSecond;
    float multiplierUnitsPerSecond;
    float displayedFinalDamage;
    float targetFinalDamage;
    float finalDamageUnitsPerSecond;
    bool isInitialized;
    bool finalDamageCountUpComplete = true;

    public bool IsScoreLerpComplete =>
        Mathf.Approximately(displayedChips, targetChips) &&
        Mathf.Approximately(displayedMultiplier, targetMultiplier);
    public bool IsFinalDamageCountUpComplete => finalDamageCountUpComplete;
    public bool IsFinalDamageSpendComplete => targetFinalDamage <= 0f &&
        Mathf.Approximately(displayedFinalDamage, 0f);
    public float FinalDamageParticleDelay => Mathf.Max(0f, finalDamageParticleDelay);

    public RectTransform ChipsTarget => chipsText != null ? chipsText.rectTransform : null;
    public RectTransform MultiplierTarget => multiplierText != null ? multiplierText.rectTransform : null;
    public RectTransform FinalDamageSource => finalScoreText != null ? finalScoreText.rectTransform : null;

    void Awake()
    {
        FindDamageManager();
    }

    void Start()
    {
        SubscribeDamageManagerEvents();
        SnapDisplayedValuesToManager();
        ClearFinalScore();
    }

    void Update()
    {
        UpdateDisplayedScores();
        UpdateDisplayedFinalDamage();
    }

    void OnDestroy()
    {
        if (damageManager == null)
        {
            return;
        }

        damageManager.OnDamageValueChanged.RemoveListener(RefreshCurrentScoreUI);
        damageManager.OnScoreReset.RemoveListener(SnapDisplayedValuesToManager);
    }

    void OnValidate()
    {
        scoreTextLerpDuration = Mathf.Max(0.01f, scoreTextLerpDuration);
        finalDamageCountUpDuration = Mathf.Max(0.01f, finalDamageCountUpDuration);
        finalDamageParticleDelay = Mathf.Max(0f, finalDamageParticleDelay);
    }

    void FindDamageManager()
    {
        if (damageManager == null)
        {
            damageManager = FindFirstObjectByType<DamageManager>();
        }

        if (damageManager == null)
        {
            Debug.LogWarning("[DamageUI] DamageManager was not found. Current score UI cannot refresh.", this);
        }
    }

    void SubscribeDamageManagerEvents()
    {
        if (damageManager == null)
        {
            Debug.LogWarning("[DamageUI] Event subscription skipped because DamageManager is missing.", this);
            return;
        }

        damageManager.OnDamageValueChanged.RemoveListener(RefreshCurrentScoreUI);
        damageManager.OnDamageValueChanged.AddListener(RefreshCurrentScoreUI);
        damageManager.OnScoreReset.RemoveListener(SnapDisplayedValuesToManager);
        damageManager.OnScoreReset.AddListener(SnapDisplayedValuesToManager);
    }

    public void RefreshCurrentScoreUI()
    {
        if (damageManager == null)
        {
            return;
        }

        if (!isInitialized)
        {
            SnapDisplayedValuesToManager();
            return;
        }

        targetChips = damageManager.CurrentChips;
        targetMultiplier = damageManager.CurrentMultiplier;
        chipsUnitsPerSecond = Mathf.Max(
            chipsUnitsPerSecond,
            Mathf.Abs(targetChips - displayedChips) / Mathf.Max(0.01f, scoreTextLerpDuration));
        multiplierUnitsPerSecond = Mathf.Max(
            multiplierUnitsPerSecond,
            Mathf.Abs(targetMultiplier - displayedMultiplier) / Mathf.Max(0.01f, scoreTextLerpDuration));
    }

    public void SnapDisplayedValuesToManager()
    {
        if (damageManager == null)
        {
            return;
        }

        displayedChips = targetChips = damageManager.CurrentChips;
        displayedMultiplier = targetMultiplier = damageManager.CurrentMultiplier;
        chipsUnitsPerSecond = 0f;
        multiplierUnitsPerSecond = 0f;
        isInitialized = true;
        WriteScoreText();
    }

    public void ShowFinalScore(int finalScore)
    {
        if (finalScoreText == null)
        {
            Debug.LogWarning("[DamageUI] Final score text is not connected.", this);
            return;
        }

        displayedFinalDamage = targetFinalDamage = Mathf.Max(0, finalScore);
        finalDamageUnitsPerSecond = 0f;
        finalDamageCountUpComplete = true;
        WriteFinalDamageText();
    }

    public void BeginFinalDamageCountUp(int finalDamage)
    {
        if (finalScoreText == null)
        {
            Debug.LogWarning("[DamageUI] Final score text is not connected.", this);
            finalDamageCountUpComplete = true;
            return;
        }

        displayedFinalDamage = 0f;
        targetFinalDamage = Mathf.Max(0, finalDamage);
        finalDamageUnitsPerSecond = targetFinalDamage / Mathf.Max(0.01f, finalDamageCountUpDuration);
        finalDamageCountUpComplete = targetFinalDamage <= 0f;
        WriteFinalDamageText();
    }

    public void SpendFinalDamage(int amount, float durationUntilNextSpawn)
    {
        int safeAmount = Mathf.Max(0, amount);
        targetFinalDamage = Mathf.Max(0f, targetFinalDamage - safeAmount);
        float distance = Mathf.Abs(displayedFinalDamage - targetFinalDamage);
        float requiredSpeed = distance / Mathf.Max(0.01f, durationUntilNextSpawn);
        finalDamageUnitsPerSecond = Mathf.Max(finalDamageUnitsPerSecond, requiredSpeed);
    }

    public void ClearFinalScore()
    {
        displayedFinalDamage = 0f;
        targetFinalDamage = 0f;
        finalDamageUnitsPerSecond = 0f;
        finalDamageCountUpComplete = true;
        if (finalScoreText != null)
        {
            finalScoreText.text = string.Empty;
        }
    }

    public void CancelFinalDamagePresentation()
    {
        ClearFinalScore();
    }

    void UpdateDisplayedScores()
    {
        if (!isInitialized || IsScoreLerpComplete)
        {
            return;
        }

        displayedChips = Mathf.MoveTowards(
            displayedChips,
            targetChips,
            Mathf.Max(0.01f, chipsUnitsPerSecond) * Time.unscaledDeltaTime);
        displayedMultiplier = Mathf.MoveTowards(
            displayedMultiplier,
            targetMultiplier,
            Mathf.Max(0.01f, multiplierUnitsPerSecond) * Time.unscaledDeltaTime);

        if (Mathf.Approximately(displayedChips, targetChips))
        {
            displayedChips = targetChips;
            chipsUnitsPerSecond = 0f;
        }

        if (Mathf.Approximately(displayedMultiplier, targetMultiplier))
        {
            displayedMultiplier = targetMultiplier;
            multiplierUnitsPerSecond = 0f;
        }

        WriteScoreText();
    }

    void WriteScoreText()
    {
        if (chipsText != null)
        {
            chipsText.text = Mathf.RoundToInt(displayedChips).ToString(CultureInfo.InvariantCulture);
        }

        if (multiplierText != null)
        {
            multiplierText.text = FormatScore(displayedMultiplier);
        }
    }

    void UpdateDisplayedFinalDamage()
    {
        if (finalScoreText == null || Mathf.Approximately(displayedFinalDamage, targetFinalDamage))
        {
            if (!finalDamageCountUpComplete && Mathf.Approximately(displayedFinalDamage, targetFinalDamage))
            {
                displayedFinalDamage = targetFinalDamage;
                finalDamageUnitsPerSecond = 0f;
                finalDamageCountUpComplete = true;
                WriteFinalDamageText();
            }

            return;
        }

        displayedFinalDamage = Mathf.MoveTowards(
            displayedFinalDamage,
            targetFinalDamage,
            Mathf.Max(0.01f, finalDamageUnitsPerSecond) * Time.unscaledDeltaTime);

        if (Mathf.Approximately(displayedFinalDamage, targetFinalDamage))
        {
            displayedFinalDamage = targetFinalDamage;
            finalDamageUnitsPerSecond = 0f;
            finalDamageCountUpComplete = true;
        }

        WriteFinalDamageText();
    }

    void WriteFinalDamageText()
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = Mathf.RoundToInt(displayedFinalDamage).ToString(CultureInfo.InvariantCulture);
        }
    }

    static string FormatScore(float value)
    {
        return value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
