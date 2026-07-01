using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Launch")]
    [SerializeField] float _launchForce = 10f;

    [Header("Speed")]
    [SerializeField] float _minMoveSpeed = 6f;
    [SerializeField] float _maxMoveSpeed = 24f;

    [Header("No Collision Destroy")]
    [SerializeField] float _noCollisionDestroyDelay = 5f;

    Rigidbody2D _rigidbody;
    BallDataManager _ballDataManager;
    BallRuntimeStatus _runtimeStatus;
    BallEffectController _effectController;
    Vector2 _lastVelocity;
    Vector2 _lastMoveDirection = Vector2.right;
    float _lastWallHitSystemTime = -999f;
    float _lastCollisionTime;
    int _collisionCount;
    bool _hasExternalLaunch;
    bool _isDestroyingByNoCollision;

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
        // 생성 직후에는 아직 충돌이 없을 수 있으므로, 발사 시점을 기준으로 무충돌 타이머를 시작합니다.
        _lastCollisionTime = Time.time;

        ApplyBallDataLaunchSpeed();

        // BallSpawner가 이미 속도를 지정한 공은 여기서 다시 랜덤 발사하지 않습니다.
        if (!_hasExternalLaunch)
        {
            LaunchRandomDirection();
        }
    }

    public void Launch(Vector2 direction, float speed)
    {
        if (!IsSafeDirection(direction))
        {
            Debug.LogWarning($"[Ball] {name}의 발사 방향이 유효하지 않아 오른쪽 방향으로 보정합니다.", this);
            direction = Vector2.right;
        }

        _launchForce = Mathf.Max(0f, speed);
        _minMoveSpeed = Mathf.Min(_minMoveSpeed, _launchForce);
        _lastMoveDirection = direction.normalized;
        _rigidbody.linearVelocity = _lastMoveDirection * _launchForce;
        _hasExternalLaunch = true;

        Debug.Log($"[Ball] {name} 발사 완료. direction: {_lastMoveDirection}, speed: {_launchForce}", this);
    }

    void Update()
    {
        CheckNoCollisionDestroyTimeout();
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
        // 어떤 Collider2D와 충돌하든 "충돌이 감지됨"으로 보고 무충돌 파괴 타이머를 갱신합니다.
        RefreshCollisionTimer();

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
        // 벽이나 오브젝트에 계속 닿아 있는 상태도 충돌 감지 상태로 처리합니다.
        RefreshCollisionTimer();

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

    void OnTriggerEnter2D(Collider2D other)
    {
        // Trigger 방식 점수 오브젝트도 충돌 감지로 인정해서 공이 불필요하게 사라지지 않게 합니다.
        RefreshCollisionTimer();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        RefreshCollisionTimer();
    }

    void RefreshCollisionTimer()
    {
        _lastCollisionTime = Time.time;
    }

    void CheckNoCollisionDestroyTimeout()
    {
        if (_isDestroyingByNoCollision)
        {
            return;
        }

        if (_noCollisionDestroyDelay <= 0f)
        {
            return;
        }

        if (Time.time - _lastCollisionTime < _noCollisionDestroyDelay)
        {
            return;
        }

        _isDestroyingByNoCollision = true;
        Debug.Log($"[Ball] {name}이 {_noCollisionDestroyDelay}초 동안 충돌하지 않아 파괴됩니다.", this);

        if (_runtimeStatus != null)
        {
            _runtimeStatus.DestroyBall();
            return;
        }

        Destroy(gameObject);
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
