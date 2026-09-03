using System;
using System.Collections;
using TMPro;
using UnityEngine;

public enum GameState
{
    MainMenu,
    RoundSelect,
    Battle,
    Result,
    Shop,
    GameOver,
    Clear,
    Event,
    Treasure
}

public enum BattleState
{
    None,
    BattleStart,
    TurnStart,
    WaitingForPlayerInput,
    Firing,
    WaitingForBalls,
    ResolvingTurn,
    BattleEnd
}

public class StateManager : MonoBehaviour
{
    public event Action<GameState, GameState> StateChanged;

    [Header("Game State")]
    [SerializeField] GameState currentState = GameState.MainMenu;
    [SerializeField] BattleState currentBattleState = BattleState.None;

    [Header("References")]
    [SerializeField] DamageManager damageManager;
    [SerializeField] BallRegistry ballRegistry;
    [SerializeField] ShotRuntimeContext shotRuntimeContext;
    [SerializeField] DamageUI damageUI;
    [SerializeField] EnemyDataHolder enemyDataHolder;
    [SerializeField] BallSpawner ballSpawner;
    [SerializeField] RoundManager roundManager;
    [SerializeField] ShopManager shopManager;
    [SerializeField] RoundSelectManager roundSelectManager;
    [SerializeField] EventManager eventManager;
    [SerializeField] TreasureManager treasureManager;
    [SerializeField] KillableDamageHighlightController killableHighlightController;
    [SerializeField] CyberPanelDissolveController panelDissolveController;

