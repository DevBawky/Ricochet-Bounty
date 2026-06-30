using TMPro;
using UnityEngine;

// Displays the current damage values and the final score on TextMeshPro UI text objects.
// Attach this script to a UI object under a Canvas, then connect the three text fields in the Inspector.
public class DamageUI : MonoBehaviour
{
    [Header("UI Text")]
    [SerializeField] TextMeshProUGUI chipsText;
    [SerializeField] TextMeshProUGUI multiplierText;
    [SerializeField] TextMeshProUGUI finalScoreText;

    [Header("References")]
    [SerializeField] DamageManager damageManager;

    void Awake()
    {
        FindDamageManager();
    }

    void Start()
    {
        SubscribeDamageManagerEvent();
        RefreshCurrentScoreUI();
        ClearFinalScore();
    }

    void OnDestroy()
    {
        if (damageManager == null)
        {
            return;
        }

        damageManager.OnDamageValueChanged.RemoveListener(RefreshCurrentScoreUI);
    }

    // Finds the DamageManager automatically when it was not connected in the Inspector.
    void FindDamageManager()
    {
        if (damageManager != null)
        {
            return;
        }

        damageManager = FindFirstObjectByType<DamageManager>();

        if (damageManager == null)
        {
            Debug.LogWarning("[DamageUI] DamageManager was not found. Current score UI cannot refresh.", this);
        }
    }

    // Subscribes to the DamageManager event so the UI refreshes whenever chips or multiplier changes.
    void SubscribeDamageManagerEvent()
    {
        if (damageManager == null)
        {
            Debug.LogWarning("[DamageUI] Event subscription skipped because DamageManager is missing.", this);
            return;
        }

        damageManager.OnDamageValueChanged.RemoveListener(RefreshCurrentScoreUI);
        damageManager.OnDamageValueChanged.AddListener(RefreshCurrentScoreUI);
    }

    // Reads the current values from DamageManager and writes them to the UI.
    public void RefreshCurrentScoreUI()
    {
        if (damageManager == null)
        {
            Debug.LogWarning("[DamageUI] RefreshCurrentScoreUI skipped because DamageManager is missing.", this);
            return;
        }

        if (chipsText != null)
        {
            chipsText.text = $"Chips : {damageManager.CurrentChips}";
        }

        if (multiplierText != null)
        {
            multiplierText.text = $"Mult : x{damageManager.CurrentMultiplier}";
        }
    }

    // Shows the final score after every Ball-tagged object has disappeared from the scene.
    public void ShowFinalScore(int finalScore)
    {
        if (finalScoreText == null)
        {
            Debug.LogWarning("[DamageUI] Final score text is not connected.", this);
            return;
        }

        finalScoreText.text = $"Final Score : {finalScore}";
    }

    // Clears the final score text at the start of play or when a new round begins.
    public void ClearFinalScore()
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = string.Empty;
        }
    }
}
