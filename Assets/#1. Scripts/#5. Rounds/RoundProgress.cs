using System;
using UnityEngine;

[Serializable]
public class RoundProgress
{
    [SerializeField] int currentStageIndex;
    [SerializeField] int currentWaveIndex;
    [SerializeField] int maxStageCount = 8;
    [SerializeField] int normalWaveCountPerStage = 5;
    [SerializeField] int bossWaveNumber = 6;

    public int CurrentStageIndex
    {
        get
        {
            return currentStageIndex;
        }
        set
        {
            currentStageIndex = Mathf.Max(0, value);
        }
    }

    public int CurrentWaveIndex
    {
        get
        {
            return currentWaveIndex;
        }
        set
        {
            currentWaveIndex = Mathf.Max(0, value);
        }
    }

    public int MaxStageCount
    {
        get
        {
            return Mathf.Max(1, maxStageCount);
        }
    }

    public int NormalWaveCountPerStage
    {
        get
        {
            return Mathf.Max(1, normalWaveCountPerStage);
        }
    }

    public int BossWaveNumber
    {
        get
        {
            return Mathf.Max(NormalWaveCountPerStage + 1, bossWaveNumber);
        }
    }

    public int WaveCountPerStage
    {
        get
        {
            return BossWaveNumber;
        }
    }

    public void ResetRun()
    {
        currentStageIndex = 0;
        currentWaveIndex = 0;
    }

    public bool IsBossWave()
    {
        return currentWaveIndex == BossWaveNumber - 1;
    }

    public bool IsFinalStage()
    {
        return currentStageIndex >= MaxStageCount - 1;
    }
}
