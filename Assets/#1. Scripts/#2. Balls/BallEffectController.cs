using System.Collections.Generic;
using UnityEngine;

// BallDataSO에 정의된 효과 목록을 읽고, 각 공 오브젝트에서 런타임 효과를 실행합니다.
// 점수, 내구도, 분열 같은 런타임 상태는 공마다 따로 관리합니다.
[RequireComponent(typeof(BallDataManager))]
public class BallEffectController : MonoBehaviour
{
    const string BallLayerName = "Ball";

    [Header("References")]
    [SerializeField] DamageManager damageManager;

    [Header("Options")]
    [SerializeField] bool triggerSpawnEffectsOnEnable = true;

    [Header("Runtime State")]
    [SerializeField] List<BallEffectRuntimeState> runtimeStates = new List<BallEffectRuntimeState>();
    [SerializeField] bool canSplit = true;

    static bool isCreatingSplitBall;
    public static bool SuppressSpawnEffectsOnEnable;

    BallDataManager ballDataManager;
    BallRuntimeStatus runtimeStatus;
    Rigidbody2D ballRigidbody;
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

    public void TriggerSpawnEffects()
    {
        TriggerEffects(BallEffectTrigger.OnSpawn);
    }

    public void TriggerObjectHitEffects()
    {
        TriggerEffects(BallEffectTrigger.OnObjectHit);
    }

    public void TriggerWallHitEffects()
    {
        TriggerEffects(BallEffectTrigger.OnWallHit);
    }

    public void TriggerDestroyEffects()
    {
        TriggerEffects(BallEffectTrigger.OnDestroy);
    }

    void TriggerEffects(BallEffectTrigger trigger)
    {
        if (!EnsureInitialized())
        {
            return;
        }

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

            ExecuteEffect(state.EffectData);
            state.MarkTriggered();
        }
    }

    void ExecuteEffect(BallEffectData effectData)
    {
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
        }
    }

    void ExecuteHealDurability(BallEffectData effectData)
    {
        if (runtimeStatus == null)
        {
            Debug.LogWarning("[BallEffectController] BallRuntimeStatus가 없어 내구도 회복 효과를 실행할 수 없습니다.", this);
            return;
        }

        int healAmount = Mathf.RoundToInt(Random.Range(effectData.minValue, effectData.maxValue));
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
                    splitRuntimeStatus.Initialize();
                }

                BallEffectController splitEffectController = splitBall.GetComponent<BallEffectController>();
                if (splitEffectController != null)
                {
                    splitEffectController.DisableSplit();
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
        if (ballRigidbody != null && ballRigidbody.linearVelocity.sqrMagnitude > 0.0001f)
        {
            return ballRigidbody.linearVelocity.magnitude;
        }

        if (BallData != null && BallData.LaunchSpeed > 0f)
        {
            return BallData.LaunchSpeed;
        }

        return 10f;
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

        int amount = Mathf.RoundToInt(Random.Range(effectData.minValue, effectData.maxValue));
        damageManager.AddChips(amount);
    }

    void AddRandomMultiplier(BallEffectData effectData)
    {
        if (damageManager == null)
        {
            Debug.LogWarning("[BallEffectController] DamageManager가 없어 Multiplier 증가 효과를 실행할 수 없습니다.", this);
            return;
        }

        float amount = Random.Range(effectData.minValue, effectData.maxValue);
        damageManager.AddMultiplier(amount);
    }

    void ExecuteDestroySelf()
    {
        if (runtimeStatus != null)
        {
            runtimeStatus.DestroyBall();
            return;
        }

        Destroy(gameObject);
    }

    void BuildRuntimeStates()
    {
        runtimeStates.Clear();

        AddRuntimeStates(BallData.SpawnEffects, BallEffectTrigger.OnSpawn);
        AddRuntimeStates(BallData.ObjectHitEffects, BallEffectTrigger.OnObjectHit);
        AddRuntimeStates(BallData.WallHitEffects, BallEffectTrigger.OnWallHit);
        AddRuntimeStates(BallData.DestroyEffects, BallEffectTrigger.OnDestroy);
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
