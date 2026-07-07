using System.Collections;
using TMPro;
using UnityEngine;

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
    [SerializeField] BattleState currentState = BattleState.None;

    [Header("References")]
    [SerializeField] DamageManager damageManager;
    [SerializeField] DamageUI damageUI;
    [SerializeField] EnemyDataHolder enemyDataHolder;
    [SerializeField] BallSpawner ballSpawner;

    [Header("UI")]
    [SerializeField] TMP_Text finalDamageText;

    [Header("Battle Settings")]
    [SerializeField] bool startBattleOnStart = true;

    [Header("Turn Timing")]
    [SerializeField] float checkInterval = 0.25f;
    [SerializeField] float finalDamageDelay = 0.5f;
    [SerializeField] float nextTurnDelay = 0.5f;

    Coroutine checkBallsCoroutine;
    Coroutine resolveTurnCoroutine;

    public BattleState CurrentState
    {
        get
        {
            return currentState;
        }
    }

    void Awake()
    {
        FindMissingReferences();
        ChangeState(BattleState.None);
    }

    void Start()
    {
        if (startBattleOnStart)
        {
            StartBattle();
        }
    }

    public void StartBattle()
    {
        FindMissingReferences();

        if (currentState != BattleState.None && currentState != BattleState.BattleEnd)
        {
            Debug.Log($"[StateManager] 이미 전투가 진행 중입니다. 현재 상태: {currentState}", this);
            return;
        }

        Debug.Log("[StateManager] 전투를 시작합니다.", this);
        ChangeState(BattleState.BattleStart);
        StartTurn();
    }

    public void StartTurn()
    {
        FindMissingReferences();

        if (currentState == BattleState.BattleEnd)
        {
            Debug.Log("[StateManager] 전투가 종료되어 새 턴을 시작하지 않습니다.", this);
            return;
        }

        StopTurnCoroutines();
        ChangeState(BattleState.TurnStart);
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

        ChangeState(BattleState.WaitingForPlayerInput);
        Debug.Log("[StateManager] 플레이어 입력 대기 상태입니다. 이제 발사할 수 있습니다.", this);
    }

    public bool CanPlayerFire()
    {
        bool canFire = currentState == BattleState.WaitingForPlayerInput;
        Debug.Log($"[StateManager] CanPlayerFire 확인: {canFire}, 현재 상태: {currentState}", this);
        return canFire;
    }

    public void OnPlayerFireStarted()
    {
        if (!CanPlayerFire())
        {
            Debug.LogWarning($"[StateManager] 발사를 시작할 수 없는 상태입니다. 현재 상태: {currentState}", this);
            return;
        }

        ChangeState(BattleState.Firing);
        Debug.Log("[StateManager] 플레이어 발사 시작 알림 수신. 상태를 Firing으로 변경했습니다.", this);
    }

    public void OnBallFireSequenceFinished()
    {
        if (currentState != BattleState.Firing)
        {
            Debug.LogWarning($"[StateManager] Firing 상태가 아닌데 발사 완료 알림을 받았습니다. 현재 상태: {currentState}", this);
            return;
        }

        ChangeState(BattleState.WaitingForBalls);
        Debug.Log("[StateManager] currentCylinder의 모든 탄환 발사 완료. Ball 태그 오브젝트 감지를 시작합니다.", this);

        if (checkBallsCoroutine != null)
        {
            StopCoroutine(checkBallsCoroutine);
        }

        checkBallsCoroutine = StartCoroutine(CheckRemainingBallsRoutine());
    }

    public void CheckRemainingBalls()
    {
        if (currentState != BattleState.WaitingForBalls)
        {
            Debug.Log($"[StateManager] Ball 감지를 건너뜁니다. 현재 상태: {currentState}", this);
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
        if (currentState != BattleState.WaitingForBalls)
        {
            Debug.LogWarning($"[StateManager] 턴 결과를 처리할 수 없는 상태입니다. 현재 상태: {currentState}", this);
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

        if (currentState == BattleState.BattleEnd)
        {
            return;
        }

        StartCoroutine(StartNextTurnAfterDelay());
    }

    public void EndBattle()
    {
        StopTurnCoroutines();
        ChangeState(BattleState.BattleEnd);
        Debug.Log("[StateManager] 전투 종료. 적이 사망했습니다.", this);
    }

    IEnumerator CheckRemainingBallsRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.05f, checkInterval));

        while (currentState == BattleState.WaitingForBalls)
        {
            CheckRemainingBalls();

            if (currentState != BattleState.WaitingForBalls)
            {
                yield break;
            }

            yield return wait;
        }
    }

    IEnumerator ResolveTurnResultRoutine()
    {
        ChangeState(BattleState.ResolvingTurn);
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

        if (enemyDataHolder != null)
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

    void ChangeState(BattleState nextState)
    {
        if (currentState == nextState)
        {
            Debug.Log($"[StateManager] BattleState 유지: {currentState}", this);
            return;
        }

        Debug.Log($"[StateManager] BattleState 변경: {currentState} -> {nextState}", this);
        currentState = nextState;
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
    }
}
