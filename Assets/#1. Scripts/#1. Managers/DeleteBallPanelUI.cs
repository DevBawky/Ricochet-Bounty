using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeleteBallPanelUI : MonoBehaviour
{
    const int BallsPerLayout = 5;

    [Header("UI")]
    [SerializeField] GameObject deleteBallPanel;
    [SerializeField] Button openButton;
    [SerializeField] Button closeButton;
    [SerializeField] TMP_Text deleteCostText;
    [SerializeField] List<Transform> ballLayouts = new List<Transform>();
    [SerializeField] DeleteBallItemUI ballItemPrefab;

    [Header("References")]
    [SerializeField] GoldManager goldManager;
    [SerializeField] PlayerBallDeck playerBallDeck;

    [Header("Delete")]
    [SerializeField, Min(0)] int deleteCost = 3;

    readonly List<DeleteBallItemUI> spawnedItems = new List<DeleteBallItemUI>();

    void Awake()
    {
        FindMissingReferences();
        BindButtons();
        RefreshCostText();
    }

    void OnEnable()
    {
        FindMissingReferences();
        BindButtons();
        SubscribeToDeck();
    }

    void OnDisable()
    {
        UnsubscribeFromDeck();
    }

    public void OpenPanel()
    {
        FindMissingReferences();
        if (deleteBallPanel == null)
        {
            Debug.LogWarning("[DeleteBallPanelUI] Delete Ball panel is not connected.", this);
            return;
        }

        deleteBallPanel.SetActive(true);
        RefreshBallList();
    }

    public void ClosePanel()
    {
        if (deleteBallPanel != null)
        {
            deleteBallPanel.SetActive(false);
        }
    }

    public void RefreshBallList()
    {
        ClearSpawnedItems();
        RefreshCostText();

        if (playerBallDeck == null || ballItemPrefab == null)
        {
            return;
        }

        IReadOnlyList<BallDataSO> ownedBalls = playerBallDeck.OwnedBalls;
        int displayCount = Mathf.Min(ownedBalls.Count, PlayerBallDeck.MaxOwnedBallCount);
        bool canDelete = playerBallDeck.CanDeleteOwnedBall;
        for (int i = 0; i < displayCount; i++)
        {
            int layoutIndex = i / BallsPerLayout;
            if (layoutIndex >= ballLayouts.Count || ballLayouts[layoutIndex] == null)
            {
                Debug.LogWarning($"[DeleteBallPanelUI] Ball Layout {layoutIndex + 1} is not connected.", this);
                break;
            }

            DeleteBallItemUI item = Instantiate(ballItemPrefab, ballLayouts[layoutIndex]);
            item.name = $"Button _ MyBall ({i + 1})";
            item.SetBallData(ownedBalls[i], i, deleteCost, canDelete, this);
            spawnedItems.Add(item);
        }
    }

    public void TryDeleteBall(int ownedBallIndex, BallDataSO expectedBallData)
    {
        FindMissingReferences();
        if (goldManager == null || playerBallDeck == null)
        {
            return;
        }

        if (!playerBallDeck.IsOwnedBallAt(ownedBallIndex, expectedBallData))
        {
            RefreshBallList();
            return;
        }

        if (!playerBallDeck.CanDeleteOwnedBall)
        {
            return;
        }

        if (!goldManager.CanAfford(deleteCost))
        {
            return;
        }

        if (!playerBallDeck.TryRemoveOwnedBallAt(ownedBallIndex, expectedBallData))
        {
            return;
        }

        if (!goldManager.TrySpendGold(deleteCost))
        {
            Debug.LogError("[DeleteBallPanelUI] Gold changed during deletion. Restoring the removed ball is required.", this);
            playerBallDeck.TryInsertOwnedBall(ownedBallIndex, expectedBallData);
            return;
        }
    }

    void RefreshCostText()
    {
        if (deleteCostText != null)
        {
            deleteCostText.text = $"Cost : ${Mathf.Max(0, deleteCost)}";
        }
    }

    void ClearSpawnedItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
            {
                spawnedItems[i].gameObject.SetActive(false);
                Destroy(spawnedItems[i].gameObject);
            }
        }

        spawnedItems.Clear();
    }

    void BindButtons()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenPanel);
            openButton.onClick.AddListener(OpenPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    void SubscribeToDeck()
    {
        if (playerBallDeck == null)
        {
            return;
        }

        playerBallDeck.OnDeckChanged.RemoveListener(OnDeckChanged);
        playerBallDeck.OnDeckChanged.AddListener(OnDeckChanged);
    }

    void UnsubscribeFromDeck()
    {
        if (playerBallDeck != null)
        {
            playerBallDeck.OnDeckChanged.RemoveListener(OnDeckChanged);
        }
    }

    void OnDeckChanged()
    {
        if (deleteBallPanel != null && deleteBallPanel.activeInHierarchy)
        {
            RefreshBallList();
        }
    }

    void FindMissingReferences()
    {
        if (goldManager == null)
        {
            goldManager = FindFirstObjectByType<GoldManager>();
        }

        if (playerBallDeck == null)
        {
            playerBallDeck = FindFirstObjectByType<PlayerBallDeck>();
        }
    }
}
