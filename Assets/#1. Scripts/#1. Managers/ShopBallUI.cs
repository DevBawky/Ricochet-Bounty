using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopBallUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Image ballImage;
    [SerializeField] Button buyButton;
    [SerializeField] TMP_Text priceText;

    [Header("References")]
    [SerializeField] GoldManager goldManager;
    [SerializeField] PlayerBallDeck playerBallDeck;

    BallDataSO currentBallData;
    bool isPurchased;
    ShopBallTooltipTrigger tooltipTrigger;

    public BallDataSO CurrentBallData => currentBallData;
    public bool IsPurchased => isPurchased;

    void Awake()
    {
        BindBuyButton();
        BindTooltipTrigger();
    }

    void OnEnable()
    {
        SubscribeToDeck();
        RefreshBuyButton();
    }

    void OnDisable()
    {
        UnsubscribeFromDeck();
        tooltipTrigger?.HideTooltip();
    }

    public void SetPurchaseDependencies(GoldManager newGoldManager, PlayerBallDeck newPlayerBallDeck)
    {
        if (goldManager == null)
        {
            goldManager = newGoldManager;
        }

        if (playerBallDeck == null)
        {
            playerBallDeck = newPlayerBallDeck;
        }

        SubscribeToDeck();
        RefreshBuyButton();
    }

    public void SetBallData(BallDataSO data)
    {
        currentBallData = data;
        isPurchased = false;

        if (data == null)
        {
            ApplyEmptyState();
            tooltipTrigger?.RefreshTooltip();
            return;
        }

        if (ballImage != null)
        {
            ballImage.enabled = true;
            ballImage.sprite = data.BallSprite;
            ballImage.color = data.BallColor;
        }

        if (priceText != null)
        {
            priceText.text = $"${data.Price}";
        }

        if (buyButton != null)
        {
            buyButton.interactable = !playerBallDeck?.IsAtCapacity ?? true;
        }

        tooltipTrigger?.RefreshTooltip();
    }

    public void RestoreState(BallDataSO data, bool purchased)
    {
        SetBallData(data);
        isPurchased = purchased;
        RefreshBuyButton();
    }

    public void BuyCurrentBall()
    {
        if (currentBallData == null)
        {
            Debug.LogWarning("[ShopBallUI] Buy ignored because currentBallData is null.", this);
            return;
        }

        if (isPurchased)
        {
            Debug.Log($"[ShopBallUI] {currentBallData.name} is already purchased.", this);
            return;
        }

        if (goldManager == null)
        {
            Debug.LogWarning("[ShopBallUI] GoldManager is missing. Cannot buy ball.", this);
            return;
        }

        if (playerBallDeck == null)
        {
            Debug.LogWarning("[ShopBallUI] PlayerBallDeck is missing. Cannot add purchased ball.", this);
            return;
        }

        if (playerBallDeck.IsAtCapacity)
        {
            Debug.Log($"[ShopBallUI] Purchase blocked. Owned ball limit reached: {PlayerBallDeck.MaxOwnedBallCount}.", this);
            return;
        }

        int price = currentBallData.Price;
        if (!goldManager.TrySpendGold(price))
        {
            Debug.Log($"[ShopBallUI] Not enough money to buy {currentBallData.name}. Price: {price}, Current Gold: {goldManager.CurrentGold}", this);
            return;
        }

        if (!playerBallDeck.TryAddBallToDeck(currentBallData))
        {
            goldManager.AddGold(price);
            Debug.LogWarning("[ShopBallUI] Deck rejected the ball after payment. The purchase price was refunded.", this);
            return;
        }
        isPurchased = true;

        if (buyButton != null)
        {
            buyButton.interactable = false;
        }

        Debug.Log($"[ShopBallUI] Purchased {currentBallData.name}. Rarity: {currentBallData.Rarity}, Price: {price}", this);
    }

    void ApplyEmptyState()
    {
        if (ballImage != null)
        {
            ballImage.sprite = null;
            ballImage.color = Color.clear;
            ballImage.enabled = false;
        }

        if (priceText != null)
        {
            priceText.text = "$0";
        }

        if (buyButton != null)
        {
            buyButton.interactable = false;
        }
    }

    void BindBuyButton()
    {
        if (buyButton == null)
        {
            return;
        }

        buyButton.onClick.RemoveListener(BuyCurrentBall);
        buyButton.onClick.AddListener(BuyCurrentBall);
    }

    void BindTooltipTrigger()
    {
        if (ballImage == null)
        {
            return;
        }

        tooltipTrigger = ballImage.GetComponent<ShopBallTooltipTrigger>();
        if (tooltipTrigger == null)
        {
            tooltipTrigger = ballImage.gameObject.AddComponent<ShopBallTooltipTrigger>();
        }

        tooltipTrigger.Initialize(this);
    }

    void SubscribeToDeck()
    {
        if (playerBallDeck == null)
        {
            return;
        }

        playerBallDeck.OnDeckChanged.RemoveListener(RefreshBuyButton);
        playerBallDeck.OnDeckChanged.AddListener(RefreshBuyButton);
    }

    void UnsubscribeFromDeck()
    {
        if (playerBallDeck != null)
        {
            playerBallDeck.OnDeckChanged.RemoveListener(RefreshBuyButton);
        }
    }

    void RefreshBuyButton()
    {
        if (buyButton != null)
        {
            buyButton.interactable = currentBallData != null && !isPurchased &&
                (playerBallDeck == null || !playerBallDeck.IsAtCapacity);
        }
    }
}
