using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Launch")]
    [SerializeField] float _launchForce = 10f;

    [Header("Speed")]
    [SerializeField] float _minMoveSpeed = 6f;
    [SerializeField] float _maxMoveSpeed = 24f;

    Rigidbody2D _rigidbody;
    BallDataManager _ballDataManager;
    BallRuntimeStatus _runtimeStatus;
    BallEffectController _effectController;
    Vector2 _lastVelocity;
    Vector2 _lastMoveDirection = Vector2.right;
    float _lastWallHitSystemTime = -999f;
    int _collisionCount;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _ballDataManager = GetComponent<BallDataManager>();
        _runtimeStatus = GetComponent<BallRuntimeStatus>();
        _effectController = GetComponent<BallEffectController>();

        _rigidbody.freezeRotation = true;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Start()
    {
        ApplyBallDataLaunchSpeed();
        LaunchRandomDirection();
    }

    void FixedUpdate()
    {
        _lastVelocity = _rigidbody.linearVelocity;
        RememberMoveDirection(_lastVelocity);
        KeepSpeedInRange();
    }

    void LaunchRandomDirection()
    {
        Vector2 direction = GetRandomDirection();
        _lastMoveDirection = direction;
        _rigidbody.linearVelocity = direction * _launchForce;
    }

    void ApplyBallDataLaunchSpeed()
    {
        if (_ballDataManager == null || _ballDataManager.BallData == null)
        {
            return;
        }

        if (_ballDataManager.BallData.LaunchSpeed <= 0f)
        {
            return;
        }

        _launchForce = _ballDataManager.BallData.LaunchSpeed;
        _minMoveSpeed = Mathf.Min(_minMoveSpeed, _launchForce);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Wall"))
        {
            return;
        }

        _collisionCount++;
        ApplyWallHitSystems();
        BounceFromWall(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Wall"))
        {
            return;
        }

        // 벽에 붙어 있을 때는 위치를 직접 밀지 않습니다.
        // Rigidbody2D.position을 수동으로 바꾸면 TilemapCollider 반대편으로 넘어가는 원인이 될 수 있습니다.
        Vector2 normal = FindBestWallNormal(collision);
        Vector2 velocity = GetSafeVelocity();

        if (Vector2.Dot(velocity, normal) < -0.01f)
        {
            SetVelocity(Vector2.Reflect(velocity, normal));
            return;
        }

        KeepSpeedInRange();
    }

    void BounceFromWall(Collision2D collision)
    {
        Vector2 velocity = GetSafeVelocity();
        Vector2 normal = FindBestWallNormal(collision);

        if (Vector2.Dot(velocity, normal) >= 0f)
        {
            KeepSpeedInRange();
            return;
        }

        SetVelocity(Vector2.Reflect(velocity, normal));
    }

    void ApplyWallHitSystems()
    {
        if (Mathf.Approximately(_lastWallHitSystemTime, Time.fixedTime))
        {
            return;
        }

        _lastWallHitSystemTime = Time.fixedTime;

        if (_runtimeStatus != null)
        {
            _runtimeStatus.ApplyWallHitDurabilityDamage();
        }

        if (_effectController != null)
        {
            _effectController.TriggerWallHitEffects();
        }
    }

    void KeepSpeedInRange()
    {
        Vector2 velocity = _rigidbody.linearVelocity;
        float speed = velocity.magnitude;

        if (speed > _maxMoveSpeed)
        {
            SetVelocity(velocity.normalized * _maxMoveSpeed);
            return;
        }

        if (speed >= _minMoveSpeed)
        {
            return;
        }

        Vector2 direction = IsSafeDirection(velocity) ? velocity.normalized : _lastMoveDirection;
        SetVelocity(direction * _minMoveSpeed);
    }

    void SetVelocity(Vector2 velocity)
    {
        Vector2 direction = IsSafeDirection(velocity) ? velocity.normalized : _lastMoveDirection;
        float speed = Mathf.Clamp(velocity.magnitude, _minMoveSpeed, _maxMoveSpeed);

        _rigidbody.linearVelocity = direction * speed;
        _lastMoveDirection = direction;
    }

    Vector2 GetSafeVelocity()
    {
        if (IsSafeDirection(_lastVelocity))
        {
            return _lastVelocity;
        }

        if (IsSafeDirection(_rigidbody.linearVelocity))
        {
            return _rigidbody.linearVelocity;
        }

        return _lastMoveDirection * _minMoveSpeed;
    }

    Vector2 FindBestWallNormal(Collision2D collision)
    {
        Vector2 moveDirection = GetSafeVelocity().normalized;
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

    void RememberMoveDirection(Vector2 velocity)
    {
        if (!IsSafeDirection(velocity))
        {
            return;
        }

        _lastMoveDirection = velocity.normalized;
    }

    Vector2 GetRandomDirection()
    {
        Vector2 direction = Random.insideUnitCircle;
        if (IsSafeDirection(direction))
        {
            return direction.normalized;
        }

        return Vector2.right;
    }

    bool IsSafeDirection(Vector2 direction)
    {
        if (float.IsNaN(direction.x) || float.IsNaN(direction.y))
        {
            return false;
        }

        return direction.sqrMagnitude > 0.0001f;
    }
}
