using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeShopUI : MonoBehaviour
{
    [SerializeField] PlayerUpgradeType upgradeType;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text valueText;
    [SerializeField] TMP_Text levelText;
    [SerializeField] TMP_Text costText;
    [SerializeField] Button purchaseButton;
    [SerializeField] TMP_Text purchaseButtonText;

    PlayerUpgradeManager upgradeManager;
    GoldManager goldManager;

    void Awake()
    {
        FindMissingReferences();
        BindPurchaseButton();
    }

    void OnEnable()
    {
        FindMissingReferences();
        BindPurchaseButton();

        if (upgradeManager != null)
        {
            upgradeManager.OnUpgradesChanged.RemoveListener(Refresh);
            upgradeManager.OnUpgradesChanged.AddListener(Refresh);
        }

        Refresh();
    }

    void OnDisable()
    {
        if (upgradeManager != null)
        {
            upgradeManager.OnUpgradesChanged.RemoveListener(Refresh);
        }
    }

    public void PurchaseUpgrade()
    {
        FindMissingReferences();
        if (upgradeManager == null)
        {
            Debug.LogWarning("[UpgradeShopUI] PlayerUpgradeManager is missing.", this);
            return;
        }

        upgradeManager.TryPurchaseUpgrade(upgradeType, goldManager);
        Refresh();
    }

    public void Refresh()
    {
        FindMissingReferences();
        int level = upgradeManager != null ? upgradeManager.GetLevel(upgradeType) : 0;
        int currentPercent = level * PlayerUpgradeManager.PercentPerLevel;

        if (valueText != null)
        {
            valueText.text = level < PlayerUpgradeManager.MaxUpgradeLevel
                ? $"{currentPercent}% -> {currentPercent + PlayerUpgradeManager.PercentPerLevel}%"
                : $"{currentPercent}%";
        }

        if (levelText != null)
        {
            levelText.text = $"LV. {level}";
        }

        bool canUpgrade = upgradeManager != null && level < PlayerUpgradeManager.MaxUpgradeLevel;
        if (costText != null)
        {
            costText.text = canUpgrade ? $"$ {upgradeManager.GetUpgradeCost(upgradeType)}" : "MAX";
        }

        if (purchaseButton != null)
        {
            purchaseButton.interactable = canUpgrade;
        }

        if (purchaseButtonText != null && !canUpgrade)
        {
            purchaseButtonText.text = "MAX";
        }
    }

    void BindPurchaseButton()
    {
        if (purchaseButton == null)
        {
            return;
        }

        purchaseButton.onClick.RemoveListener(PurchaseUpgrade);
        purchaseButton.onClick.AddListener(PurchaseUpgrade);
    }

    void FindMissingReferences()
    {
        if (upgradeManager == null)
        {
            upgradeManager = PlayerUpgradeManager.Instance;
        }

        if (upgradeManager == null)
        {
            upgradeManager = FindFirstObjectByType<PlayerUpgradeManager>(FindObjectsInactive.Include);
        }

        if (goldManager == null)
        {
            goldManager = FindFirstObjectByType<GoldManager>();
        }
    }
}
