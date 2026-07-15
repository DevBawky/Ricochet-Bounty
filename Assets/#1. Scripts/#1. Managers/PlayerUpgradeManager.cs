using TMPro;
using UnityEngine;
using UnityEngine.Events;

public enum PlayerUpgradeType
{
    BallHp,
    BallDefense,
    ScoreBoost
}

public class PlayerUpgradeManager : MonoBehaviour
{
    public const int MaxUpgradeLevel = 10;
    public const int PercentPerLevel = 10;

    public static PlayerUpgradeManager Instance { get; private set; }

    [Header("Upgrade Levels")]
    [SerializeField, Range(0, MaxUpgradeLevel)] int ballHpLevel;
    [SerializeField, Range(0, MaxUpgradeLevel)] int ballDefenseLevel;
    [SerializeField, Range(0, MaxUpgradeLevel)] int scoreBoostLevel;

    [Header("Upgrade Cost")]
    [SerializeField, Min(0)] int baseUpgradeCost = 5;
    [SerializeField, Min(0)] int upgradeCostPerLevel;

    [Header("Player Panel - Upgrades Info")]
    [SerializeField] TMP_Text ballHpLevelText;
    [SerializeField] TMP_Text ballDefenseLevelText;
    [SerializeField] TMP_Text scoreBoostLevelText;

    [Header("Events")]
    [SerializeField] UnityEvent onUpgradesChanged = new UnityEvent();

    public UnityEvent OnUpgradesChanged => onUpgradesChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PlayerUpgradeManager] More than one manager exists. The first instance will be used.", this);
            return;
        }

        Instance = this;
        ClampLevels();
        FindMissingLevelTexts();
        RefreshPlayerPanel();
    }

    void OnEnable()
    {
        FindMissingLevelTexts();
        RefreshPlayerPanel();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public int GetLevel(PlayerUpgradeType upgradeType)
    {
        switch (upgradeType)
        {
            case PlayerUpgradeType.BallHp:
                return ballHpLevel;
            case PlayerUpgradeType.BallDefense:
                return ballDefenseLevel;
            case PlayerUpgradeType.ScoreBoost:
                return scoreBoostLevel;
            default:
                return 0;
        }
    }

    public int GetPercent(PlayerUpgradeType upgradeType)
    {
        return GetLevel(upgradeType) * PercentPerLevel;
    }

    public int GetUpgradeCost(PlayerUpgradeType upgradeType)
    {
        return baseUpgradeCost + GetLevel(upgradeType) * upgradeCostPerLevel;
    }

    public bool TryPurchaseUpgrade(PlayerUpgradeType upgradeType, GoldManager goldManager)
    {
        int currentLevel = GetLevel(upgradeType);
        if (currentLevel >= MaxUpgradeLevel)
        {
            return false;
        }

        if (goldManager == null)
        {
            Debug.LogWarning("[PlayerUpgradeManager] GoldManager is missing. Cannot purchase an upgrade.", this);
            return false;
        }

        int cost = GetUpgradeCost(upgradeType);
        if (!goldManager.TrySpendGold(cost))
        {
            return false;
        }

        SetLevel(upgradeType, currentLevel + 1);
        RefreshPlayerPanel();
        OnUpgradesChanged.Invoke();
        return true;
    }

    public int GetUpgradedMaxDurability(int baseDurability)
    {
        float ratio = GetPercent(PlayerUpgradeType.BallHp) / 100f;
        return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, baseDurability) * (1f + ratio)));
    }

    public float GetReducedCollisionDamage(float baseDamage)
    {
        float defenseRatio = GetPercent(PlayerUpgradeType.BallDefense) / 100f;
        return Mathf.Max(0f, baseDamage) / (1f + defenseRatio);
    }

    public bool ShouldDoubleScore()
    {
        float chance = GetPercent(PlayerUpgradeType.ScoreBoost) / 100f;
        return chance > 0f && Random.value < chance;
    }

    public void RefreshPlayerPanel()
    {
        SetLevelText(ballHpLevelText, ballHpLevel);
        SetLevelText(ballDefenseLevelText, ballDefenseLevel);
        SetLevelText(scoreBoostLevelText, scoreBoostLevel);
    }

    void SetLevel(PlayerUpgradeType upgradeType, int level)
    {
        level = Mathf.Clamp(level, 0, MaxUpgradeLevel);
        switch (upgradeType)
        {
            case PlayerUpgradeType.BallHp:
                ballHpLevel = level;
                break;
            case PlayerUpgradeType.BallDefense:
                ballDefenseLevel = level;
                break;
            case PlayerUpgradeType.ScoreBoost:
                scoreBoostLevel = level;
                break;
        }
    }

    void ClampLevels()
    {
        ballHpLevel = Mathf.Clamp(ballHpLevel, 0, MaxUpgradeLevel);
        ballDefenseLevel = Mathf.Clamp(ballDefenseLevel, 0, MaxUpgradeLevel);
        scoreBoostLevel = Mathf.Clamp(scoreBoostLevel, 0, MaxUpgradeLevel);
    }

    void FindMissingLevelTexts()
    {
        FindLevelText(ref ballHpLevelText, "Panel | BALL HP");
        FindLevelText(ref ballDefenseLevelText, "Panel | BALL DEFENSE");
        FindLevelText(ref scoreBoostLevelText, "Panel | SCORE BOOST");
    }

    void FindLevelText(ref TMP_Text targetText, string panelName)
    {
        if (targetText != null)
        {
            return;
        }

        Transform panel = FindChild(transform, panelName);
        Transform levelTextTransform = panel != null ? FindChild(panel, "Text | Upgrade Level") : null;
        if (levelTextTransform != null)
        {
            targetText = levelTextTransform.GetComponent<TMP_Text>();
        }
    }

    static Transform FindChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName || children[i].name.StartsWith(childName + " ("))
            {
                return children[i];
            }
        }

        return null;
    }

    static void SetLevelText(TMP_Text targetText, int level)
    {
        if (targetText != null)
        {
            targetText.text = $"LV. {level}";
        }
    }
}
