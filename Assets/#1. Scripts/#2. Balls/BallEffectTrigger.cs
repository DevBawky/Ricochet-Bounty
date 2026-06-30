// BallEffectTrigger는 공 효과가 어느 상황에서 발동되는지 구분합니다.
public enum BallEffectTrigger
{
    // 공이 생성되었을 때 발동하는 효과입니다.
    OnSpawn,

    // 공이 범퍼나 점수 오브젝트 같은 일반 상호작용 오브젝트와 충돌했을 때 발동하는 효과입니다.
    OnObjectHit,

    // 공이 벽과 충돌했을 때 발동하는 효과입니다.
    OnWallHit,

    // 공이 파괴되기 직전에 발동하는 효과입니다.
    OnDestroy
}
