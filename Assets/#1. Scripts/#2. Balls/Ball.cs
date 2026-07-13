using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Launch")]
    [SerializeField] float _launchForce = 10f;

    [Header("Speed")]
    [SerializeField, Tooltip("Minimum speed kept after the ball has been launched.")]
    float _minMoveSpeed = 6f;
    [SerializeField] float _maxMoveSpeed = 24f;
    [SerializeField] bool _logMinimumSpeedCorrection;

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
        CacheComponents();
        ConfigureRigidbody();
    }

    void Start()
    {
        // 생성 직후에는 아직 충돌이 없을 수 있으므로, 생성 시점부터 무충돌 파괴 타이머를 시작합니다.
        ResetNoCollisionDestroyTimer();
        ApplyBallDataLaunchSpeed();

        // BallSpawner나 SplitBall 초기화가 이미 속도를 넣은 공은 여기서 다시 랜덤 발사하지 않습니다.
        if (!_hasExternalLaunch)
        {
            LaunchRandomDirection();
        }
    }

    public void Launch(Vector2 direction, float speed)
    {
        CacheComponents();

        if (_rigidbody == null)
        {
            Debug.LogWarning($"[Ball] {name} Rigidbody2D가 없어 발사할 수 없습니다.", this);
            return;
        }

        if (!IsSafeDirection(direction))
        {
            Debug.LogWarning($"[Ball] {name} 발사 방향이 유효하지 않아 오른쪽 방향으로 보정합니다.", this);
            direction = Vector2.right;
        }

        _launchForce = Mathf.Max(0f, speed);
        _minMoveSpeed = Mathf.Min(_minMoveSpeed, _launchForce);
        _lastMoveDirection = direction.normalized;
        _rigidbody.linearVelocity = _lastMoveDirection * _launchForce;
        _hasExternalLaunch = true;
        _rigidbody.WakeUp();

        Debug.Log($"[Ball] {name} 발사 완료. direction: {_lastMoveDirection}, speed: {_launchForce}", this);
    }

    public void ResetNoCollisionDestroyTimer()
    {
        // SplitBall은 원본 공을 복제하므로 원본의 무충돌 파괴 타이머 상태가 같이 복사될 수 있습니다.
        // 새로 태어난 분열 공은 생성 시점부터 No Collision Destroy Delay를 다시 세도록 초기화합니다.
        _lastCollisionTime = Time.time;
        _isDestroyingByNoCollision = false;

        Debug.Log($"[Ball] {name} No Collision Destroy 타이머 초기화. delay: {_noCollisionDestroyDelay}", this);
    }

    public void InitializeAsSplitBall(Vector2 direction, float speed)
    {
        // SplitBall은 원본 공을 통째로 복제하기 때문에 private 런타임 값도 함께 복사됩니다.
        // 분열 공은 새로 발사된 공처럼 물리/타이머/이동 상태를 명시적으로 다시 잡아줍니다.
        CacheComponents();
        ConfigureRigidbody();
        EnableColliders();

        _lastVelocity = Vector2.zero;
        _lastMoveDirection = IsSafeDirection(direction) ? direction.normalized : Vector2.right;
        _lastWallHitSystemTime = -999f;
        _collisionCount = 0;
        _hasExternalLaunch = true;

        ResetNoCollisionDestroyTimer();
        Launch(_lastMoveDirection, speed);

        Debug.Log($"[Ball] {name} 분열 공 초기화 완료. direction: {_lastMoveDirection}, speed: {speed}", this);
    }

    void Update()
    {
        CheckNoCollisionDestroyTimeout();
    }

    void FixedUpdate()
    {
        if (!CanCorrectSpeed())
        {
            return;
        }

        _lastVelocity = _rigidbody.linearVelocity;
        RememberMoveDirection(_lastVelocity);
        KeepSpeedInRange();
    }

    void LaunchRandomDirection()
    {
        Vector2 direction = GetRandomDirection();
        _lastMoveDirection = direction;
        Launch(direction, _launchForce);
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
        // 어떤 Collider2D와 충돌해도 무충돌 파괴 타이머를 갱신합니다.
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
        // 벽이나 오브젝트와 계속 닿아 있는 상태도 충돌 감지 상태로 처리합니다.
        RefreshCollisionTimer();

        if (!collision.gameObject.CompareTag("Wall"))
        {
            return;
        }

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
        if (!CanCorrectSpeed())
        {
            return;
        }

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
        if (!IsSafeDirection(direction))
        {
            direction = Vector2.right;
        }

        SetVelocity(direction * _minMoveSpeed);

        if (_logMinimumSpeedCorrection)
        {
            Debug.Log($"[Ball] {name} speed corrected to minimum speed. Previous Speed: {speed}, Minimum Speed: {_minMoveSpeed}, Direction: {direction}", this);
        }
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

        if (_rigidbody != null && IsSafeDirection(_rigidbody.linearVelocity))
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

    void CacheComponents()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        if (_ballDataManager == null)
        {
            _ballDataManager = GetComponent<BallDataManager>();
        }

        if (_runtimeStatus == null)
        {
            _runtimeStatus = GetComponent<BallRuntimeStatus>();
        }

        if (_effectController == null)
        {
            _effectController = GetComponent<BallEffectController>();
        }
    }

    void ConfigureRigidbody()
    {
        if (_rigidbody == null)
        {
            return;
        }

        _rigidbody.simulated = true;
        _rigidbody.bodyType = RigidbodyType2D.Dynamic;
        _rigidbody.freezeRotation = true;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rigidbody.angularVelocity = 0f;
        _rigidbody.WakeUp();
    }

    void EnableColliders()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = true;
        }
    }

    bool IsSafeDirection(Vector2 direction)
    {
        if (float.IsNaN(direction.x) || float.IsNaN(direction.y))
        {
            return false;
        }

        return direction.sqrMagnitude > 0.0001f;
    }

    bool CanCorrectSpeed()
    {
        if (_rigidbody == null)
        {
            return false;
        }

        if (!isActiveAndEnabled || !_rigidbody.simulated)
        {
            return false;
        }

        if (!_hasExternalLaunch)
        {
            return false;
        }

        if (_isDestroyingByNoCollision)
        {
            return false;
        }

        if (_runtimeStatus != null && _runtimeStatus.IsDestroying)
        {
            return false;
        }

        return _minMoveSpeed > 0f;
    }
}
