using UnityEngine;

// ScoreObject는 공이 닿았을 때 점수를 적용하는 오브젝트에 붙입니다.
// DamageManager가 직접 공 데이터를 찾지 않고, 충돌한 공의 BallDataManager에서 BallDataSO를 읽어 전달합니다.
// 이 스크립트는 점수 처리 후 공의 오브젝트 충돌 내구도와 효과도 함께 호출합니다.
[RequireComponent(typeof(Collider2D))]
public class ScoreObject : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField] DamageManager damageManager;

    [Header("Ball Check")]
    [SerializeField] string ballTag = "Ball";

    void Awake()
    {
        // Inspector에서 직접 연결하지 않아도 씬의 DamageManager를 자동으로 찾습니다.
        // Unity 6에서는 예전 FindObjectOfType보다 FindFirstObjectByType 사용을 권장합니다.
        if (damageManager == null)
        {
            damageManager = FindFirstObjectByType<DamageManager>();
        }

        if (damageManager == null)
        {
            Debug.LogWarning("[ScoreObject] DamageManager를 찾지 못했습니다. 점수 적용을 할 수 없습니다.", this);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 일반 Collider2D끼리 부딪히는 점수 오브젝트는 이 경로를 사용합니다.
        ApplyScoreFromBall(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 나중에 Trigger 방식 점수 오브젝트를 만들 때도 같은 점수 처리 흐름을 사용할 수 있습니다.
        ApplyScoreFromBall(other.gameObject);
    }

    void ApplyScoreFromBall(GameObject hitObject)
    {
        if (damageManager == null || hitObject == null)
        {
            return;
        }

        // 충돌한 오브젝트 또는 그 부모에서 BallDataManager를 찾습니다.
        // 이렇게 하면 공의 Collider가 자식 오브젝트에 있어도 같은 흐름으로 처리할 수 있습니다.
        BallDataManager ballDataManager = hitObject.GetComponentInParent<BallDataManager>();
        if (ballDataManager == null)
        {
            return;
        }

        // Ball 태그가 아닌 오브젝트는 점수 대상으로 처리하지 않습니다.
        if (!ballDataManager.CompareTag(ballTag))
        {
            return;
        }

        if (!ballDataManager.ValidateData())
        {
            return;
        }

        BallDataSO ballData = ballDataManager.BallData;
        damageManager.ApplyBallData(ballData);
        ApplyBallObjectHitSystems(ballDataManager);
    }

    void ApplyBallObjectHitSystems(BallDataManager ballDataManager)
    {
        // 점수 오브젝트에 닿았을 때 공의 내구도와 효과도 함께 처리합니다.
        // DamageManager는 점수만 담당하고, 공의 런타임 상태는 공에 붙은 컴포넌트들이 담당합니다.
        BallEffectController effectController = ballDataManager.GetComponent<BallEffectController>();
        if (effectController != null)
        {
            effectController.TriggerObjectHitEffects();
        }

        // Effects run first so a lethal hit is included in stack/cash-out rewards and
        // HealDurability can prevent destruction when it restores enough durability.
        BallRuntimeStatus runtimeStatus = ballDataManager.GetComponent<BallRuntimeStatus>();
        if (runtimeStatus != null)
        {
            runtimeStatus.ApplyObjectHitDurabilityDamage();
        }
    }
}
