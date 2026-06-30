using UnityEngine;

// BallEffectRuntimeState는 BallEffectData 하나의 플레이 중 상태를 관리합니다.
// BallEffectData는 설정값이고, 이 클래스는 마지막 발동 시간과 현재까지 발동한 횟수를 기억합니다.
[System.Serializable]
public class BallEffectRuntimeState
{
    [SerializeField] BallEffectData effectData;
    [SerializeField] BallEffectTrigger trigger;
    [SerializeField] float lastTriggerTime = -999999f;
    [SerializeField] int triggeredCount;

    public BallEffectData EffectData
    {
        get
        {
            return effectData;
        }
    }

    public BallEffectTrigger Trigger
    {
        get
        {
            return trigger;
        }
    }

    public BallEffectRuntimeState(BallEffectData effectData, BallEffectTrigger trigger)
    {
        this.effectData = effectData;
        this.trigger = trigger;
    }

    public bool CanTrigger()
    {
        if (effectData == null)
        {
            return false;
        }

        if (effectData.maxTriggerCount > 0 && triggeredCount >= effectData.maxTriggerCount)
        {
            return false;
        }

        if (effectData.cooldown > 0f && Time.time < lastTriggerTime + effectData.cooldown)
        {
            return false;
        }

        if (effectData.triggerChance <= 0f)
        {
            return false;
        }

        if (effectData.triggerChance >= 1f)
        {
            return true;
        }

        return Random.value <= effectData.triggerChance;
    }

    public void MarkTriggered()
    {
        lastTriggerTime = Time.time;
        triggeredCount++;
    }
}
