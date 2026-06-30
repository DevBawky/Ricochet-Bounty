using UnityEngine;

// Periodically checks how many objects with the Ball tag remain in the scene.
// This is a simple polling approach for the current lesson. Later, a BallSpawner or GameManager
// can track the count directly when balls are created and destroyed.
public class BallEndCheckManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] DamageManager damageManager;
    [SerializeField] DamageUI damageUI;

    [Header("Ball Check")]
    [SerializeField] string ballTag = "Ball";
    [SerializeField] float checkInterval = 0.5f;

    bool hasFinalScoreShown;

    void Start()
    {
        FindReferences();
        StartChecking();
    }

    // Finds missing scene references automatically so setup is easier for beginner projects.
    void FindReferences()
    {
        if (damageManager == null)
        {
            damageManager = FindFirstObjectByType<DamageManager>();
        }

        if (damageUI == null)
        {
            damageUI = FindFirstObjectByType<DamageUI>();
        }
    }

    // Starts the repeated Ball count check. InvokeRepeating is used because it is easy to explain.
    void StartChecking()
    {
        if (damageManager == null)
        {
            Debug.LogWarning("[BallEndCheckManager] DamageManager was not found. Final score check stopped.", this);
            return;
        }

        if (damageUI == null)
        {
            Debug.LogWarning("[BallEndCheckManager] DamageUI was not found. Final score check stopped.", this);
            return;
        }

        float safeInterval = Mathf.Max(0.1f, checkInterval);
        InvokeRepeating(nameof(CheckBallCount), 0f, safeInterval);
    }

    // Counts all active GameObjects with the Ball tag. When the count reaches 0, the final score is shown once.
    void CheckBallCount()
    {
        if (hasFinalScoreShown)
        {
            return;
        }

        GameObject[] balls;

        try
        {
            balls = GameObject.FindGameObjectsWithTag(ballTag);
        }
        catch (UnityException)
        {
            Debug.LogWarning($"[BallEndCheckManager] The tag '{ballTag}' does not exist. Final score check stopped.", this);
            CancelInvoke(nameof(CheckBallCount));
            return;
        }

        if (balls.Length > 0)
        {
            return;
        }

        ShowFinalScore();
    }

    // Calculates, displays, and logs the final score, then stops future checks.
    void ShowFinalScore()
    {
        if (damageManager == null || damageUI == null)
        {
            Debug.LogWarning("[BallEndCheckManager] Final score cannot be shown because a reference is missing.", this);
            CancelInvoke(nameof(CheckBallCount));
            return;
        }

        int finalScore = damageManager.CalculateFinalScore();
        damageUI.ShowFinalScore(finalScore);

        Debug.Log($"[BallEndCheckManager] Final Score: {finalScore}", this);

        hasFinalScoreShown = true;
        CancelInvoke(nameof(CheckBallCount));
    }
}
