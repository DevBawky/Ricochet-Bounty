using UnityEngine;

[RequireComponent(typeof(BallDataManager))]
public class BallRuntimeStatus : MonoBehaviour
{
    [Header("Runtime State")]
    [SerializeField] float currentDurability;
    [SerializeField] bool isDestroying;

    BallDataManager ballDataManager;
    PlayerUpgradeManager upgradeManager;
    bool isInitialized;

    BallDataSO BallData
    {
        get
        {
            if (ballDataManager == null)
            {
                ballDataManager = GetComponent<BallDataManager>();
            }

            return ballDataManager != null ? ballDataManager.BallData : null;
        }
    }

    public int CurrentDurability => Mathf.CeilToInt(Mathf.Max(0f, currentDurability));

    public int MaxDurability
    {
        get
        {
            BallDataSO data = BallData;
            if (data == null)
            {
                return 0;
            }

            FindUpgradeManager();
            return upgradeManager != null
                ? upgradeManager.GetUpgradedMaxDurability(data.MaxDurability)
                : Mathf.Max(1, data.MaxDurability);
        }
    }

    public bool IsDestroying => isDestroying;

    void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (ballDataManager == null)
        {
            ballDataManager = GetComponent<BallDataManager>();
        }

        if (ballDataManager == null || !ballDataManager.ValidateData())
        {
            return;
        }

        FindUpgradeManager();
        currentDurability = MaxDurability;
        isDestroying = false;
        isInitialized = true;
    }

    public void ApplyWallHitDurabilityDamage()
    {
        if (EnsureInitialized())
        {
            TakeCollisionDurabilityDamage(BallData.WallHitDurabilityDamage);
        }
    }

    public void ApplyObjectHitDurabilityDamage()
    {
        if (EnsureInitialized())
        {
            TakeCollisionDurabilityDamage(BallData.ObjectHitDurabilityDamage);
        }
    }

    public void TakeDurabilityDamage(int amount)
    {
        TakeDurabilityDamage((float)amount);
    }

    public void HealDurability(int amount)
    {
        if (!EnsureInitialized() || amount <= 0 || isDestroying)
        {
            return;
        }

        currentDurability = Mathf.Min(currentDurability + amount, MaxDurability);
        Debug.Log($"[BallRuntimeStatus] {name} durability healed: +{amount} / {currentDurability:0.##}/{MaxDurability}", this);
    }

    public bool IsDead()
    {
        return currentDurability <= 0f;
    }

    public void DestroyBall()
    {
        if (isDestroying)
        {
            return;
        }

        isDestroying = true;
        BallEffectController effectController = GetComponent<BallEffectController>();
        if (effectController != null)
        {
            effectController.TriggerDestroyEffects();
        }

        Destroy(gameObject);
    }

    void TakeCollisionDurabilityDamage(float baseDamage)
    {
        FindUpgradeManager();
        float appliedDamage = upgradeManager != null
            ? upgradeManager.GetReducedCollisionDamage(baseDamage)
            : Mathf.Max(0f, baseDamage);
        TakeDurabilityDamage(appliedDamage);
    }

    void TakeDurabilityDamage(float amount)
    {
        if (amount <= 0f || isDestroying)
        {
            return;
        }

        currentDurability -= amount;
        Debug.Log($"[BallRuntimeStatus] {name} durability damage: -{amount:0.##} / {currentDurability:0.##}/{MaxDurability}", this);
        if (IsDead())
        {
            DestroyBall();
        }
    }

    bool EnsureInitialized()
    {
        if (!isInitialized)
        {
            Initialize();
        }

        return isInitialized && BallData != null;
    }

    void FindUpgradeManager()
    {
        if (upgradeManager == null)
        {
            upgradeManager = PlayerUpgradeManager.Instance;
        }

        if (upgradeManager == null)
        {
            upgradeManager = FindFirstObjectByType<PlayerUpgradeManager>(FindObjectsInactive.Include);
        }
    }
}