    [Header("UI Panels")]
    [SerializeField] GameObject playerPanel;
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject roundSelectPanel;
    [SerializeField] GameObject battlePanel;
    [SerializeField] GameObject eventPanel;
    [SerializeField] GameObject treasurePanel;
    [SerializeField] GameObject resultPanel;
    [SerializeField] GameObject shopPanel;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] GameObject clearPanel;

    [Header("UI")]
    [SerializeField] TMP_Text finalDamageText;

    [Header("Turn Timing")]
    [SerializeField] float checkInterval = 0.25f;
    [SerializeField] float finalDamageDelay = 0.5f;
    [SerializeField] float nextTurnDelay = 0.5f;

    Coroutine checkBallsCoroutine;
    Coroutine resolveTurnCoroutine;
    Coroutine startNextTurnCoroutine;

    public GameState CurrentState
    {
        get
        {
            return currentState;
        }
    }

    public BattleState CurrentBattleState
    {
        get
        {
            return currentBattleState;
        }
    }

    void Awake()
    {
        FindMissingReferences();
        ChangeBattleState(BattleState.None);
    }

    void Start()
    {
        // 게임 시작 시에는 항상 메인 메뉴 UI만 보이도록 초기화합니다.
        ChangeState(GameState.MainMenu);
    }

    // 외부 UI 버튼 또는 게임 로직에서 게임의 큰 화면 상태를 전환할 때 사용합니다.
    public void ChangeState(GameState nextState)
    {
        if (currentState == nextState)
        {
            RefreshUIPanels();

            if (nextState == GameState.Battle && roundManager != null && roundManager.SpawnedBattleGrid == null)
            {
                roundManager.SpawnSelectedBattleGrid();
            }

            if (nextState == GameState.Shop)
            {
                FindMissingReferences();
                if (shopManager != null)
                {
                    shopManager.RefreshCostUI();
                }
            }

            if (nextState == GameState.RoundSelect)
            {
                FindMissingReferences();
                roundSelectManager?.OnRoundSelectEntered();
            }

            Debug.Log($"[StateManager] GameState 유지: {currentState}", this);
            return;
        }

        GameState previousState = currentState;
        Debug.Log($"[StateManager] GameState 변경: {currentState} -> {nextState}", this);
        currentState = nextState;
        StateChanged?.Invoke(previousState, nextState);

        // Result UI가 활성화되기 전에 잔여 라이프를 정산용으로 보존하고 다음 전투 값을 복구합니다.
        if (previousState == GameState.Battle && nextState != GameState.Battle && roundManager != null)
        {
            roundManager.CompleteBattleLife();
        }

        if (previousState == GameState.Battle && nextState != GameState.Battle)
        {
            StopTurnCoroutines();
            damageManager?.CancelPresentation();
        }

        if (previousState == GameState.Shop && nextState != GameState.Shop && shopManager != null)
        {
            shopManager.OnShopExited();
        }

        RefreshUIPanels();
        HandleBattleGridStateChange(previousState, nextState);

        if (nextState == GameState.Shop)
        {
            FindMissingReferences();
            if (shopManager != null)
            {
                shopManager.OnShopEntered();
            }
        }

        if (nextState == GameState.RoundSelect)
        {
            FindMissingReferences();
            roundSelectManager?.OnRoundSelectEntered();
        }

        if (nextState != GameState.MainMenu)
        {
            RunSaveManager.Instance?.RequestAutoSave($"GameState changed to {nextState}");
        }
    }

    // MainMenu UI의 Play Game 버튼에서 호출합니다.
    public void OnClickPlayGame()
    {
        if (RunSaveManager.Instance != null)
        {
            RunSaveManager.Instance.OnClickPlayGame();
            return;
        }

        if (PlayerUpgradeManager.Instance != null)
        {
            PlayerUpgradeManager.Instance.ResetRunData();
        }

        if (roundManager != null)
        {
            roundManager.StartNewRun();
        }

        ChangeState(GameState.RoundSelect);
    }

    // RoundSelect UI의 Battle 선택 버튼에서 호출합니다.
    public void OnClickSelectBattle()
    {
        WaveType waveType = roundManager != null ? roundManager.GetCurrentWaveType() : WaveType.Battle;
        TryStartWaveContent(waveType);
    }

    public bool TryStartWaveContent(WaveType waveType)
    {
        FindMissingReferences();

        if (roundManager == null)
        {
            Debug.LogWarning("[StateManager] RoundManager is missing.", this);
            return false;
        }

        bool isBossWave = roundManager.IsCurrentBossWave();
        if (isBossWave && waveType != WaveType.Boss)
        {
            Debug.LogWarning($"[StateManager] Boss Wave cannot start as {waveType}.", this);
            return false;
        }

        if (!isBossWave && waveType == WaveType.Boss)
        {
            Debug.LogWarning("[StateManager] Boss content cannot start during a normal Wave.", this);
            return false;
        }

        switch (waveType)
        {
            case WaveType.Battle:
            case WaveType.Boss:
                roundManager.PrepareCurrentWaveBattle();
                if (roundManager.SelectedEnemyData == null)
                {
                    return false;
                }

                if (enemyDataHolder != null)
                {
                    enemyDataHolder.Initialize(roundManager.SelectedEnemyData, roundManager.MaximumEnemyHp);
                }

                ChangeState(GameState.Battle);
                StartBattle();
                return true;

            case WaveType.Event:
                if (eventManager == null)
                {
                    Debug.LogWarning("[StateManager] EventManager is missing.", this);
                    return false;
                }

                roundManager.PrepareNonBattleWave(waveType);
                ChangeState(GameState.Event);
                eventManager.BeginEvent();
                return true;

            case WaveType.Treasure:
                if (treasureManager == null)
                {
                    Debug.LogWarning("[StateManager] TreasureManager is missing.", this);
                    return false;
                }

                roundManager.PrepareNonBattleWave(waveType);
                ChangeState(GameState.Treasure);
                treasureManager.BeginTreasure();
                return true;
        }

        return false;
    }

    // Result UI의 Continue 또는 Pay Out 버튼에서 호출합니다.
    public void OnClickContinueResult()
    {
        ChangeState(GameState.Shop);
    }

    // Shop UI의 Next Battle 버튼에서 호출합니다.
    public void OnClickNextBattle()
    {
        ChangeState(GameState.RoundSelect);
    }

    // GameOver 또는 Clear UI에서 메인 메뉴로 돌아갈 때 호출합니다.
    public void OnClickGoToMainMenu()
    {
        StopTurnCoroutines();
        ChangeBattleState(BattleState.None);
        ChangeState(GameState.MainMenu);
    }

    // GameOver UI에서 다시 시작할 때 호출합니다.
    public void OnClickRestartRun()
    {
        if (RunSaveManager.Instance != null)
        {
            RunSaveManager.Instance.StartNewRunAndSave();
            return;
        }

        StopTurnCoroutines();
        ChangeBattleState(BattleState.None);

        if (PlayerUpgradeManager.Instance != null)
        {
            PlayerUpgradeManager.Instance.ResetRunData();
        }

        if (roundManager != null)
        {
            roundManager.StartNewRun();
        }

        ChangeState(GameState.RoundSelect);
    }

    // Clear UI에서 무한 모드로 진입할 때 호출합니다.
    public void OnClickEnterEndlessMode()
    {
        ChangeState(GameState.RoundSelect);
    }

    // 적을 모두 처치했을 때 호출합니다. 추후 라운드 데이터가 추가되면 Clear 분기 로직을 이곳에 확장합니다.
    public void OnBattleCleared()
    {
        StopTurnCoroutines();
        ChangeBattleState(BattleState.BattleEnd);
        ChangeState(GameState.Result);
    }

    public void OnGameCleared()
    {
        EnterRunEndState(GameState.Clear);
    }

    public void OnNonBattleWaveCleared()
    {
        StopTurnCoroutines();
        ChangeBattleState(BattleState.None);
        ChangeState(GameState.Shop);
    }

    // 플레이어의 라이프가 모두 소모되었을 때 호출합니다.
    public void OnPlayerDefeated()
    {
        EnterRunEndState(GameState.GameOver);
    }

    public void StartBattle()
    {
        FindMissingReferences();

        if (currentBattleState != BattleState.None && currentBattleState != BattleState.BattleEnd)
        {
            Debug.Log($"[StateManager] 이미 전투가 진행 중입니다. 현재 전투 상태: {currentBattleState}", this);
            return;
        }

        Debug.Log("[StateManager] 전투를 시작합니다.", this);
        damageManager?.CancelPresentation();
        if (roundManager != null)
        {
            damageManager?.SetEnemyHealthImmediate(roundManager.CurrentEnemyHp, roundManager.MaximumEnemyHp);
        }
        ChangeBattleState(BattleState.BattleStart);
        StartTurn();
    }

    public void StartTurn()
    {
        FindMissingReferences();

        if (currentBattleState == BattleState.BattleEnd)
        {
            Debug.Log("[StateManager] 전투가 종료되어 새 턴을 시작하지 않습니다.", this);
            return;
        }

        StopTurnCoroutines();
        ChangeBattleState(BattleState.TurnStart);
        Debug.Log("[StateManager] 턴 시작. DamageManager 초기화 후 currentCylinder를 뽑습니다.", this);

        if (damageManager != null)
        {
            damageManager.CancelPresentation();
            if (roundManager != null)
            {
                damageManager.SetEnemyHealthImmediate(roundManager.CurrentEnemyHp, roundManager.MaximumEnemyHp);
            }
            damageManager.ResetScore();
            damageUI?.ClearFinalScore();
        }
        else
        {
            Debug.LogWarning("[StateManager] DamageManager가 없어 이번 턴 대미지 값을 초기화할 수 없습니다.", this);
        }

        if (ballSpawner != null)
        {
            ballSpawner.DrawNextCylinder();
        }
        else
        {
            Debug.LogWarning("[StateManager] BallSpawner가 없어 currentCylinder를 뽑을 수 없습니다.", this);
        }

        ChangeBattleState(BattleState.WaitingForPlayerInput);
        RunSaveManager.Instance?.RequestAutoSave("Turn ready");
        Debug.Log("[StateManager] 플레이어 입력 대기 상태입니다. 이제 발사할 수 있습니다.", this);
    }

    public bool CanPlayerFire()
    {
        bool canFire = currentState == GameState.Battle && currentBattleState == BattleState.WaitingForPlayerInput;
        Debug.Log($"[StateManager] CanPlayerFire 확인: {canFire}, 게임 상태: {currentState}, 전투 상태: {currentBattleState}", this);
        return canFire;
    }

    public void OnPlayerFireStarted()
    {
        if (!CanPlayerFire())
        {
            Debug.LogWarning($"[StateManager] 발사를 시작할 수 없는 상태입니다. 게임 상태: {currentState}, 전투 상태: {currentBattleState}", this);
            return;
        }

        RunSaveManager.Instance?.SaveCurrentRun("Before fire");
        killableHighlightController?.BeginScoringSequence();
        FindMissingReferences();
        if (shotRuntimeContext != null)
        {
            shotRuntimeContext.ResetShot();
        }
        else
        {
            Debug.LogWarning("[StateManager] ShotRuntimeContext가 없어 발사 단위 상태를 초기화할 수 없습니다.", this);
        }

        ChangeBattleState(BattleState.Firing);
        Debug.Log("[StateManager] 플레이어 발사 시작 알림 수신. 전투 상태를 Firing으로 변경했습니다.", this);
    }

    public void OnBallFireSequenceFinished()
    {
        if (currentBattleState != BattleState.Firing)
        {
            Debug.LogWarning($"[StateManager] Firing 상태가 아닌데 발사 완료 알림을 받았습니다. 현재 전투 상태: {currentBattleState}", this);
            return;
        }

        ChangeBattleState(BattleState.WaitingForBalls);
        Debug.Log("[StateManager] currentCylinder의 모든 탄환 발사 완료. Ball 태그 오브젝트 감지를 시작합니다.", this);

        if (checkBallsCoroutine != null)
        {
            StopCoroutine(checkBallsCoroutine);
        }

        checkBallsCoroutine = StartCoroutine(CheckRemainingBallsRoutine());
    }

    public void CheckRemainingBalls()
    {
        if (currentBattleState != BattleState.WaitingForBalls)
        {
            Debug.Log($"[StateManager] Ball 감지를 건너뜁니다. 현재 전투 상태: {currentBattleState}", this);
            return;
        }

        FindMissingReferences();
        if (ballRegistry != null)
        {
            int activeBallCount = ballRegistry.ActiveBallCount;
            Debug.Log($"[StateManager] BallRegistry 활성 탄환 수: {activeBallCount}", this);

            if (activeBallCount <= 0)
            {
                if (checkBallsCoroutine != null)
                {
                    StopCoroutine(checkBallsCoroutine);
                    checkBallsCoroutine = null;
                }

                ResolveTurnResult();
            }

            return;
        }

        GameObject[] balls;

        try
        {
            balls = GameObject.FindGameObjectsWithTag("Ball");
        }
        catch (UnityException)
        {
            Debug.LogWarning("[StateManager] 'Ball' 태그가 없습니다. Unity Tag 설정에 Ball 태그를 추가해 주세요.", this);
            return;
        }

        Debug.Log($"[StateManager] 남은 Ball 태그 오브젝트 수: {balls.Length}", this);

        if (balls.Length <= 0)
        {
            if (checkBallsCoroutine != null)
            {
                StopCoroutine(checkBallsCoroutine);
                checkBallsCoroutine = null;
            }

            ResolveTurnResult();
        }
    }

    public void ResolveTurnResult()
    {
        if (currentBattleState != BattleState.WaitingForBalls)
        {
            Debug.LogWarning($"[StateManager] 턴 결과를 처리할 수 없는 상태입니다. 현재 전투 상태: {currentBattleState}", this);
            return;
        }

        if (resolveTurnCoroutine != null)
        {
            Debug.Log("[StateManager] 이미 턴 결과 처리 중입니다. 중복 호출을 무시합니다.", this);
            return;
        }

        resolveTurnCoroutine = StartCoroutine(ResolveTurnResultRoutine());
    }

    public void EndTurn()
    {
        Debug.Log("[StateManager] 턴을 종료합니다.", this);

        if (currentBattleState == BattleState.BattleEnd)
        {
            return;
        }

        if (startNextTurnCoroutine != null)
        {
            StopCoroutine(startNextTurnCoroutine);
        }

        startNextTurnCoroutine = StartCoroutine(StartNextTurnAfterDelay());
    }

    public void EndBattle()
    {
        Debug.Log("[StateManager] 전투 종료. 적이 사망했습니다.", this);

        if (roundManager != null)
        {
            roundManager.HandleBattleCleared();
            return;
        }

        OnBattleCleared();
    }

    IEnumerator CheckRemainingBallsRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.05f, checkInterval));

        while (currentBattleState == BattleState.WaitingForBalls)
        {
            CheckRemainingBalls();

            if (currentBattleState != BattleState.WaitingForBalls)
            {
                yield break;
            }

            yield return wait;
        }
    }

    IEnumerator ResolveTurnResultRoutine()
    {
        ChangeBattleState(BattleState.ResolvingTurn);
        FindMissingReferences();

        if (damageManager != null && shotRuntimeContext != null)
        {
            shotRuntimeContext.ApplyDividendBonus(damageManager);
        }

        while (damageManager != null && !damageManager.IsScorePresentationComplete)
        {
            yield return null;
        }

        Debug.Log($"[StateManager] 점수 전달과 텍스트 보간 완료. {finalDamageDelay}초 뒤 최종 대미지를 계산합니다.", this);
        yield return new WaitForSeconds(Mathf.Max(0f, finalDamageDelay));

        int finalDamage = 0;
        if (damageManager != null)
        {
            finalDamage = damageManager.CalculateFinalScore();
        }
        else
        {
            Debug.LogWarning("[StateManager] DamageManager가 없어 최종 대미지를 0으로 처리합니다.", this);
        }

        ShowFinalDamage(finalDamage);
        Debug.Log($"[StateManager] 최종 대미지 계산 완료: {finalDamage}", this);

        while (damageUI != null && !damageUI.IsFinalDamageCountUpComplete)
        {
            yield return null;
        }

        if (damageUI != null && damageUI.FinalDamageParticleDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(damageUI.FinalDamageParticleDelay);
        }

        if (roundManager != null && roundManager.SelectedEnemyData != null)
        {
            bool deliveryStarted = damageManager != null && damageManager.BeginFinalDamageDelivery(
                finalDamage,
                roundManager.CurrentEnemyHp,
                roundManager.MaximumEnemyHp,
                damage =>
                {
                    roundManager.ApplyDamageToCurrentEnemyDeferred(damage);
                    damageManager.SetEnemyHealthTarget(roundManager.CurrentEnemyHp, roundManager.MaximumEnemyHp);
                });

            if (!deliveryStarted)
            {
                roundManager.ApplyDamageToCurrentEnemyDeferred(finalDamage);
                damageManager?.SetEnemyHealthImmediate(roundManager.CurrentEnemyHp, roundManager.MaximumEnemyHp);
                roundManager.RefreshDeferredEnemyHealthBar();
            }

            while (deliveryStarted && damageManager != null && !damageManager.IsFinalDamagePresentationComplete)
            {
                yield return null;
            }

            bool enemyDefeated = roundManager.CurrentEnemyHp <= 0;
            ResetResolvedShot();

            if (enemyDefeated)
            {
                resolveTurnCoroutine = null;
                roundManager.ResolveDeferredEnemyDamage();
                yield break;
            }

            roundManager.OnShotEndedWithoutEnemyDefeated();

            RunSaveManager.Instance?.RequestAutoSave("Shot result and life applied");

            if (currentState != GameState.Battle || currentBattleState == BattleState.BattleEnd)
            {
                resolveTurnCoroutine = null;
                yield break;
            }
        }
        else if (enemyDataHolder != null)
        {
            bool deliveryStarted = damageManager != null && damageManager.BeginFinalDamageDelivery(
                finalDamage,
                enemyDataHolder.CurrentHealth,
                enemyDataHolder.MaximumHealth,
                damage =>
                {
                    enemyDataHolder.TakeDamageDeferred(damage);
                    damageManager.SetEnemyHealthTarget(enemyDataHolder.CurrentHealth, enemyDataHolder.MaximumHealth);
                });

            if (!deliveryStarted)
            {
                enemyDataHolder.TakeDamageDeferred(finalDamage);
                damageManager?.SetEnemyHealthImmediate(enemyDataHolder.CurrentHealth, enemyDataHolder.MaximumHealth);
            }

            while (deliveryStarted && damageManager != null && !damageManager.IsFinalDamagePresentationComplete)
            {
                yield return null;
            }

            bool enemyDefeated = enemyDataHolder.CurrentHealth <= 0;
            ResetResolvedShot();
            Debug.Log($"[StateManager] 적 생존 여부 확인. Defeated: {enemyDefeated}", this);

            if (enemyDefeated)
            {
                resolveTurnCoroutine = null;
                enemyDataHolder.ResolveDeferredDamage();
                EndBattle();
                yield break;
            }

            enemyDataHolder.ResolveDeferredDamage();
        }
        else
        {
            Debug.LogWarning("[StateManager] EnemyDataHolder가 없어 적에게 대미지를 적용할 수 없습니다. 다음 턴으로 진행합니다.", this);
            ResetResolvedShot();
        }

        resolveTurnCoroutine = null;
        EndTurn();
    }

    void ResetResolvedShot()
    {
        if (damageManager != null)
        {
            damageManager.ResetScore();
        }

        if (shotRuntimeContext != null)
        {
            shotRuntimeContext.ResetShot();
        }
    }

    IEnumerator StartNextTurnAfterDelay()
    {
        Debug.Log($"[StateManager] 적이 살아있습니다. {nextTurnDelay}초 뒤 다음 턴을 시작합니다.", this);
        yield return new WaitForSeconds(Mathf.Max(0f, nextTurnDelay));
        startNextTurnCoroutine = null;
        StartTurn();
    }

    public void RestoreLoadedState(GameState savedGameState, BattleState savedBattleState)
    {
        StopTurnCoroutines();
        damageManager?.CancelPresentation();
        ClearActiveBalls();
        currentState = savedGameState;
        currentBattleState = savedGameState == GameState.Battle
            ? BattleState.WaitingForPlayerInput
            : savedBattleState == BattleState.BattleEnd ? BattleState.BattleEnd : BattleState.None;
        RefreshUIPanels();

        if (savedGameState == GameState.Battle && roundManager != null)
        {
            roundManager.AdjustCameraToSpawnedBattleGrid();
            if (enemyDataHolder != null)
            {
                enemyDataHolder.Restore(
                    roundManager.SelectedEnemyData,
                    roundManager.MaximumEnemyHp,
                    roundManager.CurrentEnemyHp);
            }

            damageManager?.SetEnemyHealthImmediate(roundManager.CurrentEnemyHp, roundManager.MaximumEnemyHp);
        }
    }

    public void ReturnToMainMenuAfterLoadFailure()
    {
        StopTurnCoroutines();
        damageManager?.CancelPresentation();
        ClearActiveBalls();
        currentBattleState = BattleState.None;
        currentState = GameState.MainMenu;
        if (roundManager != null)
        {
            roundManager.ClearSpawnedBattleGrid();
        }

        RefreshUIPanels();
    }

    void ShowFinalDamage(int finalDamage)
    {
        if (damageUI != null)
        {
            damageUI.BeginFinalDamageCountUp(finalDamage);
            return;
        }

        if (finalDamageText != null)
        {
            finalDamageText.text = finalDamage.ToString();
        }
        else
        {
            Debug.LogWarning("[StateManager] finalDamageText가 연결되어 있지 않아 TMP_Text에 최종 대미지를 표시할 수 없습니다.", this);
        }
    }

    void RefreshUIPanels()
    {
        bool showPlayerPanel = currentState != GameState.MainMenu &&
            currentState != GameState.GameOver &&
            currentState != GameState.Clear;

        if (panelDissolveController != null)
        {
            panelDissolveController.TransitionTo(GetCurrentPrimaryPanel(), showPlayerPanel);
            return;
        }

        // 상태 전환 시에는 먼저 모든 UI 패널을 끈 뒤, 현재 상태에 필요한 패널만 다시 켭니다.
        SetPanelActive(playerPanel, false);
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(roundSelectPanel, false);
        SetPanelActive(battlePanel, false);
        SetPanelActive(eventPanel, false);
        SetPanelActive(treasurePanel, false);
        SetPanelActive(resultPanel, false);
        SetPanelActive(shopPanel, false);
        SetPanelActive(gameOverPanel, false);
        SetPanelActive(clearPanel, false);

        // 종료 화면과 MainMenu에서는 Player Panel을 숨깁니다.
        SetPanelActive(playerPanel, showPlayerPanel);

        switch (currentState)
        {
            case GameState.MainMenu:
                SetPanelActive(mainMenuPanel, true);
                break;
            case GameState.RoundSelect:
                SetPanelActive(roundSelectPanel, true);
                break;
            case GameState.Battle:
                SetPanelActive(battlePanel, true);
                break;
            case GameState.Event:
                SetPanelActive(eventPanel, true);
                break;
            case GameState.Treasure:
                SetPanelActive(treasurePanel, true);
                break;
            case GameState.Result:
                SetPanelActive(resultPanel, true);
                break;
            case GameState.Shop:
                SetPanelActive(shopPanel, true);
                break;
            case GameState.GameOver:
                SetPanelActive(gameOverPanel, true);
                break;
            case GameState.Clear:
                SetPanelActive(clearPanel, true);
                break;
        }
    }

    GameObject GetCurrentPrimaryPanel()
    {
        switch (currentState)
        {
            case GameState.MainMenu: return mainMenuPanel;
            case GameState.RoundSelect: return roundSelectPanel;
            case GameState.Battle: return battlePanel;
            case GameState.Event: return eventPanel;
            case GameState.Treasure: return treasurePanel;
            case GameState.Result: return resultPanel;
            case GameState.Shop: return shopPanel;
            case GameState.GameOver: return gameOverPanel;
            case GameState.Clear: return clearPanel;
            default: return null;
        }
    }

    void HandleBattleGridStateChange(GameState previousState, GameState nextState)
    {
        if (roundManager == null)
        {
            return;
        }

        if (previousState == GameState.Battle && nextState != GameState.Battle)
        {
            roundManager.ClearSpawnedBattleGrid();
        }

        if (nextState == GameState.Battle)
        {
            roundManager.SpawnSelectedBattleGrid();
        }
    }

    void SetPanelActive(GameObject panel, bool isActive)
    {
        // 인스펙터에 패널이 아직 연결되지 않아도 상태 전환 자체는 안전하게 진행합니다.
        if (panel == null)
        {
            return;
        }

        panel.SetActive(isActive);
    }

    void ChangeBattleState(BattleState nextState)
    {
        if (currentBattleState == nextState)
        {
            Debug.Log($"[StateManager] BattleState 유지: {currentBattleState}", this);
            return;
        }

        Debug.Log($"[StateManager] BattleState 변경: {currentBattleState} -> {nextState}", this);
        currentBattleState = nextState;
    }

    void EnterRunEndState(GameState endState)
    {
        if (endState != GameState.GameOver && endState != GameState.Clear)
        {
            Debug.LogError($"[StateManager] {endState} is not a valid run end state.", this);
            return;
        }

        if (currentState == GameState.GameOver || currentState == GameState.Clear)
        {
            Debug.LogWarning($"[StateManager] Duplicate run end request was ignored. Current state: {currentState}", this);
            return;
        }

        StopTurnCoroutines();
        ChangeBattleState(BattleState.BattleEnd);
        ChangeState(endState);
        RunSaveManager.Instance?.DeleteSaveFiles();
    }

    void StopTurnCoroutines()
    {
        if (checkBallsCoroutine != null)
        {
            StopCoroutine(checkBallsCoroutine);
            checkBallsCoroutine = null;
        }

        if (resolveTurnCoroutine != null)
        {
            StopCoroutine(resolveTurnCoroutine);
            resolveTurnCoroutine = null;
        }

        if (startNextTurnCoroutine != null)
        {
            StopCoroutine(startNextTurnCoroutine);
            startNextTurnCoroutine = null;
        }
    }

    void ClearActiveBalls()
    {
        Ball[] balls = FindObjectsByType<Ball>(FindObjectsSortMode.None);
        for (int i = 0; i < balls.Length; i++)
        {
            if (balls[i] != null)
            {
                balls[i].gameObject.SetActive(false);
                Destroy(balls[i].gameObject);
            }
        }

        ballRegistry?.Clear();
    }

    void FindMissingReferences()
    {
        if (damageManager == null)
        {
            damageManager = FindFirstObjectByType<DamageManager>();
        }

        if (ballRegistry == null)
        {
            ballRegistry = FindFirstObjectByType<BallRegistry>();
        }

        if (shotRuntimeContext == null)
        {
            shotRuntimeContext = FindFirstObjectByType<ShotRuntimeContext>();
        }

        if (damageUI == null)
        {
            damageUI = FindFirstObjectByType<DamageUI>();
        }

        if (enemyDataHolder == null)
        {
            enemyDataHolder = FindFirstObjectByType<EnemyDataHolder>();
        }

        if (ballSpawner == null)
        {
            ballSpawner = FindFirstObjectByType<BallSpawner>();
        }

        if (roundManager == null)
        {
            roundManager = FindFirstObjectByType<RoundManager>();
        }

        if (shopManager == null)
        {
            shopManager = FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        }

        if (roundSelectManager == null)
        {
            roundSelectManager = FindFirstObjectByType<RoundSelectManager>(FindObjectsInactive.Include);
        }

        if (eventManager == null)
        {
            eventManager = FindFirstObjectByType<EventManager>(FindObjectsInactive.Include);
        }

        if (treasureManager == null)
        {
            treasureManager = FindFirstObjectByType<TreasureManager>(FindObjectsInactive.Include);
        }

        if (killableHighlightController == null)
        {
            killableHighlightController = FindFirstObjectByType<KillableDamageHighlightController>(FindObjectsInactive.Include);
        }

        if (panelDissolveController == null)
        {
            panelDissolveController = FindFirstObjectByType<CyberPanelDissolveController>(FindObjectsInactive.Include);
        }

        if (eventPanel == null && eventManager != null)
        {
            eventPanel = eventManager.gameObject;
        }

        if (treasurePanel == null && treasureManager != null)
        {
            treasurePanel = treasureManager.gameObject;
        }
    }
}
