using UnityEngine;

// BallEffectData는 BallDataSO 안에서 Inspector로 편집하는 효과 설정값입니다.
// 효과가 언제 발동되는지는 SpawnEffects, WallHitEffects 같은 리스트 위치가 이미 정해줍니다.
// 그래서 각 Element 안에는 효과 종류, 확률, 쿨다운, 횟수, 수치만 보관합니다.
[System.Serializable]
public class BallEffectData
{
    [Header("Effect")]
    [Tooltip("발동되었을 때 실제로 실행할 효과 종류입니다.")]
    public BallEffectType effectType;

    [Range(0f, 1f)]
    [Tooltip("효과 발동 확률입니다. 1은 100%, 0.5는 50%, 0은 발동하지 않음을 뜻합니다.")]
    public float triggerChance = 1f;

    [Header("Limit")]
    [Tooltip("같은 효과가 다시 발동하기 전까지 기다려야 하는 시간입니다. 0 이하면 쿨다운이 없습니다.")]
    public float cooldown;

    [Tooltip("한 공에서 이 효과가 최대 몇 번까지 발동할 수 있는지 정합니다. 0 이하면 제한이 없습니다.")]
    public int maxTriggerCount;

    [Header("Value")]
    [Tooltip("효과 수치의 최소값입니다. SplitBall에서는 최소 분열 개수로 사용합니다.")]
    public float minValue = 1f;

    [Tooltip("효과 수치의 최대값입니다. SplitBall에서는 최대 분열 개수로 사용합니다.")]
    public float maxValue = 1f;

    [Header("Stack Cash Out Chips")]
    [Tooltip("StackCashOutChips effect: Chips granted per Stack when the ball is destroyed.")]
    public int chipsPerStack = 1;

    [Tooltip("StackCashOutMultiplier effect: Mult granted per Stack when the ball is destroyed.")]
    public float multiplierPerStack = 0.1f;

    [Header("Stack")]
    [Tooltip("Stack effects: Stack count required before the reward is granted.")]
    public int targetStack = 3;

    [Tooltip("StackChips effect: Chips amount granted when target Stack is reached.")]
    public int chipsIncrease = 1;

    [Tooltip("StackMultiplier effect: Mult amount granted when target Stack is reached.")]
    public float multiplierIncrease = 1f;

    [Header("Overheat")]
    [Tooltip("OverheatChips effect: base Chips amount granted on each wall/object hit.")]
    public int baseChipsIncrease = 1;

    [Tooltip("OverheatChips effect: extra Chips amount granted per current Overheat.")]
    public float chipsIncreasePerOverheat = 1f;

    [Tooltip("OverheatMultiplier effect: base Mult amount granted on each wall/object hit.")]
    public float baseMultiplierIncrease = 1f;

    [Tooltip("OverheatMultiplier effect: extra Mult amount granted per current Overheat.")]
    public float multiplierIncreasePerOverheat = 0.2f;

    [Tooltip("OverheatMultiplier effect: Overheat value where self-destroy rolls begin.")]
    public int selfDestroyStartOverheat = 5;

    [Range(0f, 1f)]
    [Tooltip("OverheatMultiplier effect: self-destroy chance per hit after the start Overheat is reached.")]
    public float selfDestroyChance = 0.15f;

    [Header("Linked Reward Effect")]
    [Tooltip("Generic Stack/CashOut/Overheat effects: reward effect to run when the condition is met.")]
    public BallEffectRewardType rewardEffectType = BallEffectRewardType.AddChips;

    [Tooltip("Generic StackCashOutEffect: reward value added per Stack.")]
    public float rewardValuePerStack = 1f;

    [Tooltip("Generic StackEffect: minimum reward value when target Stack is reached.")]
    public float rewardMinValue = 1f;

    [Tooltip("Generic StackEffect: maximum reward value when target Stack is reached.")]
    public float rewardMaxValue = 1f;

    [Tooltip("Generic OverheatEffect: base reward value on each wall/object hit.")]
    public float baseRewardValue = 1f;

    [Tooltip("Generic OverheatEffect: extra reward value per current Overheat.")]
    public float rewardValuePerOverheat = 0.2f;
}
