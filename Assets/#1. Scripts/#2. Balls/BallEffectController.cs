using System.Collections.Generic;
using UnityEngine;

// BallEffectController는 BallDataManager가 들고 있는 BallDataSO의 효과 목록을 읽고 실행합니다.
// BallDataSO는 BallDataManager 한 곳에서만 관리하고, 이 컴포넌트는 실행에 필요한 런타임 상태만 가집니다.
[RequireComponent(typeof(BallDataManager))]
public class BallEffectController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] DamageManager damageManager;

    [Header("Options")]
    [SerializeField] bool triggerSpawnEffectsOnEnable = true;

    [Header("Runtime State")]
    [SerializeField] List<BallEffectRuntimeState> runtimeStates = new List<BallEffectRuntimeState>();

    static bool isCreatingSplitBall;

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
        if (!triggerSpawnEffectsOnEnable)
        {
            return;
        }

        // SplitBall이 복제 공을 만드는 순간에는 새 공의 OnSpawn 효과를 건너뜁니다.
        // OnSpawn에 SplitBall을 넣었을 때 무한 복제가 생기는 것을 막기 위한 안전장치입니다.
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
        int splitCount = GetSplitCount(effectData);
        if (splitCount <= 0)
        {
            return;
        }

        Vector2 baseDirection = GetSplitBaseDirection();
        float speed = GetSplitSpeed();
        float startAngle = Random.Range(0f, 360f);

        isCreatingSplitBall = true;

        for (int i = 0; i < splitCount; i++)
        {
            float angle = startAngle + (360f / splitCount * i);
            Vector2 direction = RotateDirection(baseDirection, angle);
            Vector3 spawnPosition = transform.position + (Vector3)(direction * 0.25f);

            GameObject splitBall = Instantiate(gameObject, spawnPosition, Quaternion.identity);
            splitBall.name = $"{gameObject.name} Split";

            Rigidbody2D splitRigidbody = splitBall.GetComponent<Rigidbody2D>();
            if (splitRigidbody != null)
            {
                splitRigidbody.linearVelocity = direction * speed;
            }

            BallRuntimeStatus splitRuntimeStatus = splitBall.GetComponent<BallRuntimeStatus>();
            if (splitRuntimeStatus != null)
            {
                splitRuntimeStatus.Initialize();
            }

            BallEffectController splitEffectController = splitBall.GetComponent<BallEffectController>();
            if (splitEffectController != null)
            {
                splitEffectController.Initialize();
            }
        }

        isCreatingSplitBall = false;

        Debug.Log($"[BallEffectController] {name} 공이 {splitCount}개로 분열했습니다.", this);
    }

    int GetSplitCount(BallEffectData effectData)
    {
        int minCount = Mathf.Max(1, Mathf.RoundToInt(effectData.minValue));
        int maxCount = Mathf.Max(minCount, Mathf.RoundToInt(effectData.maxValue));

        // 너무 큰 값이 들어오면 실수로 씬에 공을 과도하게 만드는 일이 생기므로 기본 구조에서는 제한합니다.
        maxCount = Mathf.Min(maxCount, 20);

        return Random.Range(minCount, maxCount + 1);
    }

    Vector2 GetSplitBaseDirection()
    {
        if (ballRigidbody != null && ballRigidbody.linearVelocity.sqrMagnitude > 0.0001f)
        {
            return ballRigidbody.linearVelocity.normalized;
        }

        return Random.insideUnitCircle.normalized;
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

            // BallDataSO에는 상황별 리스트가 따로 있으므로, 실제 발동 상황은 리스트 위치를 기준으로 기억합니다.
            // BallEffectData 안의 trigger 값은 Inspector에서 의도를 읽기 쉽게 남겨둔 설정값입니다.
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
