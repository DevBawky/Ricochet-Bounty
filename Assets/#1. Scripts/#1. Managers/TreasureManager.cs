using UnityEngine;
using UnityEngine.UI;

public class TreasureManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Button treasureButton;
    [SerializeField] GameObject treasureResultPanel;
    [SerializeField] Image resultImage;
    [SerializeField] Button deleteButton;
    [SerializeField] Button receiveButton;

    [Header("References")]
    [SerializeField] PlayerBallDeck playerBallDeck;
    [SerializeField] RoundManager roundManager;

    [Header("Treasure Pool")]
    [SerializeField] BallDataSO[] treasurePool;

    BallDataSO resultBallData;
    bool treasureOpened;
    bool rewardReceived;
    bool completionRequested;

    void Awake()
    {
        FindMissingReferences();
        BindButtons();
    }

    void OnDestroy()
    {
        if (treasureButton != null) treasureButton.onClick.RemoveListener(OnTreasureClicked);
        if (deleteButton != null) deleteButton.onClick.RemoveListener(OnDeleteClicked);
        if (receiveButton != null) receiveButton.onClick.RemoveListener(OnReceiveClicked);
    }

    public void BeginTreasure()
    {
        FindMissingReferences();
        BindButtons();
        resultBallData = null;
        treasureOpened = false;
        rewardReceived = false;
        completionRequested = false;

        if (resultImage != null)
        {
            resultImage.sprite = null;
            resultImage.enabled = false;
        }

        if (treasureResultPanel != null)
        {
            treasureResultPanel.SetActive(false);
        }

        if (treasureButton != null)
        {
            treasureButton.gameObject.SetActive(true);
            treasureButton.interactable = true;
        }
    }

    void OnTreasureClicked()
    {
        if (treasureOpened)
        {
            return;
        }

        resultBallData = PickRandomTreasure();
        if (resultBallData == null)
        {
            return;
        }

        treasureOpened = true;

        if (resultImage != null)
        {
            resultImage.sprite = resultBallData.BallSprite;
            resultImage.enabled = resultBallData.BallSprite != null;
        }

        if (treasureResultPanel != null)
        {
            treasureResultPanel.SetActive(true);
        }

        if (treasureButton != null)
        {
            treasureButton.interactable = false;
            treasureButton.gameObject.SetActive(false);
        }

        RefreshReceiveButton();
    }

    void OnReceiveClicked()
    {
        if (!treasureOpened || rewardReceived || resultBallData == null || completionRequested || playerBallDeck == null)
        {
            return;
        }

        if (!playerBallDeck.TryAddBallToDeck(resultBallData))
        {
            RefreshReceiveButton();
            return;
        }

        rewardReceived = true;
        RefreshReceiveButton();
        CompleteTreasure();
    }

    void OnDeleteClicked()
    {
        if (!treasureOpened || completionRequested)
        {
            return;
        }

        CompleteTreasure();
    }

    void CompleteTreasure()
    {
        completionRequested = true;
        if (roundManager == null || !roundManager.CompleteNonBattleWave(WaveType.Treasure))
        {
            completionRequested = false;
        }
    }

    void RefreshReceiveButton()
    {
        if (receiveButton != null)
        {
            receiveButton.interactable = resultBallData != null && !rewardReceived && playerBallDeck != null && !playerBallDeck.IsAtCapacity;
        }

        if (deleteButton != null)
        {
            deleteButton.interactable = treasureOpened;
        }
    }

    BallDataSO PickRandomTreasure()
    {
        if (treasurePool == null || treasurePool.Length <= 0)
        {
            Debug.LogWarning("[TreasureManager] Treasure Pool is empty.", this);
            return null;
        }

        int startIndex = Random.Range(0, treasurePool.Length);
        for (int i = 0; i < treasurePool.Length; i++)
        {
            BallDataSO ballData = treasurePool[(startIndex + i) % treasurePool.Length];
            if (ballData != null)
            {
                return ballData;
            }
        }

        Debug.LogWarning("[TreasureManager] Treasure Pool has no valid BallDataSO.", this);
        return null;
    }

    void BindButtons()
    {
        if (treasureButton != null)
        {
            treasureButton.onClick.RemoveListener(OnTreasureClicked);
            treasureButton.onClick.AddListener(OnTreasureClicked);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveListener(OnDeleteClicked);
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }

        if (receiveButton != null)
        {
            receiveButton.onClick.RemoveListener(OnReceiveClicked);
            receiveButton.onClick.AddListener(OnReceiveClicked);
        }
    }

    void FindMissingReferences()
    {
        if (playerBallDeck == null) playerBallDeck = FindFirstObjectByType<PlayerBallDeck>(FindObjectsInactive.Include);
        if (roundManager == null) roundManager = FindFirstObjectByType<RoundManager>(FindObjectsInactive.Include);
    }
}
