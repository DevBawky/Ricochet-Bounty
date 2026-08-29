using System.Collections.Generic;
using UnityEngine;

// BallDataSO에 정의된 효과 목록을 읽고, 각 공 오브젝트에서 런타임 효과를 실행합니다.
// 점수, 내구도, 분열 같은 런타임 상태는 공마다 따로 관리합니다.
[RequireComponent(typeof(BallDataManager))]
public class BallEffectController : MonoBehaviour
{
    const string BallLayerName = "Ball";

    [Header("참조")]
    [SerializeField] DamageManager damageManager;
    [SerializeField] GoldManager goldManager;
    [SerializeField] BallRegistry ballRegistry;
    [SerializeField] ShotRuntimeContext shotRuntimeContext;

    [Header("옵션")]
    [SerializeField] bool triggerSpawnEffectsOnEnable = true;

    [Header("런타임 상태")]
    [SerializeField] List<BallEffectRuntimeState> runtimeStates = new List<BallEffectRuntimeState>();
    [SerializeField] bool canSplit = true;
    [SerializeField] int stackCashOutChipsStack;
    [SerializeField] int stackCashOutMultiplierStack;
    [SerializeField] int stackChipsStack;
    [SerializeField] int stackMultiplierStack;
    [SerializeField] int overheatChipsOverheat;
    [SerializeField] int overheatMultiplierOverheat;
    [SerializeField] int linkedCashOutStack;
    [SerializeField] int linkedStack;
    [SerializeField] int linkedOverheat;
    [SerializeField] bool isColonyChild;
    [SerializeField] bool breedingAttempted;
    [SerializeField] bool dividendRegistered;
    [SerializeField] int highPopulationObjectHits;
    [SerializeField] int goldEarnedByThisBall;

    static bool isCreatingSplitBall;
    public static bool SuppressSpawnEffectsOnEnable;

    BallDataManager ballDataManager;
    BallRuntimeStatus runtimeStatus;
    Rigidbody2D ballRigidbody;
    bool isInitialized;
    Vector3 currentEffectWorldPosition;

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

    void Awake()
    {
        Initialize();
    }

    void OnEnable()
    {
        if (SuppressSpawnEffectsOnEnable)
        {
            return;
        }

        if (!triggerSpawnEffectsOnEnable)
        {
            return;
        }

        // SplitBall로 복제되는 순간에는 새 공의 OnSpawn 효과를 건너뜁니다.
        // OnSpawn에 SplitBall이 있을 때 생성 즉시 무한 분열되는 것을 막기 위함입니다.
        if (isCreatingSplitBall)
        {
            return;
        }

        TriggerSpawnEffects();
    }

    public void Initialize()
    {
        if (ballDataManager == null)
        {
            ballDataManager = GetComponent<BallDataManager>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<BallRuntimeStatus>();
        }

        if (ballRigidbody == null)
        {
            ballRigidbody = GetComponent<Rigidbody2D>();
        }

        if (damageManager == null)
        {
            damageManager = FindFirstObjectByType<DamageManager>();
        }

        if (goldManager == null)
        {
            goldManager = FindFirstObjectByType<GoldManager>();
        }

        if (ballRegistry == null)
        {
            ballRegistry = FindFirstObjectByType<BallRegistry>();
        }

        if (shotRuntimeContext == null)
        {
            shotRuntimeContext = FindFirstObjectByType<ShotRuntimeContext>();
        }

        if (!ballDataManager.ValidateData())
        {
            return;
        }

        if (damageManager == null)
        {
            Debug.LogWarning("[BallEffectController] DamageManager를 찾지 못했습니다. 점수 관련 효과는 실행할 수 없습니다.", this);
        }

        if (runtimeStatus == null)
        {
            Debug.LogWarning("[BallEffectController] BallRuntimeStatus를 찾지 못했습니다. 내구도 관련 효과는 실행할 수 없습니다.", this);
        }

        BuildRuntimeStates();
        isInitialized = true;
    }

    public void DisableSplit()
    {
        // 분열로 태어난 공은 다시 SplitBall 효과를 실행하지 못하게 합니다.
        // 점수 증가, 내구도 회복, 파괴 같은 다른 효과는 그대로 동작합니다.
        canSplit = false;
    }

    public void ConfigureAsColonyChild()
    {
        isColonyChild = true;
        canSplit = false;
    }

    public void TriggerSpawnEffects()
    {
        currentEffectWorldPosition = transform.position;
        TriggerEffects(BallEffectTrigger.OnSpawn);
    }

    public void TriggerObjectHitEffects(Vector3 hitPosition)
    {
        currentEffectWorldPosition = hitPosition;
        RefreshMissingManagerReferences();
        if (isColonyChild && damageManager != null)
        {
            damageManager.AddChips(1, currentEffectWorldPosition);
        }

        TriggerEffects(BallEffectTrigger.OnObjectHit);
    }

