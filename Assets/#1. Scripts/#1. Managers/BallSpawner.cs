using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    public event System.Action CylinderDrawn;
    public event System.Action<int> BallFired;

    [Header("References")]
    [SerializeField] GameObject ballPrefab;
    [SerializeField] PlayerBallDeck deck;
    [SerializeField] StateManager stateManager;

    [Header("Fire Settings")]
    [SerializeField] int drawCount = 3;
    [SerializeField] float fireInterval = 0.25f;

    Camera mainCamera;
    bool isFiring;

    void Awake()
    {
        mainCamera = Camera.main;
        FindMissingReferences();
    }

    void Start()
    {
        // BallSpawner는 전투 상태를 직접 시작하지 않습니다.
        // 전투 시작, 턴 시작, currentCylinder 뽑기는 StateManager가 담당합니다.
        Debug.Log("[BallSpawner] 준비 완료. 발사 입력만 처리하고 전투 흐름은 StateManager에 알립니다.", this);
    }

    void Update()
    {
        // 마우스 왼쪽 버튼을 누르면 StateManager가 허락한 상태에서만 currentCylinder를 발사합니다.
        if (Input.GetMouseButtonDown(0))
        {
            TryStartFireHand();
        }
    }

    public void DrawNextCylinder()
    {
        FindMissingReferences();

        if (deck == null)
        {
            Debug.LogWarning("[BallSpawner] PlayerBallDeck 참조가 없어 다음 currentCylinder를 뽑을 수 없습니다.", this);
            return;
        }

        deck.DrawBalls(drawCount);
        CylinderDrawn?.Invoke();
        Debug.Log($"[BallSpawner] StateManager 요청으로 currentCylinder를 뽑았습니다. drawCount: {drawCount}", this);
    }

    void TryStartFireHand()
    {
        if (isFiring)
        {
            Debug.Log("[BallSpawner] 이미 발사 중이므로 입력을 무시합니다.", this);
            return;
        }

        FindMissingReferences();

        if (stateManager == null)
        {
            Debug.LogWarning("[BallSpawner] StateManager 참조가 없어 발사 가능 상태를 확인할 수 없습니다.", this);
            return;
        }

        if (!stateManager.CanPlayerFire())
        {
            Debug.Log($"[BallSpawner] 현재 전투 상태에서는 발사할 수 없습니다. State: {stateManager.CurrentState}", this);
            return;
        }

        if (!ValidateFireReady())
        {
            return;
        }

        StartCoroutine(FireCurrentHandRoutine());
    }

    IEnumerator FireCurrentHandRoutine()
    {
        isFiring = true;
        stateManager.OnPlayerFireStarted();

        // 발사 중 currentCylinder가 바뀌지 않도록 스냅샷을 만들어 순차 발사합니다.
        List<BallDataSO> cylinderSnapshot = new List<BallDataSO>(deck.CurrentCylinder);
        Vector2 firePosition = GetMouseWorldPosition();

        Debug.Log($"[BallSpawner] currentCylinder 발사 시작. count: {cylinderSnapshot.Count}, firePoint: {firePosition}", this);

        for (int i = 0; i < cylinderSnapshot.Count; i++)
        {
            BallDataSO ballData = cylinderSnapshot[i];
            SpawnAndLaunchBall(ballData, firePosition);
            BallFired?.Invoke(i);

            if (i < cylinderSnapshot.Count - 1 && fireInterval > 0f)
            {
                yield return new WaitForSeconds(fireInterval);
            }
        }

        // 모든 탄환이 실제로 생성/발사된 뒤에만 currentCylinder를 discardPile로 이동합니다.
        deck.DiscardCurrentHand();

        isFiring = false;
        Debug.Log("[BallSpawner] currentCylinder의 모든 탄환 발사 완료. StateManager에게 알립니다.", this);

        stateManager.OnBallFireSequenceFinished();
    }

    void SpawnAndLaunchBall(BallDataSO ballData, Vector2 firePosition)
    {
        if (ballData == null)
        {
            Debug.LogWarning("[BallSpawner] BallDataSO가 null이라 탄환 생성을 건너뜁니다.", this);
            return;
        }

        GameObject spawnedBall;
        BallEffectController.SuppressSpawnEffectsOnEnable = true;
        try
        {
            spawnedBall = Instantiate(ballPrefab, firePosition, Quaternion.identity);
        }
        finally
        {
            BallEffectController.SuppressSpawnEffectsOnEnable = false;
        }

        spawnedBall.name = $"{ballData.name} Ball";

        BallDataManager dataManager = spawnedBall.GetComponent<BallDataManager>();
        if (dataManager != null)
        {
            dataManager.SetBallData(ballData);
        }
        else
        {
            Debug.LogWarning("[BallSpawner] ballPrefab에 BallDataManager가 없어 BallDataSO를 주입할 수 없습니다.", spawnedBall);
        }

        ApplyBallVisual(spawnedBall, ballData);

        BallRuntimeStatus runtimeStatus = spawnedBall.GetComponent<BallRuntimeStatus>();
        if (runtimeStatus != null)
        {
            runtimeStatus.Initialize();
        }
        else
        {
            Debug.LogWarning("[BallSpawner] ballPrefab에 BallRuntimeStatus가 없습니다.", spawnedBall);
        }

        BallEffectController effectController = spawnedBall.GetComponent<BallEffectController>();
        if (effectController != null)
        {
            effectController.Initialize();
            effectController.TriggerSpawnEffects();
        }
        else
        {
            Debug.LogWarning("[BallSpawner] ballPrefab에 BallEffectController가 없습니다.", spawnedBall);
        }

        Vector2 direction = GetRandomLaunchDirection();
        float launchSpeed = Mathf.Max(0f, ballData.LaunchSpeed);

        Ball ball = spawnedBall.GetComponent<Ball>();
        if (ball != null)
        {
            ball.Launch(direction, launchSpeed);
        }
        else
        {
            Rigidbody2D rigidbody2D = spawnedBall.GetComponent<Rigidbody2D>();
            if (rigidbody2D != null)
            {
                rigidbody2D.linearVelocity = direction * launchSpeed;
            }
            else
            {
                Debug.LogWarning("[BallSpawner] ballPrefab에 Ball 또는 Rigidbody2D가 없어 속도를 설정할 수 없습니다.", spawnedBall);
            }
        }

        Debug.Log($"[BallSpawner] 탄환 발사: {ballData.name}, speed: {launchSpeed}, direction: {direction}", spawnedBall);
    }

    void ApplyBallVisual(GameObject spawnedBall, BallDataSO ballData)
    {
        // SpriteRenderer가 루트가 아닌 자식에 붙어 있을 수도 있으므로 자식까지 검색합니다.
        SpriteRenderer spriteRenderer = spawnedBall.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("[BallSpawner] 생성된 탄환에서 SpriteRenderer를 찾을 수 없어 색상/스프라이트를 적용하지 못했습니다.", spawnedBall);
            return;
        }

        spriteRenderer.color = ballData.BallColor;

        if (ballData.BallSprite != null)
        {
            spriteRenderer.sprite = ballData.BallSprite;
            Debug.Log($"[BallSpawner] 탄환 비주얼 적용: color {ballData.BallColor}, sprite {ballData.BallSprite.name}", spawnedBall);
            return;
        }

        Debug.Log($"[BallSpawner] 탄환 색상만 적용: color {ballData.BallColor}. 스프라이트는 프리팹 기본값을 사용합니다.", spawnedBall);
    }

    bool ValidateFireReady()
    {
        if (ballPrefab == null)
        {
            Debug.LogWarning("[BallSpawner] ballPrefab이 비어 있습니다. Inspector에서 공 프리팹을 연결해 주세요.", this);
            return false;
        }

        if (deck == null)
        {
            Debug.LogWarning("[BallSpawner] PlayerBallDeck 참조가 없습니다.", this);
            return false;
        }

        if (deck.CurrentCylinder == null || deck.CurrentCylinder.Count == 0)
        {
            Debug.LogWarning("[BallSpawner] currentCylinder가 비어 있어 발사할 탄환이 없습니다.", this);
            return false;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            Debug.LogWarning("[BallSpawner] Camera.main을 찾을 수 없어 마우스 위치를 월드 좌표로 변환할 수 없습니다.", this);
            return false;
        }

        return true;
    }

    Vector2 GetMouseWorldPosition()
    {
        // firePoint를 별도 Transform으로 두지 않고 클릭한 마우스 위치를 월드 좌표로 변환해 사용합니다.
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = Mathf.Abs(mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(mouseScreenPosition);
    }

    Vector2 GetRandomLaunchDirection()
    {
        // 탄환 발사 방향은 임시 랜덤입니다. 0에 가까우면 안전한 기본 방향을 사용합니다.
        Vector2 direction = Random.insideUnitCircle;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector2.right;
        }

        return direction.normalized;
    }

    void FindMissingReferences()
    {
        if (deck == null)
        {
            deck = GetComponent<PlayerBallDeck>();
        }

        if (stateManager == null)
        {
            stateManager = FindFirstObjectByType<StateManager>();
        }
    }
}
