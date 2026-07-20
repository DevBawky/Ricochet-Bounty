using System;
using UnityEngine;
using UnityEngine.Events;

// Manages score targets and delays value application until its UI delivery image arrives.
public class DamageManager : MonoBehaviour
{
    [Header("Initial Values")]
    [SerializeField] int initialChips = 0;
    [SerializeField] float initialMultiplier = 1f;

    [Header("Events")]
    [SerializeField] UnityEvent onDamageValueChanged = new UnityEvent();
    [SerializeField] UnityEvent onScoreReset = new UnityEvent();

    [Header("Presentation")]
    [SerializeField] ScoreDeliveryUI scoreDeliveryUI;

    int currentChips;
    float currentMultiplier;
    PlayerUpgradeManager upgradeManager;

    public UnityEvent OnDamageValueChanged
    {
        get
        {
            if (onDamageValueChanged == null)
            {
                onDamageValueChanged = new UnityEvent();
            }

            return onDamageValueChanged;
        }
    }

    public UnityEvent OnScoreReset
    {
        get
        {
            if (onScoreReset == null)
            {
                onScoreReset = new UnityEvent();
            }

            return onScoreReset;
        }
    }

    public int CurrentChips => currentChips;
    public float CurrentMultiplier => currentMultiplier;
    public bool IsScorePresentationComplete =>
        scoreDeliveryUI == null || scoreDeliveryUI.IsScoreDeliveryComplete;
    public bool IsFinalDamagePresentationComplete =>
        scoreDeliveryUI == null || scoreDeliveryUI.IsDamageDeliveryComplete;

    void Awake()
    {
        if (scoreDeliveryUI == null)
        {
            scoreDeliveryUI = FindFirstObjectByType<ScoreDeliveryUI>(FindObjectsInactive.Include);
        }

        ResetScore();
    }

    public void AddChips(int amount)
    {
        AddChips(amount, transform.position);
    }

    public void AddChips(int amount, Vector3 worldPosition)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[DamageManager] AddChips ignored. Amount must be greater than 0. Amount: {amount}", this);
            return;
        }

        int appliedAmount = ShouldDoubleScore() ? amount * 2 : amount;
        if (scoreDeliveryUI != null &&
            scoreDeliveryUI.TryQueueChips(appliedAmount, worldPosition, () => ApplyDeliveredChips(appliedAmount)))
        {
            return;
        }

        ApplyDeliveredChips(appliedAmount);
    }

    public void AddMultiplier(float amount)
    {
        AddMultiplier(amount, transform.position);
    }

    public void AddMultiplier(float amount, Vector3 worldPosition)
    {
        if (amount <= 0f)
        {
            Debug.LogWarning($"[DamageManager] AddMultiplier ignored. Amount must be greater than 0. Amount: {amount}", this);
            return;
        }

        float appliedAmount = ShouldDoubleScore() ? amount * 2f : amount;
        if (scoreDeliveryUI != null &&
            scoreDeliveryUI.TryQueueMultiplier(appliedAmount, worldPosition, () => ApplyDeliveredMultiplier(appliedAmount)))
        {
            return;
        }

        ApplyDeliveredMultiplier(appliedAmount);
    }

    public void ApplyBallData(BallDataSO data)
    {
        ApplyBallData(data, transform.position);
    }

    public void ApplyBallData(BallDataSO data, Vector3 worldPosition)
    {
        if (data == null)
        {
            Debug.LogWarning("[DamageManager] ApplyBallData ignored. BallDataSO is null.", this);
            return;
        }

        if (data.ValueType == DamageValueType.Chips)
        {
            AddChips(Mathf.RoundToInt(data.Score), worldPosition);
            return;
        }

        if (data.ValueType == DamageValueType.Multiplier)
        {
            AddMultiplier(data.Score, worldPosition);
        }
    }

    public int CalculateFinalScore()
    {
        int finalScore = Mathf.RoundToInt(currentChips * currentMultiplier);
        Debug.Log($"[DamageManager] Final Score Calculated. Chips: {currentChips}, Multiplier: {currentMultiplier}, Final Score: {finalScore}", this);
        return finalScore;
    }

    public void ResetScore()
    {
        currentChips = initialChips;
        currentMultiplier = initialMultiplier;
        OnDamageValueChanged.Invoke();
        OnScoreReset.Invoke();
        Debug.Log($"[DamageManager] Score Reset. Chips: {currentChips}, Multiplier: {currentMultiplier}", this);
    }

    public void RestoreScore(int chips, float multiplier)
    {
        currentChips = Mathf.Max(0, chips);
        currentMultiplier = Mathf.Max(0f, multiplier);
        OnDamageValueChanged.Invoke();
        OnScoreReset.Invoke();
        Debug.Log($"[DamageManager] Score restored. Chips: {currentChips}, Multiplier: {currentMultiplier}", this);
    }

    public bool BeginFinalDamageDelivery(
        int finalDamage,
        int currentEnemyHealth,
        int maximumEnemyHealth,
        Action<int> applyDamage)
    {
        return scoreDeliveryUI != null && scoreDeliveryUI.BeginDamageDelivery(
            finalDamage,
            currentEnemyHealth,
            maximumEnemyHealth,
            applyDamage);
    }

    public void SetEnemyHealthTarget(int currentEnemyHealth, int maximumEnemyHealth)
    {
        scoreDeliveryUI?.SetEnemyHealthTarget(currentEnemyHealth, maximumEnemyHealth);
    }

    public void SetEnemyHealthImmediate(int currentEnemyHealth, int maximumEnemyHealth)
    {
        scoreDeliveryUI?.SetEnemyHealthImmediate(currentEnemyHealth, maximumEnemyHealth);
    }

    public void CancelPresentation()
    {
        scoreDeliveryUI?.CancelAllDeliveries();
    }

    void ApplyDeliveredChips(int amount)
    {
        currentChips += amount;
        OnDamageValueChanged.Invoke();
        Debug.Log($"[DamageManager] Chips delivered: +{amount} / Current Chips: {currentChips}", this);
    }

    void ApplyDeliveredMultiplier(float amount)
    {
        currentMultiplier += amount;
        OnDamageValueChanged.Invoke();
        Debug.Log($"[DamageManager] Multiplier delivered: +{amount} / Current Multiplier: {currentMultiplier}", this);
    }

    bool ShouldDoubleScore()
    {
        if (upgradeManager == null)
        {
            upgradeManager = PlayerUpgradeManager.Instance;
        }

        if (upgradeManager == null)
        {
            upgradeManager = FindFirstObjectByType<PlayerUpgradeManager>(FindObjectsInactive.Include);
        }

        return upgradeManager != null && upgradeManager.ShouldDoubleScore();
    }
}
