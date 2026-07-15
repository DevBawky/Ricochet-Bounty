using System;
using UnityEngine;

[Serializable]
public class WaveTypeSettings
{
    [SerializeField] WaveType waveType;
    [SerializeField] Sprite icon;
    [SerializeField] string displayName;
    [SerializeField, TextArea] string description;
    [SerializeField] Color panelColor = Color.white;
    [SerializeField, Min(0f)] float weight = 1f;

    public WaveType WaveType => waveType;
    public Sprite Icon => icon;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? waveType.ToString() : displayName;
    public string Description => description;
    public Color PanelColor => panelColor;
    public float Weight => Mathf.Max(0f, weight);
}

public class RoundSelectManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] RoundManager roundManager;
    [SerializeField] StateManager stateManager;

    [Header("Wave Type Settings")]
    [SerializeField] WaveTypeSettings[] waveTypeSettings;

    RoundSelectCardUI[] cards;
    bool selectionLocked;

    void Awake()
    {
        FindMissingReferences();
        FindCards();
    }

    public void OnRoundSelectEntered()
    {
        FindMissingReferences();
        FindCards();
        selectionLocked = false;

        if (roundManager == null || cards == null || cards.Length <= 0)
        {
            Debug.LogWarning("[RoundSelectManager] RoundManager or Stage Select cards are missing.", this);
            return;
        }

        roundManager.BeginRoundSelection();

        if (roundManager.IsCurrentBossWave())
        {
            ShowBossSelection();
            return;
        }

        ShowNormalSelections();
    }

    public void SelectWaveType(WaveType waveType)
    {
        if (selectionLocked)
        {
            return;
        }

        selectionLocked = true;

        if (stateManager == null)
        {
            FindMissingReferences();
        }

        if (stateManager == null || !stateManager.TryStartWaveContent(waveType))
        {
            selectionLocked = false;
        }
    }

    public RoundSelectSaveData CaptureSaveData()
    {
        RoundSelectSaveData data = new RoundSelectSaveData
        {
            selectionLocked = selectionLocked
        };

        FindCards();
        if (cards != null)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null && cards[i].gameObject.activeSelf)
                {
                    data.displayedWaveTypes.Add(cards[i].DisplayedWaveType);
                }
            }
        }

        return data;
    }

    public void RestoreSaveData(RoundSelectSaveData data)
    {
        FindCards();
        selectionLocked = data != null && data.selectionLocked;
        if (cards == null || data == null || data.displayedWaveTypes == null)
        {
            return;
        }

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
            {
                continue;
            }

            if (i < data.displayedWaveTypes.Count)
            {
                cards[i].Configure(GetSettings(data.displayedWaveTypes[i]), this);
            }
            else
            {
                cards[i].SetVisible(false);
            }
        }
    }

    void ShowBossSelection()
    {
        WaveTypeSettings bossSettings = GetSettings(WaveType.Boss);
        cards[0].Configure(bossSettings, this);

        for (int i = 1; i < cards.Length; i++)
        {
            cards[i].SetVisible(false);
        }
    }

    void ShowNormalSelections()
    {
        float totalWeight = GetWeight(WaveType.Battle) + GetWeight(WaveType.Event) + GetWeight(WaveType.Treasure);
        bool useBattleFallback = totalWeight <= 0f;

        if (useBattleFallback)
        {
            Debug.LogWarning("[RoundSelectManager] All normal WaveType weights are 0. Battle will be used for every card.", this);
        }

        for (int i = 0; i < cards.Length; i++)
        {
            WaveType waveType = useBattleFallback ? WaveType.Battle : DrawNormalWaveType(totalWeight);
            cards[i].Configure(GetSettings(waveType), this);
        }
    }

    WaveType DrawNormalWaveType(float totalWeight)
    {
        float randomValue = UnityEngine.Random.value * totalWeight;
        float battleWeight = GetWeight(WaveType.Battle);
        WaveType lastEnabledType = WaveType.Battle;
        if (battleWeight > 0f)
        {
            lastEnabledType = WaveType.Battle;
            if (randomValue < battleWeight)
            {
                return WaveType.Battle;
            }
        }

        randomValue -= battleWeight;
        float eventWeight = GetWeight(WaveType.Event);
        if (eventWeight > 0f)
        {
            lastEnabledType = WaveType.Event;
            if (randomValue < eventWeight)
            {
                return WaveType.Event;
            }
        }

        if (GetWeight(WaveType.Treasure) > 0f)
        {
            return WaveType.Treasure;
        }

        return lastEnabledType;
    }

    float GetWeight(WaveType waveType)
    {
        WaveTypeSettings settings = FindSettings(waveType);
        return settings != null ? settings.Weight : 0f;
    }

    WaveTypeSettings GetSettings(WaveType waveType)
    {
        WaveTypeSettings settings = FindSettings(waveType);
        if (settings != null)
        {
            return settings;
        }

        Debug.LogWarning($"[RoundSelectManager] Settings for {waveType} are missing.", this);
        return CreateFallbackSettings(waveType);
    }

    WaveTypeSettings FindSettings(WaveType waveType)
    {
        if (waveTypeSettings == null)
        {
            return null;
        }

        for (int i = 0; i < waveTypeSettings.Length; i++)
        {
            if (waveTypeSettings[i] != null && waveTypeSettings[i].WaveType == waveType)
            {
                return waveTypeSettings[i];
            }
        }

        return null;
    }

    WaveTypeSettings CreateFallbackSettings(WaveType waveType)
    {
        string json = $"{{\"waveType\":{(int)waveType},\"displayName\":\"{waveType}\",\"description\":\"{GetDefaultDescription(waveType)}\",\"panelColor\":{{\"r\":1,\"g\":1,\"b\":1,\"a\":1}},\"weight\":1}}";
        return JsonUtility.FromJson<WaveTypeSettings>(json);
    }

    string GetDefaultDescription(WaveType waveType)
    {
        switch (waveType)
        {
            case WaveType.Event:
                return "Encounter a random event and choose an option.";
            case WaveType.Treasure:
                return "Open a treasure chest and discover a random ball.";
            case WaveType.Boss:
                return "Face the stage boss and survive the final battle.";
            default:
                return "Enter a battle and defeat the enemy.";
        }
    }

    void FindCards()
    {
        cards = GetComponentsInChildren<RoundSelectCardUI>(true);
    }

    void FindMissingReferences()
    {
        if (roundManager == null)
        {
            roundManager = FindFirstObjectByType<RoundManager>(FindObjectsInactive.Include);
        }

        if (stateManager == null)
        {
            stateManager = FindFirstObjectByType<StateManager>(FindObjectsInactive.Include);
        }
    }

}
