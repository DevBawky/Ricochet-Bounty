using UnityEngine;

// BallEffectData는 BallDataSO 안에서 Inspector로 편집하는 효과 설정값입니다.
// 효과가 언제 발동되는지는 SpawnEffects, WallHitEffects 같은 리스트 위치가 이미 정해줍니다.
// 그래서 각 Element 안에는 효과 종류, 확률, 쿨다운, 횟수, 수치만 보관합니다.
[System.Serializable]
public class BallEffectData
{
    [Header("효과")]
    [Tooltip("발동되었을 때 실제로 실행할 효과 종류입니다.")]
    public BallEffectType effectType;

    [Range(0f, 1f)]
    [Tooltip("효과 발동 확률입니다. 1은 100%, 0.5는 50%, 0은 발동하지 않음을 뜻합니다.")]
    public float triggerChance = 1f;

    [Header("제한")]
    [Tooltip("같은 효과가 다시 발동하기 전까지 기다려야 하는 시간입니다. 0 이하면 쿨다운이 없습니다.")]
    public float cooldown;

    [Tooltip("한 공에서 이 효과가 최대 몇 번까지 발동할 수 있는지 정합니다. 0 이하면 제한이 없습니다.")]
    public int maxTriggerCount;

    [Header("수치")]
    [Tooltip("효과 수치의 최소값입니다. 공 분열 효과에서는 최소 분열 개수로 사용합니다.")]
    public float minValue = 1f;

    [Tooltip("효과 수치의 최대값입니다. 공 분열 효과에서는 최대 분열 개수로 사용합니다.")]
    public float maxValue = 1f;

    [Header("스택 정산")]
    [Tooltip("칩 스택 정산 효과에서 공이 파괴될 때 스택당 지급할 칩 수입니다.")]
    public int chipsPerStack = 1;

    [Tooltip("배수 스택 정산 효과에서 공이 파괴될 때 스택당 지급할 배수입니다.")]
    public float multiplierPerStack = 0.1f;

    [Header("스택")]
    [Tooltip("스택 효과에서 보상을 지급하기 위해 필요한 스택 수입니다.")]
    public int targetStack = 3;

    [Tooltip("칩 스택 효과에서 목표 스택에 도달했을 때 지급할 칩 수입니다.")]
    public int chipsIncrease = 1;

    [Tooltip("배수 스택 효과에서 목표 스택에 도달했을 때 지급할 배수입니다.")]
    public float multiplierIncrease = 1f;

    [Header("과열")]
    [Tooltip("과열 칩 효과에서 벽 또는 오브젝트 충돌마다 지급할 기본 칩 수입니다.")]
    public int baseChipsIncrease = 1;

    [Tooltip("과열 칩 효과에서 현재 과열 1당 추가로 지급할 칩 수입니다.")]
    public float chipsIncreasePerOverheat = 1f;

    [Tooltip("과열 배수 효과에서 벽 또는 오브젝트 충돌마다 지급할 기본 배수입니다.")]
    public float baseMultiplierIncrease = 1f;

    [Tooltip("과열 배수 효과에서 현재 과열 1당 추가로 지급할 배수입니다.")]
    public float multiplierIncreasePerOverheat = 0.2f;

    [Tooltip("과열 효과에서 자폭 확률 판정을 시작할 과열 수치입니다.")]
    public int selfDestroyStartOverheat = 5;

    [Range(0f, 1f)]
    [Tooltip("자폭 판정 시작 수치에 도달한 뒤 충돌할 때마다 적용할 자폭 확률입니다.")]
    public float selfDestroyChance = 0.15f;

    [Header("연결 보상 효과")]
    [Tooltip("범용 스택, 정산, 과열 효과의 조건을 만족했을 때 실행할 보상 효과입니다.")]
    public BallEffectRewardType rewardEffectType = BallEffectRewardType.AddChips;

    [Tooltip("범용 스택 정산 효과에서 스택당 추가할 보상 수치입니다.")]
    public float rewardValuePerStack = 1f;

    [Tooltip("범용 스택 효과에서 목표 스택에 도달했을 때의 최소 보상 수치입니다.")]
    public float rewardMinValue = 1f;

    [Tooltip("범용 스택 효과에서 목표 스택에 도달했을 때의 최대 보상 수치입니다.")]
    public float rewardMaxValue = 1f;

    [Tooltip("범용 과열 효과에서 벽 또는 오브젝트 충돌마다 지급할 기본 보상 수치입니다.")]
    public float baseRewardValue = 1f;

    [Tooltip("범용 과열 효과에서 현재 과열 1당 추가할 보상 수치입니다.")]
    public float rewardValuePerOverheat = 0.2f;
}
