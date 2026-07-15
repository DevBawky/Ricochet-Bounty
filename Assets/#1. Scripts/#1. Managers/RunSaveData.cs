using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RunSaveData
{
    public const int CurrentVersion = 1;
    public const string ExpectedFileType = "RicochetBountyRun";

    public string fileType = ExpectedFileType;
    public int version = CurrentVersion;
    public string savedAtUtc;
    public GameState gameState;
    public BattleState battleState;
    public WaveType waveType;
    public int stageIndex;
    public int waveIndex;
    public int currentPlayerLife;
    public int lastBattleRemainingLife;
    public int gold;
    public string enemyId;
    public int maximumEnemyHp;
    public int currentEnemyHp;
    public string battleGridId;
    public bool nonBattleWaveCompletionLocked;
    public List<BattleGridObjectSaveData> battleGridObjects = new List<BattleGridObjectSaveData>();
    public BallDeckSaveData deck = new BallDeckSaveData();
    public PlayerUpgradeSaveData upgrades = new PlayerUpgradeSaveData();
    public DamageSaveData damage = new DamageSaveData();
    public ShotRuntimeSaveData shot = new ShotRuntimeSaveData();
    public RoundSelectSaveData roundSelect = new RoundSelectSaveData();
    public ShopSaveData shop = new ShopSaveData();
    public EventSaveData eventState = new EventSaveData();
    public TreasureSaveData treasure = new TreasureSaveData();
    public ResultSaveData result = new ResultSaveData();
}

[Serializable]
public class BattleGridObjectSaveData
{
    public string prefabId;
    public Vector3 position;
    public Vector3 rotationEuler;
    public Vector3 localScale = Vector3.one;
    public bool active = true;
}

[Serializable]
public class BallDeckSaveData
{
    public List<string> ownedBalls = new List<string>();
    public List<string> drawPile = new List<string>();
    public List<string> discardPile = new List<string>();
    public List<string> currentCylinder = new List<string>();
}

[Serializable]
public class PlayerUpgradeSaveData
{
    public int ballHpLevel;
    public int ballDefenseLevel;
    public int scoreBoostLevel;
    public int currentDeleteCost;
}

[Serializable]
public class DamageSaveData
{
    public int chips;
    public float multiplier = 1f;
}

[Serializable]
public class ShotRuntimeSaveData
{
    public int goldEarnedThisShot;
    public int dividendBallCount;
    public bool dividendMultiplierApplied;
}

[Serializable]
public class RoundSelectSaveData
{
    public List<WaveType> displayedWaveTypes = new List<WaveType>();
    public bool selectionLocked;
}

[Serializable]
public class ShopSaveData
{
    public int currentRefreshCost;
    public int nextRefreshCost;
    public List<ShopSlotSaveData> slots = new List<ShopSlotSaveData>();
}

[Serializable]
public class ShopSlotSaveData
{
    public string ballId;
    public bool purchased;
}

[Serializable]
public class EventSaveData
{
    public int eventId = -1;
    public bool optionSelected;
    public bool completionRequested;
    public string displayedDescription;
}

[Serializable]
public class TreasureSaveData
{
    public string resultBallId;
    public bool treasureOpened;
    public bool rewardReceived;
    public bool completionRequested;
}

[Serializable]
public class ResultSaveData
{
    public int cachedLeftLife;
    public int cachedInterest;
    public int cachedTotal;
    public bool paidOut;
}