    public void TriggerWallHitEffects(Vector3 hitPosition)
    {
        currentEffectWorldPosition = hitPosition;
        TriggerEffects(BallEffectTrigger.OnWallHit);
    }

    public void TriggerDestroyEffects()
    {
        currentEffectWorldPosition = transform.position;
        TriggerEffects(BallEffectTrigger.OnDestroy);
    }

    void TriggerEffects(BallEffectTrigger trigger)
    {
        if (!EnsureInitialized())
        {
            return;
        }

        RefreshMissingManagerReferences();

        for (int i = 0; i < runtimeStates.Count; i++)
        {
            BallEffectRuntimeState state = runtimeStates[i];
            if (state == null || state.EffectData == null)
            {
                continue;
            }

            if (state.Trigger != trigger)
            {
                continue;
            }

            if (!state.CanTrigger())
            {
                continue;
            }

            ExecuteEffect(state);
            state.MarkTriggered();
        }
    }

    void ExecuteEffect(BallEffectRuntimeState state)
    {
        if (state == null)
        {
            return;
        }

        BallEffectData effectData = state.EffectData;
        if (effectData == null)
        {
            return;
        }

        switch (effectData.effectType)
        {
            case BallEffectType.HealDurability:
                ExecuteHealDurability(effectData);
                break;

            case BallEffectType.SplitBall:
                ExecuteSplitBall(effectData);
                break;

            case BallEffectType.AddRandomScoreValue:
                ExecuteAddRandomScoreValue(effectData);
                break;

            case BallEffectType.DestroySelf:
                ExecuteDestroySelf();
                break;

            case BallEffectType.AddGold:
                ExecuteAddGold(effectData);
                break;

            case BallEffectType.StackCashOutChips:
                ExecuteStackCashOutChips(effectData, state.Trigger);
                break;

            case BallEffectType.StackMultiplier:
                ExecuteStackMultiplier(effectData, state.Trigger);
                break;

            case BallEffectType.OverheatMultiplier:
                ExecuteOverheatMultiplier(effectData, state.Trigger);
                break;

            case BallEffectType.StackCashOutMultiplier:
                ExecuteStackCashOutMultiplier(effectData, state.Trigger);
                break;

            case BallEffectType.StackChips:
                ExecuteStackChips(effectData, state.Trigger);
                break;

            case BallEffectType.OverheatChips:
                ExecuteOverheatChips(effectData, state.Trigger);
                break;

            case BallEffectType.StackCashOutEffect:
                ExecuteStackCashOutEffect(effectData, state.Trigger);
                break;

            case BallEffectType.StackEffect:
                ExecuteStackEffect(effectData, state.Trigger);
                break;

            case BallEffectType.OverheatEffect:
                ExecuteOverheatEffect(effectData, state.Trigger);
                break;

            case BallEffectType.BreedOnFirstWallHit:
                ExecuteBreedOnFirstWallHit(state.Trigger);
                break;

            case BallEffectType.SwarmPopulation:
                ExecuteSwarmPopulation(state.Trigger);
                break;

            case BallEffectType.CompoundBounty:
                ExecuteCompoundBounty(state.Trigger);
                break;

            case BallEffectType.Dividend:
                ExecuteDividend(state.Trigger);
                break;
        }
    }

    void ExecuteHealDurability(BallEffectData effectData)
    {
        if (runtimeStatus == null)
        {
            Debug.LogWarning("[BallEffectController] BallRuntimeStatus가 없어 내구도 회복 효과를 실행할 수 없습니다.", this);
            return;
        }

        GetOrderedRange(effectData, out float minValue, out float maxValue);
        int healAmount = Mathf.RoundToInt(Random.Range(minValue, maxValue));
        runtimeStatus.HealDurability(healAmount);
    }

    void ExecuteSplitBall(BallEffectData effectData)
    {
        if (!canSplit)
        {
            Debug.Log($"[BallEffectController] {name}은 분열로 생성된 공이므로 다시 분열하지 않습니다.", this);
            return;
        }

        int splitCount = GetSplitCount(effectData);
        if (splitCount <= 0)
        {
            return;
        }

        ExecuteSplitBallCount(splitCount, false);
    }

