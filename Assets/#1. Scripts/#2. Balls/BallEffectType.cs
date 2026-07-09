// BallEffectType은 실제로 어떤 효과를 실행할지 구분합니다.
public enum BallEffectType
{
    // 공의 현재 내구도를 회복합니다.
    HealDurability,

    // 공을 분열시키기 위한 효과입니다. 실제 분열 생성은 다음 단계의 발사기/스폰 시스템에서 연결합니다.
    SplitBall,

    // Chips 또는 Multiplier 중 하나를 랜덤으로 선택해서 증가시킵니다.
    AddRandomScoreValue,

    // 현재 공을 파괴합니다.
    DestroySelf,

    // Adds gold through GoldManager.
    AddGold
}
