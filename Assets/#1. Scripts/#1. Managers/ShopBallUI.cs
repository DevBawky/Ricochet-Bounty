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

    void Awake()
    {
        BindBuyButton();
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
    }

    public void SetBallData(BallDataSO data)
    {
        currentBallData = data;
        isPurchased = false;

        if (data == null)
        {
            ApplyEmptyState();
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
            buyButton.interactable = true;
        }
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

        int price = currentBallData.Price;
        if (!goldManager.TrySpendGold(price))
        {
            Debug.Log($"[ShopBallUI] Not enough money to buy {currentBallData.name}. Price: {price}, Current Gold: {goldManager.CurrentGold}", this);
            return;
        }

        playerBallDeck.AddBallToDeck(currentBallData);
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
}