    void ExecuteSplitBallCount(int splitCount, bool createColonyChildren)
    {
        if (splitCount <= 0)
        {
            return;
        }

        RefreshMissingManagerReferences();

        if (ballRegistry != null)
        {
            splitCount = Mathf.Min(splitCount, ballRegistry.AvailableSlots);
        }
        else if (createColonyChildren)
        {
            Debug.LogWarning("[BallEffectController] BallRegistry is required to enforce the colony population limit. Breeding was skipped.", this);
            return;
        }

        if (splitCount <= 0)
        {
            return;
        }

        Vector2 baseDirection = GetSplitBaseDirection();
        float speed = GetSplitSpeed();
        float startAngle = Random.Range(0f, 360f);
        float spawnOffset = GetSplitSpawnOffset();

        isCreatingSplitBall = true;

        try
        {
            for (int i = 0; i < splitCount; i++)
            {
                float angle = startAngle + (360f / splitCount * i);
                Vector2 direction = RotateDirection(baseDirection, angle);
                Vector3 spawnPosition = transform.position + (Vector3)(direction * spawnOffset);

                GameObject splitBall = Instantiate(gameObject, spawnPosition, Quaternion.identity);
                splitBall.name = $"{gameObject.name} Split";
                ApplyOriginalIdentity(splitBall);

                BallRuntimeStatus splitRuntimeStatus = splitBall.GetComponent<BallRuntimeStatus>();
                if (splitRuntimeStatus != null)
                {
                    if (createColonyChildren)
                    {
                        splitRuntimeStatus.InitializeAsChild(1);
                    }
                    else
                    {
                        splitRuntimeStatus.Initialize();
                    }
                }

                BallEffectController splitEffectController = splitBall.GetComponent<BallEffectController>();
                if (splitEffectController != null)
                {
                    splitEffectController.DisableSplit();
                    if (createColonyChildren)
                    {
                        splitEffectController.ConfigureAsColonyChild();
                    }
                    splitEffectController.Initialize();
                }

                Ball splitBallMovement = splitBall.GetComponent<Ball>();
                if (splitBallMovement != null)
                {
                    // 원본의 물리/파괴 플래그를 그대로 쓰지 않고 새 공처럼 재초기화합니다.
                    splitBallMovement.InitializeAsSplitBall(direction, speed);
                }
            }
        }
        finally
        {
            isCreatingSplitBall = false;
        }

        Debug.Log($"[BallEffectController] {name} 공이 {splitCount}개로 분열했습니다.", this);
    }

    void ApplyOriginalIdentity(GameObject splitBall)
    {
        // Instantiate(gameObject)는 원본 공의 컴포넌트, 태그, 레이어를 복제합니다.
        // 그래도 자식 콜라이더 구조가 있을 수 있으므로 태그/레이어를 한 번 더 맞춰줍니다.
        int ballLayer = GetBallLayerOrFallback(gameObject.layer);
        splitBall.tag = gameObject.tag;
        splitBall.layer = ballLayer;

        Transform originalRoot = transform;
        Transform splitRoot = splitBall.transform;
        int childCount = Mathf.Min(originalRoot.childCount, splitRoot.childCount);

        for (int i = 0; i < childCount; i++)
        {
            CopyTransformIdentity(originalRoot.GetChild(i), splitRoot.GetChild(i), ballLayer);
        }
    }

    void CopyTransformIdentity(Transform original, Transform copied, int ballLayer)
    {
        copied.gameObject.tag = original.gameObject.tag;
        copied.gameObject.layer = ballLayer;

        int childCount = Mathf.Min(original.childCount, copied.childCount);
        for (int i = 0; i < childCount; i++)
        {
            CopyTransformIdentity(original.GetChild(i), copied.GetChild(i), ballLayer);
        }
    }

    int GetBallLayerOrFallback(int fallbackLayer)
    {
        int ballLayer = LayerMask.NameToLayer(BallLayerName);
        if (ballLayer == -1)
        {
            Debug.LogWarning($"[BallEffectController] '{BallLayerName}' 레이어가 없어 분열 공의 레이어를 원본 레이어로 유지합니다.", this);
            return fallbackLayer;
        }

        return ballLayer;
    }

    int GetSplitCount(BallEffectData effectData)
    {
        int minCount = Mathf.Max(1, Mathf.RoundToInt(effectData.minValue));
        int maxCount = Mathf.Max(minCount, Mathf.RoundToInt(effectData.maxValue));

        // 실수로 너무 큰 값을 넣었을 때 공이 과도하게 생기지 않도록 제한합니다.
        maxCount = Mathf.Min(maxCount, 20);

        return Random.Range(minCount, maxCount + 1);
    }

    Vector2 GetSplitBaseDirection()
    {
        if (ballRigidbody != null && ballRigidbody.linearVelocity.sqrMagnitude > 0.0001f)
        {
            return ballRigidbody.linearVelocity.normalized;
        }

        Vector2 randomDirection = Random.insideUnitCircle;
        if (randomDirection.sqrMagnitude > 0.0001f)
        {
            return randomDirection.normalized;
        }

        return Vector2.right;
    }

    float GetSplitSpeed()
    {
        float minimumSpeed = 0f;
        Ball ballMovement = GetComponent<Ball>();
        if (ballMovement != null)
        {
            minimumSpeed = ballMovement.MinimumMoveSpeed;
        }

        if (ballRigidbody != null && ballRigidbody.linearVelocity.sqrMagnitude > 0.0001f)
        {
            return Mathf.Max(ballRigidbody.linearVelocity.magnitude, minimumSpeed);
        }

        if (BallData != null && BallData.LaunchSpeed > 0f)
        {
            return Mathf.Max(BallData.LaunchSpeed, minimumSpeed);
        }

        return Mathf.Max(10f, minimumSpeed);
    }

