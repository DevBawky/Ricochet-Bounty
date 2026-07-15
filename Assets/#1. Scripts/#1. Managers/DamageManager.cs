using UnityEngine;
using UnityEngine.Events;

// Manages the accumulated Chips and Multiplier values.
// This class does not know about UI. It only changes values and sends an event when they change.
public class DamageManager : MonoBehaviour
{
    [Header("Initial Values")]
    [SerializeField] int initialChips = 0;
    [SerializeField] float initialMultiplier = 1f;

    [Header("Events")]
    [SerializeField] UnityEvent onDamageValueChanged = new UnityEvent();

    int currentChips;
    float currentMultiplier;
    PlayerUpgradeManager upgradeManager;

    // Other scripts can subscribe to this event and refresh their own UI or logic.
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

    public int CurrentChips
    {
        get
        {
            return currentChips;
        }
    }

    public float CurrentMultiplier
    {
        get
        {
            return currentMultiplier;
        }
    }

    void Awake()
    {
        ResetScore();
    }

    // Adds Chips. Zero or negative values are ignored because this manager only accumulates damage values.
    public void AddChips(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[DamageManager] AddChips ignored. Amount must be greater than 0. Amount: {amount}", this);
            return;
        }

        int appliedAmount = ShouldDoubleScore() ? amount * 2 : amount;
        currentChips += appliedAmount;
        OnDamageValueChanged.Invoke();

        Debug.Log($"[DamageManager] Chips Added: +{appliedAmount} / Current Chips: {currentChips}", this);
    }

    // Adds Multiplier. The final score is calculated later by Chips * Multiplier.
    public void AddMultiplier(float amount)
    {
        if (amount <= 0f)
        {
            Debug.LogWarning($"[DamageManager] AddMultiplier ignored. Amount must be greater than 0. Amount: {amount}", this);
            return;
        }

        float appliedAmount = ShouldDoubleScore() ? amount * 2f : amount;
        currentMultiplier += appliedAmount;
        OnDamageValueChanged.Invoke();

        Debug.Log($"[DamageManager] Multiplier Added: +{appliedAmount} / Current Multiplier: {currentMultiplier}", this);
    }

    // Applies a BallDataSO value based on whether the ball gives Chips or Multiplier.
    public void ApplyBallData(BallDataSO data)
    {
        if (data == null)
        {
            Debug.LogWarning("[DamageManager] ApplyBallData ignored. BallDataSO is null.", this);
            return;
        }

        if (data.ValueType == DamageValueType.Chips)
        {
            int chipsAmount = Mathf.RoundToInt(data.Score);
            AddChips(chipsAmount);
            return;
        }

        if (data.ValueType == DamageValueType.Multiplier)
        {
            AddMultiplier(data.Score);
            return;
        }
    }

    // Calculates the final score after all balls have disappeared.
    public int CalculateFinalScore()
    {
        float finalScoreFloat = currentChips * currentMultiplier;
        int finalScore = Mathf.RoundToInt(finalScoreFloat);

        Debug.Log($"[DamageManager] Final Score Calculated. Chips: {currentChips}, Multiplier: {currentMultiplier}, Final Score: {finalScore}", this);

        return finalScore;
    }

    // Restores the score values to their Inspector defaults.
    public void ResetScore()
    {
        currentChips = initialChips;
        currentMultiplier = initialMultiplier;
        OnDamageValueChanged.Invoke();

        Debug.Log($"[DamageManager] Score Reset. Chips: {currentChips}, Multiplier: {currentMultiplier}", this);
    }

    public void RestoreScore(int chips, float multiplier)
    {
        currentChips = Mathf.Max(0, chips);
        currentMultiplier = Mathf.Max(0f, multiplier);
        OnDamageValueChanged.Invoke();
        Debug.Log($"[DamageManager] Score restored. Chips: {currentChips}, Multiplier: {currentMultiplier}", this);
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

        // Each public score-add operation reaches this method exactly once.
        return upgradeManager != null && upgradeManager.ShouldDoubleScore();
    }
}
