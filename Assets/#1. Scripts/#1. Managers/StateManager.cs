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
    Clear
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
    [Header("Game State")]
    [SerializeField] GameState currentState = GameState.MainMenu;
    [SerializeField] BattleState currentBattleState = BattleState.None;

    [Header("References")]
    [SerializeField] DamageManager damageManager;
    [SerializeField] DamageUI damageUI;
    [SerializeField] EnemyDataHolder enemyDataHolder;
    [SerializeField] BallSpawner ballSpawner;
    [SerializeField] RoundManager roundManager;

    [Header("UI Panels")]
    [SerializeField] GameObject playerPanel;
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject roundSelectPanel;
    [SerializeField] GameObject battlePanel;
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

            Debug.Log($"[StateManager] GameState 유지: {currentState}", this);
            return;
        }

        GameState previousState = currentState;
        Debug.Log($"[StateManager] GameState 변경: {currentState} -> {nextState}", this);
        currentState = nextState;
        RefreshUIPanels();
        HandleBattleGridStateChange(previousState, nextState);
    }

    // MainMenu UI의 Play Game 버튼에서 호출합니다.
    public void OnClickPlayGame()
    {
        if (roundManager != null)
        {
            roundManager.StartNewRun();
        }

        ChangeState(GameState.RoundSelect);
    }

    // RoundSelect UI의 Battle 선택 버튼에서 호출합니다.
    public void OnClickSelectBattle()
    {
        if (roundManager != null)
        {
            roundManager.PrepareCurrentWaveBattle();
        }

        ChangeState(GameState.Battle);
        StartBattle();
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
        StopTurnCoroutines();
        ChangeBattleState(BattleState.None);

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

    // 플레이어의 라이프가 모두 소모되었을 때 호출합니다.
    public void OnPlayerDefeated()
    {
        StopTurnCoroutines();
        ChangeBattleState(BattleState.BattleEnd);
        ChangeState(GameState.GameOver);
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
            damageManager.ResetScore();
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
        Debug.Log($"[StateManager] Ball이 0개입니다. {finalDamageDelay}초 뒤 최종 대미지를 계산합니다.", this);

        yield return new WaitForSeconds(Mathf.Max(0f, finalDamageDelay));

        FindMissingReferences();

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

        if (roundManager != null && roundManager.SelectedEnemyData != null)
        {
            roundManager.ApplyDamageToCurrentEnemy(finalDamage);

            if (roundManager.CurrentEnemyHp <= 0)
            {
                resolveTurnCoroutine = null;
                yield break;
            }

            roundManager.OnShotEndedWithoutEnemyDefeated();

            if (roundManager.CurrentPlayerLife <= 0)
            {
                resolveTurnCoroutine = null;
                yield break;
            }
        }
        else if (enemyDataHolder != null)
        {
            enemyDataHolder.TakeDamage(finalDamage);
            Debug.Log($"[StateManager] 적 생존 여부 확인. IsDead: {enemyDataHolder.IsDead}", this);

            if (enemyDataHolder.IsDead)
            {
                resolveTurnCoroutine = null;
                EndBattle();
                yield break;
            }
        }
        else
        {
            Debug.LogWarning("[StateManager] EnemyDataHolder가 없어 적에게 대미지를 적용할 수 없습니다. 다음 턴으로 진행합니다.", this);
        }

        resolveTurnCoroutine = null;
        EndTurn();
    }

    IEnumerator StartNextTurnAfterDelay()
    {
        Debug.Log($"[StateManager] 적이 살아있습니다. {nextTurnDelay}초 뒤 다음 턴을 시작합니다.", this);
        yield return new WaitForSeconds(Mathf.Max(0f, nextTurnDelay));
        startNextTurnCoroutine = null;
        StartTurn();
    }

    void ShowFinalDamage(int finalDamage)
    {
        if (finalDamageText != null)
        {
            finalDamageText.text = $"Final Damage : {finalDamage}";
        }
        else
        {
            Debug.LogWarning("[StateManager] finalDamageText가 연결되어 있지 않아 TMP_Text에 최종 대미지를 표시할 수 없습니다.", this);
        }

        if (damageUI != null)
        {
            damageUI.ShowFinalScore(finalDamage);
        }
    }

    void RefreshUIPanels()
    {
        // 상태 전환 시에는 먼저 모든 UI 패널을 끈 뒤, 현재 상태에 필요한 패널만 다시 켭니다.
        SetPanelActive(playerPanel, false);
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(roundSelectPanel, false);
        SetPanelActive(battlePanel, false);
        SetPanelActive(resultPanel, false);
        SetPanelActive(shopPanel, false);
        SetPanelActive(gameOverPanel, false);
        SetPanelActive(clearPanel, false);

        // Player Panel은 MainMenu를 제외한 모든 상태에서 항상 표시합니다.
        SetPanelActive(playerPanel, currentState != GameState.MainMenu);

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

    void FindMissingReferences()
    {
        if (damageManager == null)
        {
            damageManager = FindFirstObjectByType<DamageManager>();
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
    }
}
