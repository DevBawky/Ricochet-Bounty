using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject ballPrefab;
    [SerializeField] PlayerBallDeck deck;

    [Header("Fire Settings")]
    [SerializeField] int drawCount = 3;
    [SerializeField] float fireInterval = 0.25f;

    Camera _mainCamera;
    bool _isFiring;

    void Awake()
    {
        _mainCamera = Camera.main;

        if (deck == null)
        {
            deck = GetComponent<PlayerBallDeck>();
        }
    }

    void Start()
    {
        if (deck == null)
        {
            Debug.LogWarning("[BallSpawner] PlayerBallDeck 참조가 없습니다. 덱에서 탄환을 뽑을 수 없습니다.", this);
            return;
        }

        deck.DrawBalls(drawCount);
        Debug.Log($"[BallSpawner] 전투 시작 손패 뽑기 완료. drawCount: {drawCount}", this);
    }

    void Update()
    {
        // 마우스 왼쪽 버튼을 누르면 현재 손패의 탄환을 순서대로 발사합니다.
        if (Input.GetMouseButtonDown(0))
        {
            TryStartFireHand();
        }
    }

    void TryStartFireHand()
    {
        if (_isFiring)
        {
            Debug.Log("[BallSpawner] 이미 발사 중이므로 입력을 무시합니다.", this);
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
        _isFiring = true;

        // 발사 중 손패 리스트가 바뀌지 않도록 스냅샷을 떠서 사용합니다.
        List<BallDataSO> handSnapshot = new List<BallDataSO>(deck.CurrentHand);
        Vector2 firePosition = GetMouseWorldPosition();

        Debug.Log($"[BallSpawner] 손패 발사 시작. count: {handSnapshot.Count}, firePoint: {firePosition}", this);

        for (int i = 0; i < handSnapshot.Count; i++)
        {
            BallDataSO ballData = handSnapshot[i];
            SpawnAndLaunchBall(ballData, firePosition);

            if (i < handSnapshot.Count - 1 && fireInterval > 0f)
            {
                yield return new WaitForSeconds(fireInterval);
            }
        }

        deck.DiscardCurrentHand();
        deck.DrawBalls(drawCount);

        _isFiring = false;
        Debug.Log("[BallSpawner] 손패 발사 종료. 새 손패를 뽑았습니다.", this);
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
            Debug.LogWarning("[BallSpawner] ballPrefab에 BallDataManager가 없습니다. BallDataSO를 주입할 수 없습니다.", spawnedBall);
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
        // SpriteRenderer가 공 루트가 아니라 자식에 붙어 있을 수도 있으므로 자식까지 검색합니다.
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

        // BallSprite가 비어 있으면 Instantiate된 기본 Ball Prefab의 스프라이트를 그대로 둡니다.
        Debug.Log($"[BallSpawner] 탄환 색상만 적용: color {ballData.BallColor}. 스프라이트는 기본 프리팹 값을 사용합니다.", spawnedBall);
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

        if (deck.CurrentHand == null || deck.CurrentHand.Count == 0)
        {
            Debug.LogWarning("[BallSpawner] currentHand가 비어 있어 발사할 탄환이 없습니다.", this);
            return false;
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera == null)
        {
            Debug.LogWarning("[BallSpawner] Camera.main을 찾을 수 없어 마우스 위치를 월드 좌표로 변환할 수 없습니다.", this);
            return false;
        }

        return true;
    }

    Vector2 GetMouseWorldPosition()
    {
        // firePoint는 별도 Transform을 두지 않고, 클릭한 마우스 위치를 월드 좌표로 변환해서 사용합니다.
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = Mathf.Abs(_mainCamera.transform.position.z);
        return _mainCamera.ScreenToWorldPoint(mouseScreenPosition);
    }

    Vector2 GetRandomLaunchDirection()
    {
        // 탄환 발사 방향은 랜덤입니다. 0에 가까운 값이 나오면 안전한 기본 방향을 사용합니다.
        Vector2 direction = Random.insideUnitCircle;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector2.right;
        }

        return direction.normalized;
    }
}
