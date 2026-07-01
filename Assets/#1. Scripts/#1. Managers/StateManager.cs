using System.Collections;
using TMPro;
using UnityEngine;

public class StateManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] DamageManager damageManager;
    [SerializeField] DamageUI damageUI;
    [SerializeField] EnemyDataHolder enemyDataHolder;
    [SerializeField] BallSpawner ballSpawner;

    [Header("UI")]
    [SerializeField] TMP_Text finalDamageText;

    [Header("Turn Timing")]
    [SerializeField] float checkInterval = 0.25f;
    [SerializeField] float finalDamageDelay = 0.5f;
    [SerializeField] float nextTurnDelay = 0.5f;

    bool isCheckingBalls;
    bool isResolvingTurn;

    void Awake()
    {
        FindMissingReferences();
    }

    public void OnBallFireSequenceFinished()
    {
        // 게임 시작 직후에는 Ball이 0개일 수 있으므로, BallSpawner가 모든 탄환을 발사한 뒤에만 감지를 시작합니다.
        if (isCheckingBalls || isResolvingTurn)
        {
            Debug.Log("[StateManager] 이미 Ball 감지 또는 턴 결과 처리가 진행 중입니다. 중복 호출을 무시합니다.", this);
            return;
        }

        Debug.Log("[StateManager] 모든 탄환 발사 완료 알림 수신. Ball 태그 오브젝트 감지를 시작합니다.", this);
        StartCoroutine(CheckRemainingBalls());
    }

    IEnumerator CheckRemainingBalls()
    {
        isCheckingBalls = true;

        WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.05f, checkInterval));

        while (!isResolvingTurn)
        {
            GameObject[] balls;

            try
            {
                balls = GameObject.FindGameObjectsWithTag("Ball");
            }
            catch (UnityException)
            {
                Debug.LogWarning("[StateManager] 'Ball' 태그가 없습니다. Unity Tag 설정에 Ball 태그를 추가해 주세요.", this);
                isCheckingBalls = false;
                yield break;
            }

            Debug.Log($"[StateManager] 남은 Ball 태그 오브젝트 수: {balls.Length}", this);

            if (balls.Length <= 0)
            {
                isCheckingBalls = false;
                StartCoroutine(ResolveTurnResultRoutine());
                yield break;
            }

            yield return wait;
        }

        isCheckingBalls = false;
    }

    IEnumerator ResolveTurnResultRoutine()
    {
        // Ball 0개 감지가 여러 번 들어와도 최종 대미지가 중복 계산되지 않도록 방어합니다.
        if (isResolvingTurn)
        {
            Debug.Log("[StateManager] 턴 결과 처리 중복 호출을 무시합니다.", this);
            yield break;
        }

        isResolvingTurn = true;
        FindMissingReferences();

        Debug.Log($"[StateManager] Ball이 0개입니다. {finalDamageDelay}초 뒤 최종 대미지를 계산합니다.", this);
        yield return new WaitForSeconds(Mathf.Max(0f, finalDamageDelay));

        if (damageManager == null)
        {
            Debug.LogWarning("[StateManager] DamageManager가 없어 최종 대미지를 계산할 수 없습니다.", this);
            isResolvingTurn = false;
            yield break;
        }

        int finalDamage = damageManager.CalculateFinalScore();
        ShowFinalDamage(finalDamage);
        Debug.Log($"[StateManager] 최종 대미지 계산 완료: {finalDamage}", this);

        if (enemyDataHolder != null)
        {
            enemyDataHolder.TakeDamage(finalDamage);
        }
        else
        {
            Debug.LogWarning("[StateManager] EnemyDataHolder가 없어 적에게 대미지를 적용할 수 없습니다.", this);
        }

        // 적 체력바/사망 처리까지 끝난 뒤, 잠깐 결과를 보여주고 다음 탄환을 뽑습니다.
        Debug.Log($"[StateManager] 적 체력 처리 완료. {nextTurnDelay}초 뒤 다음 턴 발사 가능 상태로 돌아갑니다.", this);
        yield return new WaitForSeconds(Mathf.Max(0f, nextTurnDelay));

        damageManager.ResetScore();
        Debug.Log("[StateManager] DamageManager 초기화 완료.", this);

        if (ballSpawner != null)
        {
            ballSpawner.DrawNextCylinder();
            Debug.Log("[StateManager] 다음 턴 currentCylinder 뽑기 완료. 다시 발사할 수 있습니다.", this);
        }
        else
        {
            Debug.LogWarning("[StateManager] BallSpawner가 없어 다음 턴 탄환을 뽑을 수 없습니다.", this);
        }

        isResolvingTurn = false;
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

        // 기존 DamageUI를 쓰고 있는 씬도 같이 지원합니다.
        if (damageUI != null)
        {
            damageUI.ShowFinalScore(finalDamage);
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
