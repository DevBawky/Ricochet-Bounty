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
    UpgradeShopTooltipTrigger tooltipTrigger;

    public string TooltipTitle
    {
        get
        {
            switch (upgradeType)
            {
                case PlayerUpgradeType.BallHp:
                    return "내구도";
                case PlayerUpgradeType.BallDefense:
                    return "충돌 피해 감소";
                case PlayerUpgradeType.ScoreBoost:
                    return "점수 증폭";
                default:
                    return "강화";
            }
        }
    }

    public string TooltipDescription => BuildTooltipDescription();

    void Awake()
    {
        FindMissingReferences();
        BindPurchaseButton();
        BindTooltipTrigger();
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

        tooltipTrigger?.HideTooltip();
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
                ? $"{currentPercent}% → {currentPercent + PlayerUpgradeManager.PercentPerLevel}%"
                : $"{currentPercent}%";
        }

        if (levelText != null)
        {
            levelText.text = level.ToString();
        }

        bool isBelowMaxLevel = upgradeManager != null && level < PlayerUpgradeManager.MaxUpgradeLevel;
        int upgradeCost = 0;
        bool hasConfiguredCost = isBelowMaxLevel && upgradeManager.TryGetUpgradeCost(upgradeType, out upgradeCost);
        bool canUpgrade = isBelowMaxLevel && hasConfiguredCost;
        if (costText != null)
        {
            costText.text = level >= PlayerUpgradeManager.MaxUpgradeLevel
                ? "최대"
                : hasConfiguredCost ? $"$ {upgradeCost}" : "없음";
        }

        if (purchaseButton != null)
        {
            purchaseButton.interactable = canUpgrade;
        }

        if (purchaseButtonText != null && !canUpgrade)
        {
            purchaseButtonText.text = "최대";
        }

        tooltipTrigger?.RefreshTooltip();
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

    void BindTooltipTrigger()
    {
        tooltipTrigger = GetComponent<UpgradeShopTooltipTrigger>();
        if (tooltipTrigger == null)
        {
            tooltipTrigger = gameObject.AddComponent<UpgradeShopTooltipTrigger>();
        }

        tooltipTrigger.Initialize(this);
    }

    string BuildTooltipDescription()
    {
        FindMissingReferences();
        int level = upgradeManager != null ? upgradeManager.GetLevel(upgradeType) : 0;
        int currentPercent = level * PlayerUpgradeManager.PercentPerLevel;
        int nextPercent = Mathf.Min(
            PlayerUpgradeManager.MaxUpgradeLevel * PlayerUpgradeManager.PercentPerLevel,
            currentPercent + PlayerUpgradeManager.PercentPerLevel);
        bool isMaxLevel = level >= PlayerUpgradeManager.MaxUpgradeLevel;

        switch (upgradeType)
        {
            case PlayerUpgradeType.BallHp:
                return BuildProgressDescription(
                    "모든 공의 최대 내구도가 증가합니다.",
                    $"현재 보너스: +{currentPercent}%",
                    $"다음 레벨: +{nextPercent}%",
                    isMaxLevel);

            case PlayerUpgradeType.BallDefense:
                float currentDamageTaken = 100f / (1f + currentPercent / 100f);
                float nextDamageTaken = 100f / (1f + nextPercent / 100f);
                return BuildProgressDescription(
                    "벽과 오브젝트 충돌로 받는 내구도 피해가 감소합니다.",
                    $"현재 받는 피해: {currentDamageTaken:0.#}%",
                    $"다음 레벨: {nextDamageTaken:0.#}%",
                    isMaxLevel);

            case PlayerUpgradeType.ScoreBoost:
                return BuildProgressDescription(
                    "칩 또는 배수를 얻을 때마다 두 배로 획득할 확률이 생깁니다.",
                    $"현재 확률: {currentPercent}%",
                    $"다음 레벨: {nextPercent}%",
                    isMaxLevel);

            default:
                return string.Empty;
        }
    }

    static string BuildProgressDescription(
        string effectDescription,
        string currentValue,
        string nextValue,
        bool isMaxLevel)
    {
        return isMaxLevel
            ? $"{effectDescription}\n{currentValue} (최대)"
            : $"{effectDescription}\n{currentValue}\n{nextValue}";
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
