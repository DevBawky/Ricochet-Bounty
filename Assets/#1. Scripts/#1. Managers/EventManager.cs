using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[Serializable]
public class RoundEventData
{
    [SerializeField] int eventId;
    [SerializeField] string title;
    [SerializeField, TextArea] string description;
    [SerializeField] string option1;
    [SerializeField] string option2;

    public int EventId => eventId;
    public string Title => title;
    public string Description => description;
    public string Option1 => option1;
    public string Option2 => option2;
}

public class EventManager : MonoBehaviour
{
    const int MinimumGoldReward = 5;
    const int MaximumGoldReward = 15;

    [Header("UI")]
    [SerializeField] TMP_Text eventTitleText;
    [SerializeField] TMP_Text eventDescriptionText;
    [SerializeField] Button option1Button;
    [SerializeField] TMP_Text option1Text;
    [SerializeField] Button option2Button;
    [SerializeField] TMP_Text option2Text;
    [SerializeField] Button closeButton;

    [Header("References")]
    [SerializeField] GoldManager goldManager;
    [SerializeField] PlayerBallDeck playerBallDeck;
    [SerializeField] RoundManager roundManager;

    [Header("Broker Ball Pool")]
    [SerializeField] BallDataSO[] ballPool;

    [Header("Event Pool")]
    [SerializeField] RoundEventData[] eventPool;

    RoundEventData currentEvent;
    bool optionSelected;
    bool completionRequested;

    void Awake()
    {
        FindMissingReferences();
        BindButtons();
    }

