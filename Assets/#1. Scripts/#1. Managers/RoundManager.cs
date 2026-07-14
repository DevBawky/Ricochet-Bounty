using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class RoundManager : MonoBehaviour
{
    [Header("Progress")]
    [SerializeField] RoundProgress roundProgress = new RoundProgress();
    [SerializeField] StageData[] stageDataList;

    [Header("References")]
    [SerializeField] StateManager stateManager;

    [Header("Battle Camera")]
    [SerializeField, Tooltip("Orthographic camera used to display the Battle Grid.")]
    Camera battleCamera;
    [SerializeField, Tooltip("Screen-space UI region where the entire Battle Grid must be visible.")]
    RectTransform battleViewRect;
    [SerializeField, Min(0f), Tooltip("World-space margin added around every side of the Battle Grid.")]
    float cameraPadding = 0.25f;
    [SerializeField, Min(0.01f)] float minimumOrthographicSize = 1f;
    [SerializeField, Min(0.01f)] float maximumOrthographicSize = 20f;

    [Header("Round UI")]
    [SerializeField] TextMeshProUGUI stageText;
    [SerializeField] TextMeshProUGUI waveText;
    [SerializeField] TextMeshProUGUI stageProgressText;
    [SerializeField] Image bossProgressBarImage;

    [Header("Target UI")]
    [SerializeField] Image targetEnemyImage;
    [SerializeField] TextMeshProUGUI targetEnemyNameText;
    [SerializeField] TextMeshProUGUI targetEnemyHpText;
    [SerializeField] Image targetEnemyHpFillImage;

    [Header("Selected Battle Data")]
    [SerializeField] EnemyData selectedEnemyData;
    [SerializeField] GameObject selectedBattleGridPrefab;
    [SerializeField] GameObject spawnedBattleGrid;
    [SerializeField] int currentEnemyHp;

    [Header("Player Life")]
    [SerializeField] int maxPlayerLife = 5;
    [SerializeField] int currentPlayerLife;

    public RoundProgress Progress
    {
        get
        {
            return roundProgress;
        }
    }

    public EnemyData SelectedEnemyData
    {
        get
        {
            return selectedEnemyData;
        }
    }

    public GameObject SelectedBattleGridPrefab
    {
        get
        {
            return selectedBattleGridPrefab;
        }
    }

    public GameObject SpawnedBattleGrid
    {
        get
        {
            return spawnedBattleGrid;
        }
    }

    public int CurrentEnemyHp
    {
        get
        {
            return currentEnemyHp;
        }
    }

    public int CurrentPlayerLife
    {
        get
        {
            return currentPlayerLife;
        }
    }

    void Awake()
    {
        FindMissingReferences();

        if (currentPlayerLife <= 0)
        {
            currentPlayerLife = Mathf.Max(0, maxPlayerLife);
        }
    }

    void Start()
    {
        RefreshRoundUI();
        RefreshTargetUI();
    }

    // 새 런을 시작할 때 진행도를 Stage 1, Wave 1로 되돌립니다.
    public void StartNewRun()
    {
        roundProgress.ResetRun();
        currentPlayerLife = Mathf.Max(0, maxPlayerLife);
        ClearSelectedBattleData();
        RefreshRoundUI();
        RefreshTargetUI();
    }

    // 현재 Wave가 보스 Wave인지 확인합니다.
    public bool IsCurrentBossWave()
    {
        return roundProgress.IsBossWave();
    }

    // 현재 Wave의 대표 타입을 반환합니다. Event/Treasure 선택 로직은 Round Select 카드 시스템에서 확장합니다.
    public WaveType GetCurrentWaveType()
    {
        if (IsCurrentBossWave())
        {
            return WaveType.Boss;
        }

        return WaveType.Battle;
    }

    // Round Select에서 Battle 또는 Boss 카드를 선택했을 때 호출해 현재 전투 데이터를 확정합니다.
    public void PrepareCurrentWaveBattle()
    {
        StageData currentStageData = GetCurrentStageData();
        if (currentStageData == null)
        {
            ClearSelectedBattleData();
            RefreshTargetUI();
            Debug.LogWarning("[RoundManager] 현재 StageData가 없어 전투 데이터를 선택할 수 없습니다.", this);
            return;
        }

        if (IsCurrentBossWave())
        {
            selectedEnemyData = currentStageData.BossEnemy;
            selectedBattleGridPrefab = currentStageData.BossBattleGrid;
        }
        else
        {
            selectedEnemyData = PickRandomEnemy(currentStageData.EnemyCandidates);
            selectedBattleGridPrefab = PickRandomBattleGrid(currentStageData.BattleGridCandidates);
        }

        currentEnemyHp = selectedEnemyData != null ? Mathf.Max(0, selectedEnemyData.MaxHealth) : 0;
        RefreshRoundUI();
        RefreshTargetUI();

        Debug.Log($"[RoundManager] 전투 데이터 선택 완료. Enemy: {GetSelectedEnemyName()}, Grid: {GetSelectedGridName()}, HP: {currentEnemyHp}", this);
    }

    // Battle 상태에 진입할 때 선택된 전투 맵 프리팹을 월드 좌표 (0, 0)에 생성합니다.
    public void SpawnSelectedBattleGrid()
    {
        ClearSpawnedBattleGrid();

        if (selectedBattleGridPrefab == null)
        {
            Debug.LogWarning("[RoundManager] 선택된 전투 맵 프리팹이 없어 맵을 생성할 수 없습니다.", this);
            return;
        }

        spawnedBattleGrid = Instantiate(selectedBattleGridPrefab, Vector3.zero, Quaternion.identity);
        spawnedBattleGrid.name = selectedBattleGridPrefab.name;
        SpawnBattleGridObjects();
        AdjustCameraToSpawnedBattleGrid();

        Debug.Log($"[RoundManager] 전투 맵 생성 완료: {spawnedBattleGrid.name}, 위치: {spawnedBattleGrid.transform.position}", this);
    }

    [ContextMenu("Adjust Camera To Spawned Battle Grid")]
    public void AdjustCameraToSpawnedBattleGrid()
    {
        if (spawnedBattleGrid == null)
        {
            Debug.LogWarning("[RoundManager] Camera adjustment skipped because no Battle Grid is spawned.", this);
            return;
        }

        if (battleCamera == null)
        {
            Debug.LogWarning("[RoundManager] Battle Camera is not connected.", this);
            return;
        }

        if (!battleCamera.orthographic)
        {
            Debug.LogWarning("[RoundManager] Battle Camera must use Orthographic projection.", battleCamera);
            return;
        }

        if (battleViewRect == null)
        {
            Debug.LogWarning("[RoundManager] Battle View RectTransform is not connected.", this);
            return;
        }

        Canvas.ForceUpdateCanvases();

        if (!TryGetBattleGridBounds(spawnedBattleGrid, out Bounds stageBounds))
        {
            Debug.LogWarning($"[RoundManager] No usable TilemapRenderer, Renderer, or Collider2D bounds were found under {spawnedBattleGrid.name}.", spawnedBattleGrid);
            return;
        }

        if (!TryGetBattleViewScreenRect(battleViewRect, out Rect screenRect))
        {
            Debug.LogWarning("[RoundManager] Battle View has no usable screen-space area.", battleViewRect);
            return;
        }

        float battleViewAspect = screenRect.width / screenRect.height;
        float paddedWidth = stageBounds.size.x + cameraPadding * 2f;
        float paddedHeight = stageBounds.size.y + cameraPadding * 2f;
        float sizeForHeight = paddedHeight * 0.5f;
        float sizeForWidth = paddedWidth * 0.5f / battleViewAspect;
        float minimumSize = Mathf.Min(minimumOrthographicSize, maximumOrthographicSize);
        float maximumSize = Mathf.Max(minimumOrthographicSize, maximumOrthographicSize);

        battleCamera.orthographicSize = Mathf.Clamp(
            Mathf.Max(sizeForHeight, sizeForWidth),
            minimumSize,
            maximumSize
        );

        Vector2 battleViewScreenCenter = screenRect.center;
        float distanceToStagePlane = stageBounds.center.z - battleCamera.transform.position.z;
        Vector3 worldAtBattleViewCenter = battleCamera.ScreenToWorldPoint(
            new Vector3(battleViewScreenCenter.x, battleViewScreenCenter.y, distanceToStagePlane)
        );
        Vector3 cameraPosition = battleCamera.transform.position;
        cameraPosition.x += stageBounds.center.x - worldAtBattleViewCenter.x;
        cameraPosition.y += stageBounds.center.y - worldAtBattleViewCenter.y;
        battleCamera.transform.position = cameraPosition;

        Debug.Log($"[RoundManager] Battle Camera adjusted. Grid Bounds: {stageBounds}, Battle View: {screenRect}, Orthographic Size: {battleCamera.orthographicSize}, Camera Position: {battleCamera.transform.position}", this);
    }

    // Battle이 끝나거나 다른 상태로 이동할 때 생성된 전투 맵을 제거합니다.
    public void ClearSpawnedBattleGrid()
    {
        if (spawnedBattleGrid == null)
        {
            return;
        }

        Destroy(spawnedBattleGrid);
        spawnedBattleGrid = null;
    }

    // 발사 1회가 끝났을 때 누적 대미지를 현재 적 HP에 적용합니다.
    public void ApplyDamageToCurrentEnemy(int damage)
    {
        if (selectedEnemyData == null)
        {
            Debug.LogWarning("[RoundManager] 선택된 적 데이터가 없어 대미지를 적용할 수 없습니다.", this);
            return;
        }

        int safeDamage = Mathf.Max(0, damage);
        currentEnemyHp = Mathf.Max(0, currentEnemyHp - safeDamage);
        RefreshTargetUI();

        Debug.Log($"[RoundManager] 현재 적에게 대미지 적용: -{safeDamage}, 남은 HP: {currentEnemyHp}", this);

        if (currentEnemyHp <= 0)
        {
            HandleCurrentEnemyDefeated();
        }
    }

    // 적을 처치하지 못한 채 발사 1회가 끝났을 때 라이프를 1 차감합니다.
    public void OnShotEndedWithoutEnemyDefeated()
    {
        if (selectedEnemyData == null || currentEnemyHp <= 0)
        {
            return;
        }

        currentPlayerLife = Mathf.Max(0, currentPlayerLife - 1);
        Debug.Log($"[RoundManager] 적을 처치하지 못해 라이프를 1 차감합니다. 남은 라이프: {currentPlayerLife}", this);

        if (currentPlayerLife <= 0)
        {
            HandlePlayerLifeDepleted();
        }
    }

    // 적 처치 시 라운드 진행도를 갱신하고 StateManager의 전투 클리어 흐름으로 넘깁니다.
    public void HandleBattleCleared()
    {
        bool clearedBossWave = IsCurrentBossWave();
        bool clearedFinalBoss = clearedBossWave && roundProgress.IsFinalStage();

        selectedEnemyData?.OnDeath();

        if (clearedFinalBoss)
        {
            ClearSelectedBattleData();
            RefreshRoundUI();
            RefreshTargetUI();

            if (stateManager != null)
            {
                stateManager.ChangeState(GameState.Clear);
            }

            Debug.Log("[RoundManager] 마지막 스테이지 보스를 클리어했습니다. Clear 상태로 전환합니다.", this);
            return;
        }

        AdvanceProgressAfterBattle(clearedBossWave);
        ClearSelectedBattleData();
        RefreshRoundUI();
        RefreshTargetUI();

        if (stateManager != null)
        {
            stateManager.OnBattleCleared();
        }
    }

    // 라이프 시스템이 연결되면 적을 처치하지 못한 발사 종료 시 이 함수를 호출해 GameOver 분기로 확장할 수 있습니다.
    public void HandlePlayerLifeDepleted()
    {
        if (stateManager != null)
        {
            stateManager.OnPlayerDefeated();
        }
    }

    public void RefreshRoundUI()
    {
        int displayStage = roundProgress.CurrentStageIndex + 1;
        int displayWave = roundProgress.CurrentWaveIndex + 1;

        if (stageText != null)
        {
            stageText.text = $"Stage {displayStage} / {roundProgress.MaxStageCount}";
        }

        if (waveText != null)
        {
            waveText.text = $"Wave {displayWave} / {roundProgress.WaveCountPerStage}";
        }

        if (bossProgressBarImage != null)
        {
            bossProgressBarImage.fillAmount = GetCurrentStageProgress01();
        }

        if (stageProgressText != null)
        {
            int progressPercent = Mathf.RoundToInt(GetCurrentStageProgress01() * 100f);
            stageProgressText.text = $"{progressPercent}%";
        }
    }

    void HandleCurrentEnemyDefeated()
    {
        Debug.Log("[RoundManager] 현재 적 HP가 0 이하입니다. 전투 클리어 처리를 시작합니다.", this);
        HandleBattleCleared();
    }

    void AdvanceProgressAfterBattle(bool clearedBossWave)
    {
        if (clearedBossWave)
        {
            roundProgress.CurrentStageIndex++;
            roundProgress.CurrentWaveIndex = 0;
            return;
        }

        roundProgress.CurrentWaveIndex++;
    }

    void SpawnBattleGridObjects()
    {
        if (spawnedBattleGrid == null)
        {
            return;
        }

        BattleGridObjectSpawner objectSpawner = spawnedBattleGrid.GetComponentInChildren<BattleGridObjectSpawner>();
        if (objectSpawner == null)
        {
            Debug.Log($"[RoundManager] BattleGridObjectSpawner가 없는 맵입니다. Grid: {spawnedBattleGrid.name}", spawnedBattleGrid);
            return;
        }

        objectSpawner.SpawnObjects();
    }

    bool TryGetBattleGridBounds(GameObject battleGrid, out Bounds bounds)
    {
        TilemapRenderer[] tilemapRenderers = battleGrid.GetComponentsInChildren<TilemapRenderer>(true);
        if (TryEncapsulateRendererBounds(tilemapRenderers, out bounds))
        {
            return true;
        }

        Renderer[] renderers = battleGrid.GetComponentsInChildren<Renderer>(true);
        if (TryEncapsulateRendererBounds(renderers, out bounds))
        {
            return true;
        }

        Collider2D[] colliders = battleGrid.GetComponentsInChildren<Collider2D>(true);
        bool hasBounds = false;
        bounds = default;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
            {
                continue;
            }

            Bounds colliderBounds = collider.bounds;
            if (colliderBounds.size.sqrMagnitude <= 0f)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = colliderBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(colliderBounds);
            }
        }

        return hasBounds;
    }

    bool TryEncapsulateRendererBounds<T>(T[] renderers, out Bounds bounds) where T : Renderer
    {
        bool hasBounds = false;
        bounds = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            T renderer = renderers[i];
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            Bounds rendererBounds = renderer.bounds;
            if (rendererBounds.size.sqrMagnitude <= 0f)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = rendererBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(rendererBounds);
            }
        }

        return hasBounds;
    }

    bool TryGetBattleViewScreenRect(RectTransform viewRect, out Rect screenRect)
    {
        Vector3[] worldCorners = new Vector3[4];
        viewRect.GetWorldCorners(worldCorners);

        Canvas canvas = viewRect.GetComponentInParent<Canvas>();
        Camera canvasCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            canvasCamera = canvas.worldCamera != null ? canvas.worldCamera : battleCamera;
        }

        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(canvasCamera, worldCorners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(canvasCamera, worldCorners[2]);
        float xMin = Mathf.Min(bottomLeft.x, topRight.x);
        float yMin = Mathf.Min(bottomLeft.y, topRight.y);
        float width = Mathf.Abs(topRight.x - bottomLeft.x);
        float height = Mathf.Abs(topRight.y - bottomLeft.y);

        screenRect = new Rect(xMin, yMin, width, height);
        return width > 0.01f && height > 0.01f;
    }

    StageData GetCurrentStageData()
    {
        if (stageDataList == null || stageDataList.Length <= 0)
        {
            return null;
        }

        int stageIndex = Mathf.Clamp(roundProgress.CurrentStageIndex, 0, stageDataList.Length - 1);
        return stageDataList[stageIndex];
    }

    EnemyData PickRandomEnemy(EnemyData[] enemies)
    {
        if (enemies == null || enemies.Length <= 0)
        {
            return null;
        }

        return enemies[Random.Range(0, enemies.Length)];
    }

    GameObject PickRandomBattleGrid(GameObject[] battleGrids)
    {
        if (battleGrids == null || battleGrids.Length <= 0)
        {
            return null;
        }

        return battleGrids[Random.Range(0, battleGrids.Length)];
    }

    void RefreshTargetUI()
    {
        FindTargetEnemyNameText();

        if (targetEnemyImage != null)
        {
            targetEnemyImage.sprite = selectedEnemyData != null ? selectedEnemyData.EnemySprite : null;
            targetEnemyImage.enabled = selectedEnemyData != null && selectedEnemyData.EnemySprite != null;
        }

        if (targetEnemyNameText != null)
        {
            targetEnemyNameText.text = selectedEnemyData != null ? selectedEnemyData.EnemyName : "Enemy";
        }

        if (targetEnemyHpText != null)
        {
            if (selectedEnemyData == null)
            {
                targetEnemyHpText.text = "- / -";
            }
            else
            {
                targetEnemyHpText.text = $"{currentEnemyHp} / {selectedEnemyData.MaxHealth}";
            }
        }

        if (targetEnemyHpFillImage != null)
        {
            targetEnemyHpFillImage.fillAmount = GetCurrentEnemyHpRatio();
        }
    }

    float GetCurrentStageProgress01()
    {
        int progressMaxIndex = Mathf.Max(1, roundProgress.WaveCountPerStage - 1);
        return Mathf.Clamp01((float)roundProgress.CurrentWaveIndex / progressMaxIndex);
    }

    float GetCurrentEnemyHpRatio()
    {
        if (selectedEnemyData == null || selectedEnemyData.MaxHealth <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)currentEnemyHp / selectedEnemyData.MaxHealth);
    }

    void ClearSelectedBattleData()
    {
        ClearSpawnedBattleGrid();
        selectedEnemyData = null;
        selectedBattleGridPrefab = null;
        currentEnemyHp = 0;
    }

    string GetSelectedEnemyName()
    {
        return selectedEnemyData != null ? selectedEnemyData.EnemyName : "None";
    }

    string GetSelectedGridName()
    {
        return selectedBattleGridPrefab != null ? selectedBattleGridPrefab.name : "None";
    }

    void FindMissingReferences()
    {
        if (stateManager == null)
        {
            stateManager = FindFirstObjectByType<StateManager>();
        }

        FindTargetEnemyNameText();
    }

    void FindTargetEnemyNameText()
    {
        if (targetEnemyNameText != null || targetEnemyImage == null || targetEnemyImage.transform.parent == null)
        {
            return;
        }

        Transform enemyRoot = targetEnemyImage.transform.parent;
        TextMeshProUGUI[] texts = enemyRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].transform.parent == enemyRoot && texts[i].name == "Text (TMP)")
            {
                targetEnemyNameText = texts[i];
                return;
            }
        }
    }

    void OnValidate()
    {
        cameraPadding = Mathf.Max(0f, cameraPadding);
        minimumOrthographicSize = Mathf.Max(0.01f, minimumOrthographicSize);
        maximumOrthographicSize = Mathf.Max(minimumOrthographicSize, maximumOrthographicSize);
    }
}
