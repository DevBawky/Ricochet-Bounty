using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class BallRarityWeight
{
    public BallRarity rarity = BallRarity.Common;
    [Min(0f)] public float weight = 1f;
}

public class ShopManager : MonoBehaviour
{
    [Header("Shop Pool")]
    [SerializeField] List<BallDataSO> availableBalls = new List<BallDataSO>();

    [Header("Slots")]
    [SerializeField] List<ShopBallUI> shopSlots = new List<ShopBallUI>();

    [Header("Rarity Weights")]
    [SerializeField] List<BallRarityWeight> rarityWeights = new List<BallRarityWeight>
    {
        new BallRarityWeight { rarity = BallRarity.Common, weight = 60f },
        new BallRarityWeight { rarity = BallRarity.Rare, weight = 25f },
        new BallRarityWeight { rarity = BallRarity.Epic, weight = 10f },
        new BallRarityWeight { rarity = BallRarity.Legendary, weight = 5f }
    };

    [Header("References")]
    [SerializeField] GoldManager goldManager;
    [SerializeField] PlayerBallDeck playerBallDeck;
    [SerializeField] Button refreshButton;

    [Header("Refresh Cost")]
    [SerializeField, Min(0)] int firstRefreshCost = 1;
    [SerializeField, Min(0)] int secondRefreshCost = 1;
    [SerializeField] TMP_Text refreshCostText;

    int currentRefreshCost;
    int nextRefreshCost;

    void Awake()
    {
        FindMissingReferences();
        BindRefreshButton();
        ApplySlotDependencies();
        ResetRefreshCostForShopVisit();
    }

    void OnEnable()
    {
        ApplySlotDependencies();
    }

    public void GenerateShopItems()
    {
        FindMissingReferences();
        ApplySlotDependencies();

        if (shopSlots == null || shopSlots.Count <= 0)
        {
            Debug.LogWarning("[ShopManager] No ShopBallUI slots are connected.", this);
            return;
        }

        if (availableBalls == null || availableBalls.Count <= 0)
        {
            Debug.LogWarning("[ShopManager] No BallDataSO entries are registered in availableBalls.", this);
            ClearSlots();
            return;
        }

        for (int i = 0; i < shopSlots.Count; i++)
        {
            ShopBallUI slot = shopSlots[i];
            if (slot == null)
            {
                Debug.LogWarning($"[ShopManager] Shop slot {i} is not connected.", this);
                continue;
            }

            BallDataSO selectedBall = PickWeightedRandomBall();
            slot.SetBallData(selectedBall);

            if (selectedBall != null)
            {
                Debug.Log($"[ShopManager] Slot {i} selected: {selectedBall.name}, Rarity: {selectedBall.Rarity}, Price: {selectedBall.Price}", this);
            }
        }
    }

    public void RefreshShop()
    {
        FindMissingReferences();

        if (!CanGenerateShopItems())
        {
            return;
        }

        if (goldManager == null)
        {
            Debug.LogWarning("[ShopManager] GoldManager is missing. Cannot refresh shop.", this);
            return;
        }

        int paidCost = currentRefreshCost;
        if (!goldManager.TrySpendGold(paidCost))
        {
            Debug.Log($"[ShopManager] Refresh failed. Need: {paidCost}, Current Gold: {goldManager.CurrentGold}", this);
            return;
        }

        GenerateShopItems();
        AdvanceRefreshCost();
        Debug.Log($"[ShopManager] Shop refreshed. Paid: {paidCost}, Next Cost: {currentRefreshCost}", this);
    }

    public void OnShopEntered()
    {
        ResetRefreshCostForShopVisit();
        GenerateShopItems();
    }

    public void OnShopExited()
    {
        // Refresh Fibonacci state is reset on the next actual Shop entry.
    }

    public void RefreshCostUI()
    {
        if (refreshCostText != null)
        {
            refreshCostText.text = $"$ {currentRefreshCost}";
        }
    }

    public ShopSaveData CaptureSaveData()
    {
        ShopSaveData data = new ShopSaveData
        {
            currentRefreshCost = currentRefreshCost,
            nextRefreshCost = nextRefreshCost
        };

        if (shopSlots != null)
        {
            for (int i = 0; i < shopSlots.Count; i++)
            {
                ShopBallUI slot = shopSlots[i];
                data.slots.Add(new ShopSlotSaveData
                {
                    ballId = slot != null && slot.CurrentBallData != null ? slot.CurrentBallData.SaveId : string.Empty,
                    purchased = slot != null && slot.IsPurchased
                });
            }
        }

        return data;
    }

    public void RestoreSaveData(ShopSaveData data, IList<BallDataSO> resolvedSlotBalls)
    {
        if (data == null)
        {
            ResetRefreshCostForShopVisit();
            ClearSlots();
            return;
        }

        currentRefreshCost = Mathf.Max(0, data.currentRefreshCost);
        nextRefreshCost = Mathf.Max(0, data.nextRefreshCost);
        RefreshCostUI();
        ApplySlotDependencies();

        if (shopSlots == null)
        {
            return;
        }

        for (int i = 0; i < shopSlots.Count; i++)
        {
            if (shopSlots[i] == null)
            {
                continue;
            }

            BallDataSO ballData = resolvedSlotBalls != null && i < resolvedSlotBalls.Count
                ? resolvedSlotBalls[i]
                : null;
            bool purchased = data.slots != null && i < data.slots.Count && data.slots[i].purchased;
            shopSlots[i].RestoreState(ballData, purchased);
        }
    }

