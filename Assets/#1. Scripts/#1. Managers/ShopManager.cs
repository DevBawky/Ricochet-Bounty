using System.Collections.Generic;
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

    [Header("Refresh")]
    [SerializeField, Min(0)] int refreshCost;

    void Awake()
    {
        FindMissingReferences();
        BindRefreshButton();
        ApplySlotDependencies();
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

        if (refreshCost > 0)
        {
            if (goldManager == null)
            {
                Debug.LogWarning("[ShopManager] Cannot refresh with a cost because GoldManager is missing.", this);
                return;
            }

            if (!goldManager.TrySpendGold(refreshCost))
            {
                Debug.Log($"[ShopManager] Refresh failed. Need: {refreshCost}, Current Gold: {goldManager.CurrentGold}", this);
                return;
            }
        }

        Debug.Log($"[ShopManager] Refresh shop. Cost: {refreshCost}", this);
        GenerateShopItems();
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
    }
}
