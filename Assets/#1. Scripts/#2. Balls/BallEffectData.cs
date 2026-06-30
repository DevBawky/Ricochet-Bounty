using UnityEngine;

// BallEffectData는 BallDataSO 안에서 Inspector로 편집하는 효과 설정값입니다.
// 이 클래스는 설정만 담고, 쿨다운이나 발동 횟수 같은 런타임 상태는 BallEffectRuntimeState가 관리합니다.
[System.Serializable]
public class BallEffectData
{
    [Header("Trigger")]
    [Tooltip("이 효과가 어느 상황에서 발동될지 정합니다.")]
    public BallEffectTrigger trigger;

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
    [Tooltip("효과 수치의 최소값입니다.")]
    public float minValue = 1f;

    [Tooltip("효과 수치의 최대값입니다.")]
    public float maxValue = 1f;
}
