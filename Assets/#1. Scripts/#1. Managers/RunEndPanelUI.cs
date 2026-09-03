using TMPro;
using UnityEngine;

public class RunEndPanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] StateManager stateManager;
    [SerializeField] RoundManager roundManager;
    [SerializeField] GoldManager goldManager;
    [SerializeField] PlayerBallDeck playerBallDeck;

    [Header("UI")]
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text stageText;
    [SerializeField] TMP_Text waveText;
    [SerializeField] TMP_Text goldText;
    [SerializeField] TMP_Text ballCountText;
    [SerializeField] TMP_Text remainingLifeText;

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (titleText != null && stateManager != null)
        {
            titleText.text = stateManager.CurrentState == GameState.Clear
                ? "BOUNTY COMPLETE"
                : "RUN FAILED";
        }

        if (stageText != null && roundManager != null)
        {
            stageText.text = $"STAGE {roundManager.Progress.CurrentStageIndex + 1}";
        }

        if (waveText != null && roundManager != null)
        {
            waveText.text = $"WAVE {roundManager.Progress.CurrentWaveIndex + 1}";
        }

        if (goldText != null && goldManager != null)
        {
            goldText.text = $"GOLD {goldManager.CurrentGold}";
        }

        if (ballCountText != null && playerBallDeck != null)
        {
            ballCountText.text = $"BALLS {playerBallDeck.OwnedBalls.Count}";
        }

        if (remainingLifeText != null && roundManager != null)
        {
            remainingLifeText.text = $"LIFE {roundManager.LastBattleRemainingLife}";
        }
    }
}
