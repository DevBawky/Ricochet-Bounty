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
}