    void OnDestroy()
    {
        if (option1Button != null) option1Button.onClick.RemoveListener(OnOption1Selected);
        if (option2Button != null) option2Button.onClick.RemoveListener(OnOption2Selected);
        if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    public void BeginEvent()
    {
        FindMissingReferences();
        BindButtons();
        currentEvent = PickRandomEvent();
        optionSelected = false;
        completionRequested = false;

        if (currentEvent == null)
        {
            SetEventText("이벤트", "설정된 유효한 이벤트가 없습니다.", "선택 불가", "선택 불가");
            SetOptionButtons(false);
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(true);
                closeButton.interactable = true;
            }
            optionSelected = true;
            return;
        }

        SetEventText(currentEvent.Title, currentEvent.Description, currentEvent.Option1, currentEvent.Option2);

        SetOptionButtons(true);
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
        }
    }

    void OnOption1Selected()
    {
        ResolveOption(true);
    }

    void OnOption2Selected()
    {
        ResolveOption(false);
    }

    void ResolveOption(bool firstOption)
    {
        if (optionSelected)
        {
            return;
        }

        optionSelected = true;
        SetOptionButtons(false);

        string result = currentEvent != null && currentEvent.EventId == 0
            ? ResolveTreasureKeys()
            : ResolveBroker(firstOption);

        if (eventDescriptionText != null)
        {
            eventDescriptionText.text = result;
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(true);
            closeButton.interactable = true;
        }

        RunSaveManager.Instance?.RequestAutoSave("Event result applied");
    }

    public EventSaveData CaptureSaveData()
    {
        return new EventSaveData
        {
            eventId = currentEvent != null ? currentEvent.EventId : -1,
            optionSelected = optionSelected,
            completionRequested = completionRequested,
            displayedDescription = eventDescriptionText != null ? eventDescriptionText.text : string.Empty
        };
    }

    public void RestoreSaveData(EventSaveData data)
    {
        FindMissingReferences();
        BindButtons();
        currentEvent = data != null ? FindEventById(data.eventId) : null;
        optionSelected = data != null && data.optionSelected;
        completionRequested = data != null && data.completionRequested;

        if (currentEvent == null)
        {
            SetEventText("이벤트", data != null ? data.displayedDescription : string.Empty, "선택 불가", "선택 불가");
        }
        else
        {
            string description = optionSelected && data != null
                ? data.displayedDescription
                : currentEvent.Description;
            SetEventText(currentEvent.Title, description, currentEvent.Option1, currentEvent.Option2);
        }

        SetOptionButtons(!optionSelected && currentEvent != null);
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(optionSelected);
            closeButton.interactable = optionSelected && !completionRequested;
        }
    }

    public void ResetRuntimeState()
    {
        currentEvent = null;
        optionSelected = false;
        completionRequested = false;
    }

    public bool HasEventId(int eventId)
    {
        return FindEventById(eventId) != null;
    }

    string ResolveTreasureKeys()
    {
        bool success = Random.value < 0.5f;
        if (!success)
        {
            return "열쇠가 맞지 않았습니다. 아무 일도 일어나지 않았습니다.";
        }

        int reward = Random.Range(MinimumGoldReward, MaximumGoldReward + 1);
        if (goldManager != null)
        {
            goldManager.AddGold(reward);
        }

        return $"상자가 열렸습니다. 골드 {reward}개를 획득했습니다.";
    }

    string ResolveBroker(bool addBalls)
    {
        if (playerBallDeck == null)
        {
            return "중개인이 공 덱을 찾지 못했습니다.";
        }

        if (addBalls)
        {
            int addedCount = 0;
            for (int i = 0; i < 3 && !playerBallDeck.IsAtCapacity; i++)
            {
                BallDataSO ballData = PickRandomBall();
                if (ballData == null || !playerBallDeck.TryAddBallToDeck(ballData))
                {
                    break;
                }

                addedCount++;
            }

            return $"중개인이 무작위 공 {addedCount}개를 덱에 추가했습니다.";
        }

        int removedCount = 0;
        for (int i = 0; i < 2 && playerBallDeck.CanDeleteOwnedBall; i++)
        {
            int index = Random.Range(0, playerBallDeck.OwnedBallCount);
            BallDataSO ballData = playerBallDeck.OwnedBalls[index];
            if (playerBallDeck.TryRemoveOwnedBallAt(index, ballData))
            {
                removedCount++;
            }
        }

        return $"중개인이 무작위 공 {removedCount}개를 덱에서 제거했습니다.";
    }

    BallDataSO PickRandomBall()
    {
        if (ballPool == null || ballPool.Length <= 0)
        {
            Debug.LogWarning("[EventManager] Broker ball pool is empty.", this);
            return null;
        }

        int startIndex = Random.Range(0, ballPool.Length);
        for (int i = 0; i < ballPool.Length; i++)
        {
            BallDataSO ballData = ballPool[(startIndex + i) % ballPool.Length];
            if (ballData != null)
            {
                return ballData;
            }
        }

        Debug.LogWarning("[EventManager] Broker ball pool has no valid BallDataSO.", this);
        return null;
    }

    RoundEventData PickRandomEvent()
    {
        if (eventPool == null || eventPool.Length <= 0)
        {
            Debug.LogWarning("[EventManager] Event Pool is empty.", this);
            return null;
        }

        int startIndex = Random.Range(0, eventPool.Length);
        for (int i = 0; i < eventPool.Length; i++)
        {
            RoundEventData eventData = eventPool[(startIndex + i) % eventPool.Length];
            if (eventData != null)
            {
                return eventData;
            }
        }

        Debug.LogWarning("[EventManager] Event Pool has no valid entries.", this);
        return null;
    }

    RoundEventData FindEventById(int eventId)
    {
        if (eventPool == null)
        {
            return null;
        }

        for (int i = 0; i < eventPool.Length; i++)
        {
            if (eventPool[i] != null && eventPool[i].EventId == eventId)
            {
                return eventPool[i];
            }
        }

        return null;
    }

    void OnCloseClicked()
    {
        if (!optionSelected || completionRequested)
        {
            return;
        }

        completionRequested = true;
        if (roundManager == null || !roundManager.CompleteNonBattleWave(WaveType.Event))
        {
            completionRequested = false;
        }
    }

    void SetEventText(string title, string description, string option1, string option2)
    {
        if (eventTitleText != null) eventTitleText.text = title;
        if (eventDescriptionText != null) eventDescriptionText.text = description;
        if (option1Text != null) option1Text.text = option1;
        if (option2Text != null) option2Text.text = option2;
    }

    void SetOptionButtons(bool active)
    {
        if (option1Button != null)
        {
            option1Button.interactable = active;
            option1Button.gameObject.SetActive(active);
        }

        if (option2Button != null)
        {
            option2Button.interactable = active;
            option2Button.gameObject.SetActive(active);
        }
    }

    void BindButtons()
    {
        if (option1Button != null)
        {
            option1Button.onClick.RemoveListener(OnOption1Selected);
            option1Button.onClick.AddListener(OnOption1Selected);
        }

        if (option2Button != null)
        {
            option2Button.onClick.RemoveListener(OnOption2Selected);
            option2Button.onClick.AddListener(OnOption2Selected);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseClicked);
            closeButton.onClick.AddListener(OnCloseClicked);
        }
    }

    void FindMissingReferences()
    {
        if (goldManager == null) goldManager = FindFirstObjectByType<GoldManager>(FindObjectsInactive.Include);
        if (playerBallDeck == null) playerBallDeck = FindFirstObjectByType<PlayerBallDeck>(FindObjectsInactive.Include);
        if (roundManager == null) roundManager = FindFirstObjectByType<RoundManager>(FindObjectsInactive.Include);
    }
}
