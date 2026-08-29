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

    [Header("Upgrade Costs by Current Level")]
    [SerializeField] int[] ballHpUpgradeCosts = new int[MaxUpgradeLevel];
    [SerializeField] int[] ballDefenseUpgradeCosts = new int[MaxUpgradeLevel];
    [SerializeField] int[] scoreBoostUpgradeCosts = new int[MaxUpgradeLevel];

    [Header("Delete Ball Cost")]
    [SerializeField, Min(0)] int initialDeleteCost = 3;
    [SerializeField, Min(0)] int deleteCostIncrease = 1;

    int currentDeleteCost;

    [Header("Player Panel - Upgrades Info")]
    [SerializeField] TMP_Text ballHpLevelText;
    [SerializeField] TMP_Text ballDefenseLevelText;
    [SerializeField] TMP_Text scoreBoostLevelText;

    [Header("Events")]
    [SerializeField] UnityEvent onUpgradesChanged = new UnityEvent();

    public UnityEvent OnUpgradesChanged => onUpgradesChanged;
    public int CurrentDeleteCost => currentDeleteCost;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PlayerUpgradeManager] More than one manager exists. The first instance will be used.", this);
            return;
        }

        Instance = this;
        ClampLevels();
        ResetDeleteCost();
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

    public bool TryGetUpgradeCost(PlayerUpgradeType upgradeType, out int cost)
    {
        cost = 0;
        int currentLevel = GetLevel(upgradeType);
        if (currentLevel >= MaxUpgradeLevel)
        {
            return false;
        }

        int[] costs = GetUpgradeCosts(upgradeType);
        if (costs == null || currentLevel < 0 || currentLevel >= costs.Length)
        {
            int length = costs != null ? costs.Length : 0;
            Debug.LogWarning($"[PlayerUpgradeManager] {upgradeType} cost for level {currentLevel} is missing. Array Length: {length}", this);
            return false;
        }

        cost = Mathf.Max(0, costs[currentLevel]);
        return true;
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

        if (!TryGetUpgradeCost(upgradeType, out int cost))
        {
            return false;
        }

        if (!goldManager.TrySpendGold(cost))
        {
            return false;
        }

        SetLevel(upgradeType, currentLevel + 1);
        RefreshPlayerPanel();
        OnUpgradesChanged.Invoke();
        return true;
    }

    public void AdvanceDeleteCost()
    {
        long nextCost = (long)currentDeleteCost + Mathf.Max(0, deleteCostIncrease);
        currentDeleteCost = nextCost > int.MaxValue ? int.MaxValue : (int)nextCost;
    }

    public void ResetRunData()
    {
        ballHpLevel = 0;
        ballDefenseLevel = 0;
        scoreBoostLevel = 0;
        ResetDeleteCost();
        RefreshPlayerPanel();
        OnUpgradesChanged.Invoke();
    }

    public PlayerUpgradeSaveData CaptureSaveData()
    {
        return new PlayerUpgradeSaveData
        {
            ballHpLevel = ballHpLevel,
            ballDefenseLevel = ballDefenseLevel,
            scoreBoostLevel = scoreBoostLevel,
            currentDeleteCost = currentDeleteCost
        };
    }

    public void RestoreSaveData(PlayerUpgradeSaveData data)
    {
        if (data == null)
        {
            ResetRunData();
            return;
        }

        ballHpLevel = Mathf.Clamp(data.ballHpLevel, 0, MaxUpgradeLevel);
        ballDefenseLevel = Mathf.Clamp(data.ballDefenseLevel, 0, MaxUpgradeLevel);
        scoreBoostLevel = Mathf.Clamp(data.scoreBoostLevel, 0, MaxUpgradeLevel);
        currentDeleteCost = Mathf.Max(0, data.currentDeleteCost);
        RefreshPlayerPanel();
        OnUpgradesChanged.Invoke();
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

    void ResetDeleteCost()
    {
        currentDeleteCost = Mathf.Max(0, initialDeleteCost);
    }

    int[] GetUpgradeCosts(PlayerUpgradeType upgradeType)
    {
        switch (upgradeType)
        {
            case PlayerUpgradeType.BallHp:
                return ballHpUpgradeCosts;
            case PlayerUpgradeType.BallDefense:
                return ballDefenseUpgradeCosts;
            case PlayerUpgradeType.ScoreBoost:
                return scoreBoostUpgradeCosts;
            default:
                return null;
        }
    }

    void OnValidate()
    {
        ClampLevels();
        initialDeleteCost = Mathf.Max(0, initialDeleteCost);
        deleteCostIncrease = Mathf.Max(0, deleteCostIncrease);
        ClampCosts(ballHpUpgradeCosts);
        ClampCosts(ballDefenseUpgradeCosts);
        ClampCosts(scoreBoostUpgradeCosts);
    }

    static void ClampCosts(int[] costs)
    {
        if (costs == null)
        {
            return;
        }

        for (int i = 0; i < costs.Length; i++)
        {
            costs[i] = Mathf.Max(0, costs[i]);
        }
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
            targetText.text = level.ToString();
        }
    }
}