    float GetSplitSpawnOffset()
    {
        // 원본과 너무 겹쳐 태어나면 첫 물리 프레임의 충돌 콜백이 불안정할 수 있습니다.
        // 콜라이더 크기 기준으로 살짝 밖에서 생성합니다.
        Collider2D ballCollider = GetComponent<Collider2D>();
        if (ballCollider == null)
        {
            return 0.5f;
        }

        return Mathf.Max(0.25f, ballCollider.bounds.extents.magnitude + 0.05f);
    }

    Vector2 RotateDirection(Vector2 direction, float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos
        ).normalized;
    }

    void ExecuteAddRandomScoreValue(BallEffectData effectData)
    {
        if (Random.value < 0.5f)
        {
            AddRandomChips(effectData);
            return;
        }

        AddRandomMultiplier(effectData);
    }

    void AddRandomChips(BallEffectData effectData)
    {
        if (damageManager == null)
        {
            Debug.LogWarning("[BallEffectController] DamageManager가 없어 Chips 증가 효과를 실행할 수 없습니다.", this);
            return;
        }

        GetOrderedRange(effectData, out float minValue, out float maxValue);
        int amount = Mathf.RoundToInt(Random.Range(minValue, maxValue));
        damageManager.AddChips(amount, currentEffectWorldPosition);
    }

    void AddRandomMultiplier(BallEffectData effectData)
    {
        if (damageManager == null)
        {
            Debug.LogWarning("[BallEffectController] DamageManager가 없어 Multiplier 증가 효과를 실행할 수 없습니다.", this);
            return;
        }

        GetOrderedRange(effectData, out float minValue, out float maxValue);
        float amount = Random.Range(minValue, maxValue);
        damageManager.AddMultiplier(amount, currentEffectWorldPosition);
    }

    void ExecuteDestroySelf()
    {
        Debug.Log($"[BallEffectController] {name} triggered DestroySelf.", this);

        if (runtimeStatus != null)
        {
            runtimeStatus.DestroyBall();
            return;
        }

        Destroy(gameObject);
    }

    void ExecuteAddGold(BallEffectData effectData)
    {
        if (goldManager == null)
        {
            Debug.LogWarning("[BallEffectController] GoldManager was not found. AddGold effect cannot run.", this);
            return;
        }

        GetOrderedRange(effectData, out float minValue, out float maxValue);
        int amount = Mathf.RoundToInt(Random.Range(minValue, maxValue));
        if (amount <= 0)
        {
            Debug.LogWarning($"[BallEffectController] AddGold amount must be greater than 0. Amount: {amount}", this);
            return;
        }

        GrantBallGold(amount, "AddGold", true);
    }

    void ExecuteBreedOnFirstWallHit(BallEffectTrigger trigger)
    {
        if (trigger != BallEffectTrigger.OnWallHit || breedingAttempted || isColonyChild)
        {
            return;
        }

        // The first wall hit consumes breeding even when the registry has no free slots.
        breedingAttempted = true;
        ExecuteSplitBallCount(2, true);
    }

    void ExecuteSwarmPopulation(BallEffectTrigger trigger)
    {
        if (trigger != BallEffectTrigger.OnObjectHit || damageManager == null)
        {
            return;
        }

        RefreshMissingManagerReferences();
        if (ballRegistry == null)
        {
            Debug.LogWarning("[BallEffectController] BallRegistry was not found. SwarmPopulation cannot run.", this);
            return;
        }

        int activeBallCount = ballRegistry.ActiveBallCount;
        int chips = activeBallCount >= 9 ? 4 : activeBallCount >= 6 ? 3 : 2;
        damageManager.AddChips(chips, currentEffectWorldPosition);

        if (activeBallCount < 9)
        {
            return;
        }

        highPopulationObjectHits++;
        if (highPopulationObjectHits % 3 == 0)
        {
            damageManager.AddMultiplier(0.2f, currentEffectWorldPosition);
        }
    }

    void ExecuteCompoundBounty(BallEffectTrigger trigger)
    {
        if (goldManager == null || damageManager == null)
        {
            return;
        }

        int gold = goldManager.CurrentGold;
        if (trigger == BallEffectTrigger.OnObjectHit)
        {
            int chips = Mathf.Clamp(gold / 5 + 1, 1, 5);
            damageManager.AddChips(chips, currentEffectWorldPosition);
            return;
        }

        if (trigger != BallEffectTrigger.OnDestroy)
        {
            return;
        }

        float multiplier = gold >= 30 ? 0.6f : gold >= 20 ? 0.4f : gold >= 10 ? 0.2f : 0f;
        if (multiplier > 0f)
        {
            damageManager.AddMultiplier(multiplier, currentEffectWorldPosition);
        }
    }

    void ExecuteDividend(BallEffectTrigger trigger)
    {
        if (trigger != BallEffectTrigger.OnSpawn || dividendRegistered)
        {
            return;
        }

        RefreshMissingManagerReferences();
        if (shotRuntimeContext == null)
        {
            Debug.LogWarning("[BallEffectController] ShotRuntimeContext was not found. Dividend registration was skipped.", this);
            return;
        }

        dividendRegistered = true;
        shotRuntimeContext.RegisterDividendBall();
    }

    void GrantBallGold(int requestedAmount, string context, bool enforceGoldenBallCap)
    {
        RefreshMissingManagerReferences();
        if (goldManager == null || requestedAmount <= 0)
        {
            return;
        }

        if (isColonyChild)
        {
            Debug.Log($"[BallEffectController] Colony child {name} cannot earn gold. Context: {context}", this);
            return;
        }

        const int MaxGoldPerBall = 2;
        int amount = enforceGoldenBallCap
            ? Mathf.Min(requestedAmount, MaxGoldPerBall - goldEarnedByThisBall)
            : requestedAmount;
        if (amount <= 0)
        {
            return;
        }

        goldManager.AddGold(amount);
        if (enforceGoldenBallCap)
        {
            goldEarnedByThisBall += amount;
        }

        if (shotRuntimeContext != null)
        {
            shotRuntimeContext.RecordGoldEarned(amount);
        }

        string capLog = enforceGoldenBallCap ? $" ({goldEarnedByThisBall}/{MaxGoldPerBall})" : string.Empty;
        Debug.Log($"[BallEffectController] {name} granted {amount} gold{capLog}. Context: {context}", this);
    }

    void ExecuteStackCashOutChips(BallEffectData effectData, BallEffectTrigger trigger)
    {
        if (trigger == BallEffectTrigger.OnObjectHit || trigger == BallEffectTrigger.OnWallHit)
        {
            stackCashOutChipsStack++;
            Debug.Log($"[StackCashOutChipsBallEffect] {name} Stack increased. Trigger: {trigger}, Stack: {stackCashOutChipsStack}", this);
            return;
        }

        if (trigger != BallEffectTrigger.OnDestroy)
        {
            return;
        }

        int chipsPerStack = Mathf.Max(0, effectData.chipsPerStack);
        int grantedChips = stackCashOutChipsStack * chipsPerStack;

        if (damageManager == null)
        {
            Debug.LogWarning($"[StackCashOutChipsBallEffect] DamageManager was not found. Final Stack: {stackCashOutChipsStack}, Chips not granted.", this);
            stackCashOutChipsStack = 0;
            return;
        }

        if (grantedChips > 0)
        {
            damageManager.AddChips(grantedChips, currentEffectWorldPosition);
        }

        Debug.Log($"[StackCashOutChipsBallEffect] {name} cashed out. Final Stack: {stackCashOutChipsStack}, Chips Per Stack: {chipsPerStack}, Granted Chips: {grantedChips}", this);
        stackCashOutChipsStack = 0;
    }

    void ExecuteStackCashOutMultiplier(BallEffectData effectData, BallEffectTrigger trigger)
    {
        if (trigger == BallEffectTrigger.OnObjectHit || trigger == BallEffectTrigger.OnWallHit)
        {
            stackCashOutMultiplierStack++;
            Debug.Log($"[StackCashOutMultiplierBallEffect] {name} Stack increased. Trigger: {trigger}, Stack: {stackCashOutMultiplierStack}", this);
            return;
        }

        if (trigger != BallEffectTrigger.OnDestroy)
        {
            return;
        }

        float multiplierPerStack = Mathf.Max(0f, effectData.multiplierPerStack);
        float grantedMultiplier = stackCashOutMultiplierStack * multiplierPerStack;

        if (damageManager == null)
        {
            Debug.LogWarning($"[StackCashOutMultiplierBallEffect] DamageManager was not found. Final Stack: {stackCashOutMultiplierStack}, Mult not granted.", this);
            stackCashOutMultiplierStack = 0;
            return;
        }

        if (grantedMultiplier > 0f)
        {
            damageManager.AddMultiplier(grantedMultiplier, currentEffectWorldPosition);
        }

        Debug.Log($"[StackCashOutMultiplierBallEffect] {name} cashed out. Final Stack: {stackCashOutMultiplierStack}, Mult Per Stack: {multiplierPerStack}, Granted Mult: {grantedMultiplier}", this);
        stackCashOutMultiplierStack = 0;
    }

    void ExecuteStackChips(BallEffectData effectData, BallEffectTrigger trigger)
    {
        if (trigger != BallEffectTrigger.OnObjectHit)
        {
            return;
        }

        int targetStack = Mathf.Max(1, effectData.targetStack);
        stackChipsStack++;

        Debug.Log($"[StackChipsBallEffect] {name} Stack increased. Stack: {stackChipsStack}/{targetStack}", this);

        if (stackChipsStack < targetStack)
        {
            return;
        }

        int amount = Mathf.Max(0, effectData.chipsIncrease);
        if (damageManager == null)
        {
            Debug.LogWarning($"[StackChipsBallEffect] DamageManager was not found. Chips not granted. Stack reset from {stackChipsStack}.", this);
            stackChipsStack = 0;
            return;
        }

        if (amount > 0)
        {
            damageManager.AddChips(amount, currentEffectWorldPosition);
        }

        Debug.Log($"[StackChipsBallEffect] {name} reached target Stack. Chips Granted: +{amount}, Stack Reset: {stackChipsStack}->0", this);
        stackChipsStack = 0;
    }

    void ExecuteStackMultiplier(BallEffectData effectData, BallEffectTrigger trigger)
    {
        if (trigger != BallEffectTrigger.OnObjectHit)
        {
            return;
        }

        int targetStack = Mathf.Max(1, effectData.targetStack);
        stackMultiplierStack++;

        Debug.Log($"[StackMultiplierBallEffect] {name} Stack increased. Stack: {stackMultiplierStack}/{targetStack}", this);

        if (stackMultiplierStack < targetStack)
        {
            return;
        }

        float amount = Mathf.Max(0f, effectData.multiplierIncrease);
        if (damageManager == null)
        {
            Debug.LogWarning($"[StackMultiplierBallEffect] DamageManager was not found. Mult not granted. Stack reset from {stackMultiplierStack}.", this);
            stackMultiplierStack = 0;
            return;
        }

        if (amount > 0f)
        {
            damageManager.AddMultiplier(amount, currentEffectWorldPosition);
        }

        Debug.Log($"[StackMultiplierBallEffect] {name} reached target Stack. Mult Granted: +{amount}, Stack Reset: {stackMultiplierStack}->0", this);
        stackMultiplierStack = 0;
    }

    void ExecuteOverheatMultiplier(BallEffectData effectData, BallEffectTrigger trigger)
    {
        if (trigger != BallEffectTrigger.OnObjectHit && trigger != BallEffectTrigger.OnWallHit)
        {
            return;
        }

        overheatMultiplierOverheat++;

        float baseIncrease = Mathf.Max(0f, effectData.baseMultiplierIncrease);
        float increasePerOverheat = Mathf.Max(0f, effectData.multiplierIncreasePerOverheat);
        float amount = baseIncrease + overheatMultiplierOverheat * increasePerOverheat;

        if (damageManager == null)
        {
            Debug.LogWarning($"[OverheatMultiplierBallEffect] DamageManager was not found. Overheat: {overheatMultiplierOverheat}, Mult not granted.", this);
        }
        else if (amount > 0f)
        {
            damageManager.AddMultiplier(amount, currentEffectWorldPosition);
        }

        int selfDestroyStartOverheat = Mathf.Max(1, effectData.selfDestroyStartOverheat);
        float selfDestroyChance = Mathf.Clamp01(effectData.selfDestroyChance);
        bool canRollSelfDestroy = overheatMultiplierOverheat >= selfDestroyStartOverheat && selfDestroyChance > 0f;
        float roll = canRollSelfDestroy ? Random.value : -1f;
        bool shouldSelfDestroy = canRollSelfDestroy && roll <= selfDestroyChance;

        Debug.Log($"[OverheatMultiplierBallEffect] {name} hit. Trigger: {trigger}, Overheat: {overheatMultiplierOverheat}, Mult Granted: +{amount}, Self Destroy Roll: {(canRollSelfDestroy ? roll.ToString("0.000") : "Not Ready")}, Chance: {selfDestroyChance}, Destroy: {shouldSelfDestroy}", this);

        if (!shouldSelfDestroy)
        {
            return;
        }

        ExecuteDestroySelf();
    }

    void ExecuteOverheatChips(BallEffectData effectData, BallEffectTrigger trigger)
    {
        if (trigger != BallEffectTrigger.OnObjectHit && trigger != BallEffectTrigger.OnWallHit)
        {
            return;
        }

        overheatChipsOverheat++;

        int baseIncrease = Mathf.Max(0, effectData.baseChipsIncrease);
        float increasePerOverheat = Mathf.Max(0f, effectData.chipsIncreasePerOverheat);
        int amount = Mathf.RoundToInt(baseIncrease + overheatChipsOverheat * increasePerOverheat);

        if (damageManager == null)
        {
            Debug.LogWarning($"[OverheatChipsBallEffect] DamageManager was not found. Overheat: {overheatChipsOverheat}, Chips not granted.", this);
        }
        else if (amount > 0)
        {
            damageManager.AddChips(amount, currentEffectWorldPosition);
        }

        int selfDestroyStartOverheat = Mathf.Max(1, effectData.selfDestroyStartOverheat);
        float selfDestroyChance = Mathf.Clamp01(effectData.selfDestroyChance);
        bool canRollSelfDestroy = overheatChipsOverheat >= selfDestroyStartOverheat && selfDestroyChance > 0f;
        float roll = canRollSelfDestroy ? Random.value : -1f;
        bool shouldSelfDestroy = canRollSelfDestroy && roll <= selfDestroyChance;

        Debug.Log($"[OverheatChipsBallEffect] {name} hit. Trigger: {trigger}, Overheat: {overheatChipsOverheat}, Chips Granted: +{amount}, Self Destroy Roll: {(canRollSelfDestroy ? roll.ToString("0.000") : "Not Ready")}, Chance: {selfDestroyChance}, Destroy: {shouldSelfDestroy}", this);

        if (!shouldSelfDestroy)
        {
            return;
        }

        ExecuteDestroySelf();
    }

    void ExecuteStackCashOutEffect(BallEffectData effectData, BallEffectTrigger trigger)
    {
        if (trigger == BallEffectTrigger.OnObjectHit || trigger == BallEffectTrigger.OnWallHit)
        {
            linkedCashOutStack++;
            Debug.Log($"[StackCashOutEffect] {name} Stack increased. Trigger: {trigger}, Stack: {linkedCashOutStack}, Reward: {effectData.rewardEffectType}", this);
            return;
        }

        if (trigger != BallEffectTrigger.OnDestroy)
        {
            return;
        }

        float rewardValue = linkedCashOutStack * Mathf.Max(0f, effectData.rewardValuePerStack);
        ExecuteRewardEffect(effectData.rewardEffectType, rewardValue, $"StackCashOutEffect Stack: {linkedCashOutStack}");

        Debug.Log($"[StackCashOutEffect] {name} cashed out. Final Stack: {linkedCashOutStack}, Reward: {effectData.rewardEffectType}, Reward Value: {rewardValue}", this);
        linkedCashOutStack = 0;
    }

    void ExecuteStackEffect(BallEffectData effectData, BallEffectTrigger trigger)
    {
        if (trigger != BallEffectTrigger.OnObjectHit)
        {
            return;
        }

        int targetStack = Mathf.Max(1, effectData.targetStack);
        linkedStack++;

        Debug.Log($"[StackEffect] {name} Stack increased. Stack: {linkedStack}/{targetStack}, Reward: {effectData.rewardEffectType}", this);

        if (linkedStack < targetStack)
        {
            return;
        }

        float minValue = Mathf.Min(effectData.rewardMinValue, effectData.rewardMaxValue);
        float maxValue = Mathf.Max(effectData.rewardMinValue, effectData.rewardMaxValue);
        float rewardValue = Random.Range(minValue, maxValue);

        ExecuteRewardEffect(effectData.rewardEffectType, rewardValue, $"StackEffect Stack: {linkedStack}/{targetStack}");

        Debug.Log($"[StackEffect] {name} reached target Stack. Reward: {effectData.rewardEffectType}, Reward Value: {rewardValue}, Stack Reset: {linkedStack}->0", this);
        linkedStack = 0;
    }

    void ExecuteOverheatEffect(BallEffectData effectData, BallEffectTrigger trigger)
    {
        if (trigger != BallEffectTrigger.OnObjectHit && trigger != BallEffectTrigger.OnWallHit)
        {
            return;
        }

        linkedOverheat++;

        float baseValue = Mathf.Max(0f, effectData.baseRewardValue);
        float valuePerOverheat = Mathf.Max(0f, effectData.rewardValuePerOverheat);
        float rewardValue = baseValue + linkedOverheat * valuePerOverheat;

        ExecuteRewardEffect(effectData.rewardEffectType, rewardValue, $"OverheatEffect Overheat: {linkedOverheat}");

        int selfDestroyStartOverheat = Mathf.Max(1, effectData.selfDestroyStartOverheat);
        float selfDestroyChance = Mathf.Clamp01(effectData.selfDestroyChance);
        bool canRollSelfDestroy = linkedOverheat >= selfDestroyStartOverheat && selfDestroyChance > 0f;
        float roll = canRollSelfDestroy ? Random.value : -1f;
        bool shouldSelfDestroy = canRollSelfDestroy && roll <= selfDestroyChance;

        Debug.Log($"[OverheatEffect] {name} hit. Trigger: {trigger}, Overheat: {linkedOverheat}, Reward: {effectData.rewardEffectType}, Reward Value: {rewardValue}, Self Destroy Roll: {(canRollSelfDestroy ? roll.ToString("0.000") : "Not Ready")}, Chance: {selfDestroyChance}, Destroy: {shouldSelfDestroy}", this);

        if (!shouldSelfDestroy)
        {
            return;
        }

        ExecuteDestroySelf();
    }

    void ExecuteRewardEffect(BallEffectRewardType rewardType, float value, string context)
    {
        switch (rewardType)
        {
            case BallEffectRewardType.AddChips:
                AddRewardChips(value, context);
                break;

            case BallEffectRewardType.AddMultiplier:
                AddRewardMultiplier(value, context);
                break;

            case BallEffectRewardType.AddRandomScoreValue:
                ExecuteRewardRandomScoreValue(value, context);
                break;

            case BallEffectRewardType.HealDurability:
                ExecuteRewardHealDurability(value, context);
                break;

            case BallEffectRewardType.SplitBall:
                ExecuteRewardSplitBall(value, context);
                break;

            case BallEffectRewardType.DestroySelf:
                Debug.Log($"[BallEffectController] Linked reward DestroySelf triggered. Context: {context}", this);
                ExecuteDestroySelf();
                break;

            case BallEffectRewardType.AddGold:
                ExecuteRewardAddGold(value, context);
                break;
        }
    }

    void AddRewardChips(float value, string context)
    {
        if (damageManager == null)
        {
            Debug.LogWarning($"[BallEffectController] DamageManager was not found. Linked Chips reward cannot run. Context: {context}", this);
            return;
        }

        int amount = Mathf.RoundToInt(Mathf.Max(0f, value));
        if (amount <= 0)
        {
            return;
        }

        damageManager.AddChips(amount, currentEffectWorldPosition);
    }

    void AddRewardMultiplier(float value, string context)
    {
        if (damageManager == null)
        {
            Debug.LogWarning($"[BallEffectController] DamageManager was not found. Linked Mult reward cannot run. Context: {context}", this);
            return;
        }

        float amount = Mathf.Max(0f, value);
        if (amount <= 0f)
        {
            return;
        }

        damageManager.AddMultiplier(amount, currentEffectWorldPosition);
    }

    void ExecuteRewardRandomScoreValue(float value, string context)
    {
        if (Random.value < 0.5f)
        {
            AddRewardChips(value, context);
            return;
        }

        AddRewardMultiplier(value, context);
    }

    void ExecuteRewardHealDurability(float value, string context)
    {
        if (runtimeStatus == null)
        {
            Debug.LogWarning($"[BallEffectController] BallRuntimeStatus was not found. Linked heal reward cannot run. Context: {context}", this);
            return;
        }

        int amount = Mathf.RoundToInt(Mathf.Max(0f, value));
        if (amount <= 0)
        {
            return;
        }

        runtimeStatus.HealDurability(amount);
    }

    void ExecuteRewardSplitBall(float value, string context)
    {
        if (!canSplit)
        {
            Debug.Log($"[BallEffectController] Linked SplitBall reward skipped because this ball cannot split. Context: {context}", this);
            return;
        }

        int splitCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(0f, value)), 0, 20);
        ExecuteSplitBallCount(splitCount, false);
    }

    void ExecuteRewardAddGold(float value, string context)
    {
        if (goldManager == null)
        {
            Debug.LogWarning($"[BallEffectController] GoldManager was not found. Linked gold reward cannot run. Context: {context}", this);
            return;
        }

        int amount = Mathf.RoundToInt(Mathf.Max(0f, value));
        if (amount <= 0)
        {
            return;
        }

        GrantBallGold(amount, context, false);
    }

    void BuildRuntimeStates()
    {
        runtimeStates.Clear();
        stackCashOutChipsStack = 0;
        stackCashOutMultiplierStack = 0;
        stackChipsStack = 0;
        stackMultiplierStack = 0;
        overheatChipsOverheat = 0;
        overheatMultiplierOverheat = 0;
        linkedCashOutStack = 0;
        linkedStack = 0;
        linkedOverheat = 0;
        breedingAttempted = false;
        dividendRegistered = false;
        highPopulationObjectHits = 0;
        goldEarnedByThisBall = 0;

        AddRuntimeStates(BallData.SpawnEffects, BallEffectTrigger.OnSpawn);
        AddRuntimeStates(BallData.ObjectHitEffects, BallEffectTrigger.OnObjectHit);
        AddRuntimeStates(BallData.WallHitEffects, BallEffectTrigger.OnWallHit);
        AddRuntimeStates(BallData.DestroyEffects, BallEffectTrigger.OnDestroy);
    }

    void RefreshMissingManagerReferences()
    {
        if (damageManager == null)
        {
            damageManager = FindFirstObjectByType<DamageManager>();
        }

        if (goldManager == null)
        {
            goldManager = FindFirstObjectByType<GoldManager>();
        }

        if (ballRegistry == null)
        {
            ballRegistry = FindFirstObjectByType<BallRegistry>();
        }

        if (shotRuntimeContext == null)
        {
            shotRuntimeContext = FindFirstObjectByType<ShotRuntimeContext>();
        }
    }

    void GetOrderedRange(BallEffectData effectData, out float minValue, out float maxValue)
    {
        minValue = Mathf.Min(effectData.minValue, effectData.maxValue);
        maxValue = Mathf.Max(effectData.minValue, effectData.maxValue);
    }

    void AddRuntimeStates(List<BallEffectData> effects, BallEffectTrigger trigger)
    {
        if (effects == null)
        {
            return;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] == null)
            {
                continue;
            }

            runtimeStates.Add(new BallEffectRuntimeState(effects[i], trigger));
        }
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
