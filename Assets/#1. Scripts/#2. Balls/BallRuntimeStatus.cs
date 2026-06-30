using UnityEngine;

// BallRuntimeStatus는 공마다 달라지는 현재 내구도와 파괴 상태를 관리합니다.
// BallDataSO 참조는 직접 들고 있지 않고, 항상 같은 오브젝트의 BallDataManager에서 읽습니다.
// 이렇게 하면 공 프리팹에서 BallDataSO는 BallDataManager 한 곳에만 꽂으면 됩니다.
[RequireComponent(typeof(BallDataManager))]
public class BallRuntimeStatus : MonoBehaviour
{
    [Header("Runtime State")]
    [SerializeField] int currentDurability;
    [SerializeField] bool isDestroying;

    BallDataManager ballDataManager;
    bool isInitialized;

    BallDataSO BallData
    {
        get
        {
            if (ballDataManager == null)
            {
                ballDataManager = GetComponent<BallDataManager>();
            }

            return ballDataManager.BallData;
        }
    }

    public int CurrentDurability
    {
        get
        {
            return currentDurability;
        }
    }

    public int MaxDurability
    {
        get
        {
            BallDataSO data = BallData;
            if (data == null)
            {
                return 0;
            }

            return data.MaxDurability;
        }
    }

    public bool IsDestroying
    {
        get
        {
            return isDestroying;
        }
    }

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

        if (!ballDataManager.ValidateData())
        {
            return;
        }

        currentDurability = Mathf.Max(1, ballDataManager.BallData.MaxDurability);
        isDestroying = false;
        isInitialized = true;
    }

    public void ApplyWallHitDurabilityDamage()
    {
        if (!EnsureInitialized())
        {
            return;
        }

        TakeDurabilityDamage(BallData.WallHitDurabilityDamage);
    }

    public void ApplyObjectHitDurabilityDamage()
    {
        if (!EnsureInitialized())
        {
            return;
        }

        TakeDurabilityDamage(BallData.ObjectHitDurabilityDamage);
    }

    public void TakeDurabilityDamage(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[BallRuntimeStatus] 감소할 내구도는 1 이상이어야 합니다. 입력값: {amount}", this);
            return;
        }

        if (isDestroying)
        {
            return;
        }

        currentDurability -= amount;
        Debug.Log($"[BallRuntimeStatus] {name} 내구도 감소: -{amount} / 현재 내구도: {currentDurability}", this);

        if (IsDead())
        {
            DestroyBall();
        }
    }

    public void HealDurability(int amount)
    {
        if (!EnsureInitialized())
        {
            return;
        }

        if (amount <= 0)
        {
            Debug.LogWarning($"[BallRuntimeStatus] 회복할 내구도는 1 이상이어야 합니다. 입력값: {amount}", this);
            return;
        }

        currentDurability = Mathf.Min(currentDurability + amount, BallData.MaxDurability);
        Debug.Log($"[BallRuntimeStatus] {name} 내구도 회복: +{amount} / 현재 내구도: {currentDurability}", this);
    }

    public bool IsDead()
    {
        return currentDurability <= 0;
    }

    public void DestroyBall()
    {
        // DestroySelf 효과와 내구도 감소가 동시에 들어와도 파괴 처리는 한 번만 실행합니다.
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

    bool EnsureInitialized()
    {
        if (!isInitialized)
        {
            Initialize();
        }

        return BallData != null;
    }
}
