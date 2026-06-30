using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] float _launchForce = 10f;
    [SerializeField] float _pushOutDistance = 0.01f;

    Rigidbody2D _rigidbody;
    SpriteRenderer _spriteRenderer;
    BallDataManager _ballDataManager;
    BallRuntimeStatus _runtimeStatus;
    BallEffectController _effectController;
    Vector2 _lastVelocity;
    int _collisionCount;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _ballDataManager = GetComponent<BallDataManager>();
        _runtimeStatus = GetComponent<BallRuntimeStatus>();
        _effectController = GetComponent<BallEffectController>();

        _rigidbody.freezeRotation = true;
    }

    void Start()
    {
        ApplyBallDataLaunchSpeed();
        LaunchRandomDirection();
    }

    void FixedUpdate()
    {
        _lastVelocity = _rigidbody.linearVelocity;
    }

    void LaunchRandomDirection()
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        _rigidbody.AddForce(direction * _launchForce, ForceMode2D.Impulse);
    }

    void ApplyBallDataLaunchSpeed()
    {
        // BallDataSO에 기본 발사 속도가 설정되어 있으면 그 값을 사용합니다.
        // BallDataSO가 비어 있으면 Inspector의 _launchForce 값을 그대로 사용합니다.
        if (_ballDataManager == null || _ballDataManager.BallData == null)
        {
            return;
        }

        if (_ballDataManager.BallData.LaunchSpeed <= 0f)
        {
            return;
        }

        _launchForce = _ballDataManager.BallData.LaunchSpeed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Wall"))
        {
            return;
        }

        _collisionCount++;
        ApplyWallHitSystems();


        if (_lastVelocity == Vector2.zero)
        {
            return;
        }

        Vector2 bestNormal = FindBestWallNormal(collision);
        float movingIntoWallAmount = Vector2.Dot(_lastVelocity, bestNormal);

        if (movingIntoWallAmount >= 0f)
        {
            return;
        }

        Vector2 reflectedVelocity = Vector2.Reflect(_lastVelocity, bestNormal);
        _rigidbody.linearVelocity = reflectedVelocity;

        Vector2 pushedPosition = _rigidbody.position + bestNormal * _pushOutDistance;
        _rigidbody.position = pushedPosition;
    }

    void ApplyWallHitSystems()
    {
        // 벽 충돌 시 내구도 감소와 벽 충돌 효과를 실행합니다.
        // 실제 벽 충돌 감지는 기존 OnCollisionEnter2D를 그대로 사용합니다.
        if (_runtimeStatus != null)
        {
            _runtimeStatus.ApplyWallHitDurabilityDamage();
        }

        if (_effectController != null)
        {
            _effectController.TriggerWallHitEffects();
        }
    }

    Vector2 FindBestWallNormal(Collision2D collision)
    {
        Vector2 moveDirection = _lastVelocity.normalized;
        Vector2 bestNormal = collision.GetContact(0).normal;
        float bestDot = Vector2.Dot(moveDirection, bestNormal);

        for (int i = 1; i < collision.contactCount; i++)
        {
            Vector2 normal = collision.GetContact(i).normal;
            float dot = Vector2.Dot(moveDirection, normal);

            if (dot < bestDot)
            {
                bestDot = dot;
                bestNormal = normal;
            }
        }

        return bestNormal;
    }
}
