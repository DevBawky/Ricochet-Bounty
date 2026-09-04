using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("발사")]
    [SerializeField] float _launchForce = 10f;

    [Header("속도")]
    [SerializeField, Tooltip("공이 발사된 뒤 유지할 최소 속도입니다.")]
    float _minMoveSpeed = 6f;
    [SerializeField] float _maxMoveSpeed = 24f;
    [SerializeField] bool _logMinimumSpeedCorrection;

    [Header("끼임 복구")]
    [SerializeField, Min(0.1f), Tooltip("공이 끼었는지 실제 이동 거리를 확인하는 주기입니다.")]
    float _stuckCheckInterval = 0.5f;
    [SerializeField, Min(0.01f), Tooltip("확인 주기 동안 이 거리보다 적게 이동하면 공이 끼인 것으로 판단합니다.")]
    float _stuckMinimumTravelDistance = 0.2f;
    [SerializeField, Min(0f), Tooltip("끼인 공을 다시 발사하기 전에 위치를 살짝 이동할 거리입니다.")]
    float _stuckEscapeDistance = 0.15f;

    [Header("무충돌 파괴")]
    [SerializeField] float _noCollisionDestroyDelay = 5f;

    Rigidbody2D _rigidbody;
    BallDataManager _ballDataManager;
    BallRuntimeStatus _runtimeStatus;
    BallEffectController _effectController;
    BallRegistry _ballRegistry;
    Vector2 _lastVelocity;
    Vector2 _lastMoveDirection = Vector2.right;
    float _lastWallHitSystemTime = -999f;
    float _lastCollisionTime;
    int _collisionCount;
    bool _hasExternalLaunch;
    bool _isDestroyingByNoCollision;
    Vector2 _lastPhysicsPosition;
    float _travelDistanceSinceStuckCheck;
    float _nextStuckCheckTime;
    float _waveElapsedTime;
    Vector2 _waveForwardDirection = Vector2.right;
    Vector2 _lastAppliedWaveVelocity;
    bool _hasAppliedWaveVelocity;

    public float MinimumMoveSpeed
    {
        get
        {
            return Mathf.Max(0f, _minMoveSpeed);
        }
    }

    void Awake()
    {
        CacheComponents();
        ConfigureRigidbody();
    }

    void OnEnable()
    {
        // 풀에서 다시 활성화되거나 실행 중인 공을 복제해도 이전 발사/파동 상태를 이어받지 않습니다.
        _hasExternalLaunch = false;
        ResetWaveMovement(_lastMoveDirection);
        RegisterWithBallRegistry();
    }

    void OnDisable()
    {
        if (_ballRegistry != null)
        {
            _ballRegistry.Unregister(this);
        }

        ResetWaveMovement(_lastMoveDirection);
    }

    public void PrepareForPoolSpawn()
    {
        CacheComponents();
        ConfigureRigidbody();
        EnableColliders();

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
        }

        _lastVelocity = Vector2.zero;
        _lastMoveDirection = Vector2.right;
        _lastWallHitSystemTime = -999f;
        _collisionCount = 0;
        _hasExternalLaunch = false;
        _isDestroyingByNoCollision = false;
        ResetWaveMovement(_lastMoveDirection);
        ResetNoCollisionDestroyTimer();
        ResetStuckCheck();
    }

    public void PrepareForPoolRelease()
    {
        CacheComponents();
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.Sleep();
            _rigidbody.simulated = false;
        }

        _lastVelocity = Vector2.zero;
        _hasExternalLaunch = false;
        _isDestroyingByNoCollision = false;
        ResetWaveMovement(Vector2.right);
    }

    void Start()
    {
        RegisterWithBallRegistry();
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

        _launchForce = Mathf.Clamp(Mathf.Max(0f, speed), _minMoveSpeed, _maxMoveSpeed);
        _lastMoveDirection = direction.normalized;
        ResetWaveMovement(_lastMoveDirection);
        _rigidbody.linearVelocity = _lastMoveDirection * _launchForce;
        _hasExternalLaunch = true;
        _rigidbody.WakeUp();
        ResetStuckCheck();

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
        float guaranteedSpeed = Mathf.Max(speed, _minMoveSpeed);
        Launch(_lastMoveDirection, guaranteedSpeed);

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
        if (IsWaveMovementEnabled())
        {
            SynchronizeWaveDirectionWithPhysics(_lastVelocity);
        }
        else
        {
            RememberMoveDirection(_lastVelocity);
        }

        KeepSpeedInRange();
        CheckAndRecoverFromStuck();
        ApplyWaveMovement();
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

        _launchForce = Mathf.Clamp(_ballDataManager.BallData.LaunchSpeed, _minMoveSpeed, _maxMoveSpeed);
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
        ApplyWallHitSystems(GetCollisionEffectPosition(collision));
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

        BallPoolHandle poolHandle = GetComponent<BallPoolHandle>();
        if (poolHandle != null && poolHandle.Release())
        {
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

    void ApplyWallHitSystems(Vector3 hitPosition)
    {
        if (Mathf.Approximately(_lastWallHitSystemTime, Time.fixedTime))
        {
            return;
        }

        _lastWallHitSystemTime = Time.fixedTime;

        if (_ballDataManager != null)
        {
            _ballDataManager.SpawnCollisionEffect(hitPosition);
        }

        if (_effectController != null)
        {
            _effectController.TriggerWallHitEffects(hitPosition);
        }

        // Resolve hit effects before durability can destroy the ball. This lets the final
        // hit contribute to stack/cash-out effects and allows durability healing to matter.
        if (_runtimeStatus != null)
        {
            _runtimeStatus.ApplyWallHitDurabilityDamage();
        }
    }

    Vector3 GetCollisionEffectPosition(Collision2D collision)
    {
        Vector3 hitPosition = transform.position;
        if (collision != null && collision.contactCount > 0)
        {
            hitPosition = collision.GetContact(0).point;
            hitPosition.z = transform.position.z;
        }

        return hitPosition;
    }

    void RegisterWithBallRegistry()
    {
        if (_ballRegistry == null)
        {
            _ballRegistry = FindFirstObjectByType<BallRegistry>();
        }

        if (_ballRegistry != null)
        {
            _ballRegistry.Register(this);
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

    void CheckAndRecoverFromStuck()
    {
        Vector2 currentPosition = _rigidbody.position;
        _travelDistanceSinceStuckCheck += Vector2.Distance(currentPosition, _lastPhysicsPosition);
        _lastPhysicsPosition = currentPosition;

        if (Time.fixedTime < _nextStuckCheckTime)
        {
            return;
        }

        bool isStuck = _travelDistanceSinceStuckCheck < _stuckMinimumTravelDistance;
        _travelDistanceSinceStuckCheck = 0f;
        _nextStuckCheckTime = Time.fixedTime + _stuckCheckInterval;

        if (!isStuck)
        {
            return;
        }

        Vector2 escapeDirection = GetRandomDirection();
        if (_stuckEscapeDistance > 0f)
        {
            _rigidbody.position += escapeDirection * _stuckEscapeDistance;
            _lastPhysicsPosition = _rigidbody.position;
        }

        SetVelocity(escapeDirection * Mathf.Max(_minMoveSpeed, _launchForce));
        _rigidbody.WakeUp();

        Debug.Log($"[Ball] {name} was stuck and has been relaunched. Direction: {escapeDirection}, Speed: {_rigidbody.linearVelocity.magnitude}", this);
    }

    void ResetStuckCheck()
    {
        if (_rigidbody == null)
        {
            return;
        }

        _lastPhysicsPosition = _rigidbody.position;
        _travelDistanceSinceStuckCheck = 0f;
        _nextStuckCheckTime = Time.fixedTime + _stuckCheckInterval;
    }

    void SetVelocity(Vector2 velocity)
    {
        Vector2 direction = IsSafeDirection(velocity) ? velocity.normalized : _lastMoveDirection;
        float speed = Mathf.Clamp(velocity.magnitude, _minMoveSpeed, _maxMoveSpeed);

        _rigidbody.linearVelocity = direction * speed;
        _lastMoveDirection = direction;

        if (IsWaveMovementEnabled())
        {
            // 벽 반사, Minimum Speed 보정, 끼임 탈출 결과를 다음 파동의 새 전진 방향으로 사용합니다.
            _waveForwardDirection = direction;
            _hasAppliedWaveVelocity = false;
        }
    }

    void ApplyWaveMovement()
    {
        if (!IsWaveMovementEnabled())
        {
            return;
        }

        BallDataSO ballData = _ballDataManager.BallData;
        float amplitude = ballData.WaveAmplitude;
        float frequency = ballData.WaveFrequency;
        if (amplitude <= 0f || frequency <= 0f)
        {
            _hasAppliedWaveVelocity = false;
            return;
        }

        Vector2 currentVelocity = _rigidbody.linearVelocity;
        if (!IsSafeDirection(currentVelocity))
        {
            return;
        }

        if (!IsSafeDirection(_waveForwardDirection))
        {
            _waveForwardDirection = currentVelocity.normalized;
        }

        _waveElapsedTime += Time.fixedDeltaTime;
        float phase = _waveElapsedTime * frequency * Mathf.PI * 2f;
        Vector2 perpendicular = new Vector2(-_waveForwardDirection.y, _waveForwardDirection.x);
        float lateralAmount = Mathf.Sin(phase) * amplitude;
        Vector2 waveDirection = (_waveForwardDirection + perpendicular * lateralAmount).normalized;
        float speed = Mathf.Clamp(currentVelocity.magnitude, _minMoveSpeed, _maxMoveSpeed);
        Vector2 waveVelocity = waveDirection * speed;

        _rigidbody.linearVelocity = waveVelocity;
        _lastVelocity = waveVelocity;
        _lastMoveDirection = waveDirection;
        _lastAppliedWaveVelocity = waveVelocity;
        _hasAppliedWaveVelocity = true;
    }

    void SynchronizeWaveDirectionWithPhysics(Vector2 currentVelocity)
    {
        if (!IsSafeDirection(currentVelocity))
        {
            return;
        }

        if (!_hasAppliedWaveVelocity)
        {
            _waveForwardDirection = currentVelocity.normalized;
            return;
        }

        Vector2 appliedDirection = _lastAppliedWaveVelocity.normalized;
        Vector2 physicsDirection = currentVelocity.normalized;
        if (Vector2.Dot(appliedDirection, physicsDirection) >= 0.9999f)
        {
            return;
        }

        // Physics2D 또는 충돌 오브젝트가 바꾼 실제 속도 방향을 보존합니다.
        _waveForwardDirection = physicsDirection;
        _hasAppliedWaveVelocity = false;
    }

    void ResetWaveMovement(Vector2 direction)
    {
        _waveElapsedTime = 0f;
        _waveForwardDirection = IsSafeDirection(direction) ? direction.normalized : Vector2.right;
        _lastAppliedWaveVelocity = Vector2.zero;
        _hasAppliedWaveVelocity = false;
    }

    bool IsWaveMovementEnabled()
    {
        return _ballDataManager != null
            && _ballDataManager.BallData != null
            && _ballDataManager.BallData.MovementType == BallMovementType.Wave;
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
