using UnityEngine;

// DamageManager는 전투 중 누적되는 Chips와 Multiplier 값을 관리합니다.
// 이번 단계에서는 충돌 처리, 공 파괴, 적 체력 감소, UI 표시를 구현하지 않습니다.
// 오직 BallDataSO를 받아 Chips 또는 Multiplier를 누적하고, 최종 점수를 계산하는 기본 구조만 담당합니다.
public class DamageManager : MonoBehaviour
{
    [Header("Initial Values")]
    [SerializeField] int initialChips = 0; // 전투 또는 라운드가 시작될 때 적용할 Chips 초기값입니다.
    [SerializeField] float initialMultiplier = 1f; // Multiplier 초기값입니다. 곱셈 기준값이므로 1에서 시작합니다.

    int currentChips;
    float currentMultiplier;

    // 외부 스크립트가 현재 누적 값을 읽을 수 있도록 읽기 전용 프로퍼티를 제공합니다.
    public int CurrentChips
    {
        get
        {
            return currentChips;
        }
    }

    public float CurrentMultiplier
    {
        get
        {
            return currentMultiplier;
        }
    }

    void Awake()
    {
        // 게임이 시작될 때 Inspector에 설정한 초기값이 current 값에 적용되도록 초기화합니다.
        ResetScore();
    }

    // Chips 값을 더합니다.
    // amount가 0 이하라면 점수가 줄어들거나 의미 없는 계산이 될 수 있으므로 처리하지 않습니다.
    public void AddChips(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[DamageManager] AddChips ignored. Amount must be greater than 0. Amount: {amount}", this);
            return;
        }

        currentChips += amount;

        Debug.Log($"[DamageManager] Chips Added: +{amount} / Current Chips: {currentChips}", this);
    }

    // Multiplier 값을 더합니다.
    // 이 프로젝트의 기본 구조는 Multiplier를 누적한 뒤 마지막에 Chips와 곱하는 방식입니다.
    public void AddMultiplier(float amount)
    {
        if (amount <= 0f)
        {
            Debug.LogWarning($"[DamageManager] AddMultiplier ignored. Amount must be greater than 0. Amount: {amount}", this);
            return;
        }

        currentMultiplier += amount;

        Debug.Log($"[DamageManager] Multiplier Added: +{amount} / Current Multiplier: {currentMultiplier}", this);
    }

    // BallDataSO를 받아 타입에 맞는 값을 누적합니다.
    // 다음 단계에서 공이 오브젝트와 충돌했을 때 충돌 시스템이 이 메서드를 호출하도록 연결할 예정입니다.
    public void ApplyBallData(BallDataSO data)
    {
        if (data == null)
        {
            Debug.LogWarning("[DamageManager] ApplyBallData ignored. BallDataSO is null.", this);
            return;
        }

        if (data.ValueType == DamageValueType.Chips)
        {
            // Chips는 정수 점수로 사용하기 위해 Mathf.RoundToInt로 변환합니다.
            int chipsAmount = Mathf.RoundToInt(data.Score);
            AddChips(chipsAmount);
            return;
        }

        if (data.ValueType == DamageValueType.Multiplier)
        {
            AddMultiplier(data.Score);
            return;
        }
    }

    // 모든 공의 처리가 끝난 뒤 호출해서 최종 점수를 계산할 예정입니다.
    // 발라트로식 구조처럼 누적된 Chips와 Multiplier를 곱한 값을 최종 점수로 사용합니다.
    public int CalculateFinalScore()
    {
        float finalScoreFloat = currentChips * currentMultiplier;
        int finalScore = Mathf.RoundToInt(finalScoreFloat);

        Debug.Log($"[DamageManager] Final Score Calculated. Chips: {currentChips}, Multiplier: {currentMultiplier}, Final Score: {finalScore}", this);

        return finalScore;
    }

    // 현재 누적 값을 Inspector에서 설정한 초기값으로 되돌립니다.
    // 새 라운드나 새 계산을 시작할 때 사용할 수 있습니다.
    public void ResetScore()
    {
        currentChips = initialChips;
        currentMultiplier = initialMultiplier;

        Debug.Log($"[DamageManager] Score Reset. Chips: {currentChips}, Multiplier: {currentMultiplier}", this);
    }
}