    public void ResetRuntimeState()
    {
        ResetRefreshCostForShopVisit();
        ClearSlots();
    }

    void ResetRefreshCostForShopVisit()
    {
        currentRefreshCost = Mathf.Max(0, firstRefreshCost);
        nextRefreshCost = Mathf.Max(0, secondRefreshCost);
        RefreshCostUI();
    }

    void AdvanceRefreshCost()
    {
        long followingCost = (long)currentRefreshCost + nextRefreshCost;
        currentRefreshCost = nextRefreshCost;
        nextRefreshCost = followingCost > int.MaxValue ? int.MaxValue : (int)followingCost;
        RefreshCostUI();
    }

    bool CanGenerateShopItems()
    {
        if (shopSlots == null || shopSlots.Count <= 0)
        {
            Debug.LogWarning("[ShopManager] No ShopBallUI slots are connected.", this);
            return false;
        }

        if (availableBalls == null || availableBalls.Count <= 0)
        {
            Debug.LogWarning("[ShopManager] No BallDataSO entries are registered in availableBalls.", this);
            return false;
        }

        bool hasValidSlot = false;
        for (int i = 0; i < shopSlots.Count; i++)
        {
            if (shopSlots[i] != null)
            {
                hasValidSlot = true;
                break;
            }
        }

        bool hasValidBall = false;
        for (int i = 0; i < availableBalls.Count; i++)
        {
            if (availableBalls[i] != null)
            {
                hasValidBall = true;
                break;
            }
        }

        if (!hasValidSlot || !hasValidBall)
        {
            Debug.LogWarning("[ShopManager] Shop refresh configuration has no valid slot or ball.", this);
            return false;
        }

        return true;
    }

    BallDataSO PickWeightedRandomBall()
    {
        float totalWeight = 0f;

        for (int i = 0; i < availableBalls.Count; i++)
        {
            BallDataSO ballData = availableBalls[i];
            if (ballData == null)
            {
                continue;
            }

            totalWeight += GetWeightForRarity(ballData.Rarity);
        }

        if (totalWeight <= 0f)
        {
            return PickFallbackRandomBall();
        }

        float roll = Random.Range(0f, totalWeight);
        float current = 0f;

        for (int i = 0; i < availableBalls.Count; i++)
        {
            BallDataSO ballData = availableBalls[i];
            if (ballData == null)
            {
                continue;
            }

            current += GetWeightForRarity(ballData.Rarity);
            if (roll <= current)
            {
                return ballData;
            }
        }

        return PickFallbackRandomBall();
    }

    BallDataSO PickFallbackRandomBall()
    {
        List<BallDataSO> validBalls = new List<BallDataSO>();
        for (int i = 0; i < availableBalls.Count; i++)
        {
            if (availableBalls[i] != null)
            {
                validBalls.Add(availableBalls[i]);
            }
        }

        if (validBalls.Count <= 0)
        {
            return null;
        }

        return validBalls[Random.Range(0, validBalls.Count)];
    }

    float GetWeightForRarity(BallRarity rarity)
    {
        if (rarityWeights == null)
        {
            return 0f;
        }

        for (int i = 0; i < rarityWeights.Count; i++)
        {
            BallRarityWeight rarityWeight = rarityWeights[i];
            if (rarityWeight == null)
            {
                continue;
            }

            if (rarityWeight.rarity == rarity)
            {
                return Mathf.Max(0f, rarityWeight.weight);
            }
        }

        return 0f;
    }

    void ClearSlots()
    {
        if (shopSlots == null)
        {
            return;
        }

        for (int i = 0; i < shopSlots.Count; i++)
        {
            if (shopSlots[i] != null)
            {
                shopSlots[i].SetBallData(null);
            }
        }
    }

    void ApplySlotDependencies()
    {
        if (shopSlots == null)
        {
            return;
        }

        for (int i = 0; i < shopSlots.Count; i++)
        {
            if (shopSlots[i] != null)
            {
                shopSlots[i].SetPurchaseDependencies(goldManager, playerBallDeck);
            }
        }
    }

    void BindRefreshButton()
    {
        if (refreshButton == null)
        {
            return;
        }

        refreshButton.onClick.RemoveListener(RefreshShop);
        refreshButton.onClick.AddListener(RefreshShop);
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

        if (refreshCostText == null && refreshButton != null)
        {
            refreshCostText = refreshButton.GetComponentInChildren<TMP_Text>(true);
        }
    }

    void OnValidate()
    {
        firstRefreshCost = Mathf.Max(0, firstRefreshCost);
        secondRefreshCost = Mathf.Max(0, secondRefreshCost);
    }
}
