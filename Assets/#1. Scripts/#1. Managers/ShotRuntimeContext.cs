using UnityEngine;

// Keeps values that must survive until every ball in the current shot is gone.
// Add this to an existing scene manager; no manager is created at runtime.
public class ShotRuntimeContext : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("Multiplier granted per gold earned when at least one Dividend Ball participated in the shot.")]
    float dividendMultiplierPerGold = 0.2f;

    public int GoldEarnedThisShot { get; private set; }
    public int DividendBallCount { get; private set; }
    public bool DividendMultiplierApplied { get; private set; }

    public void ResetShot()
    {
        GoldEarnedThisShot = 0;
        DividendBallCount = 0;
        DividendMultiplierApplied = false;
    }

    public void RecordGoldEarned(int amount)
    {
        if (amount > 0)
        {
            GoldEarnedThisShot += amount;
        }
    }

    public void RegisterDividendBall()
    {
        DividendBallCount++;
    }

    public void ApplyDividendBonus(DamageManager damageManager)
    {
        if (damageManager == null || DividendMultiplierApplied)
        {
            return;
        }

        // This flag guards the complete finalization so repeated resolve calls cannot duplicate either reward.
        DividendMultiplierApplied = true;

        int participatingDividendBalls = Mathf.Min(DividendBallCount, 3);
        if (participatingDividendBalls <= 0 || GoldEarnedThisShot <= 0)
        {
            return;
        }

        int bonusChips = GoldEarnedThisShot * 4 * participatingDividendBalls;
        damageManager.AddChips(bonusChips);

        float bonusMultiplier = GoldEarnedThisShot * Mathf.Max(0f, dividendMultiplierPerGold);
        if (bonusMultiplier > 0f)
        {
            damageManager.AddMultiplier(bonusMultiplier);
        }

        Debug.Log($"[ShotRuntimeContext] Dividend finalized. Gold: {GoldEarnedThisShot}, Balls: {participatingDividendBalls}, Chips: +{bonusChips}, Mult: +{bonusMultiplier:0.##}", this);
    }
}
