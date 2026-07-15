using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultPayOutUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] RoundManager roundManager;
    [SerializeField] GoldManager goldManager;

    [Header("UI")]
    [SerializeField] TMP_Text leftLifeAmountText;
    [SerializeField] TMP_Text interestAmountText;
    [SerializeField] TMP_Text totalAmountText;
    [SerializeField] Button payOutButton;

    [Header("Rules")]
    [SerializeField, Min(1)] int goldPerInterest = 5;
    [SerializeField, Min(0)] int interestRewardPerStep = 1;

    int cachedLeftLife;
    int cachedInterest;
    int cachedTotal;
    bool isPaidOut;

    void Awake()
    {
        FindMissingReferences();
        BindPayOutButton();
    }

    void OnEnable()
    {
        FindMissingReferences();
        isPaidOut = false;
        RefreshPayOutPreview();
    }

    public void RefreshPayOutPreview()
    {
        cachedLeftLife = roundManager != null ? Mathf.Max(0, roundManager.LastBattleRemainingLife) : 0;
        int currentGold = goldManager != null ? Mathf.Max(0, goldManager.CurrentGold) : 0;
        cachedInterest = currentGold / Mathf.Max(1, goldPerInterest) * Mathf.Max(0, interestRewardPerStep);
        cachedTotal = cachedLeftLife + cachedInterest;

        if (leftLifeAmountText != null)
        {
            leftLifeAmountText.text = cachedLeftLife.ToString();
        }

        if (interestAmountText != null)
        {
            interestAmountText.text = cachedInterest.ToString();
        }

        if (totalAmountText != null)
        {
            totalAmountText.text = cachedTotal.ToString();
        }

        if (payOutButton != null)
        {
            payOutButton.interactable = !isPaidOut;
        }

        Debug.Log($"[ResultPayOutUI] Preview refreshed. Left Life: {cachedLeftLife}, Current Gold: {currentGold}, Interest: {cachedInterest}, Total: {cachedTotal}", this);
    }

    public void PayOut()
    {
        if (isPaidOut)
        {
            Debug.Log("[ResultPayOutUI] Pay Out ignored because it was already paid.", this);
            return;
        }

        FindMissingReferences();

        if (goldManager == null)
        {
            Debug.LogWarning("[ResultPayOutUI] GoldManager is missing. Cannot pay out.", this);
            return;
        }

        RefreshPayOutPreview();
        isPaidOut = true;

        if (cachedTotal > 0)
        {
            goldManager.AddGold(cachedTotal);
        }

        if (payOutButton != null)
        {
            payOutButton.interactable = false;
        }

        Debug.Log($"[ResultPayOutUI] Paid out: +{cachedTotal}. Left Life: {cachedLeftLife}, Interest: {cachedInterest}", this);
        RunSaveManager.Instance?.RequestAutoSave("Result reward applied");
    }

    public ResultSaveData CaptureSaveData()
    {
        return new ResultSaveData
        {
            cachedLeftLife = cachedLeftLife,
            cachedInterest = cachedInterest,
            cachedTotal = cachedTotal,
            paidOut = isPaidOut
        };
    }

    public void RestoreSaveData(ResultSaveData data)
    {
        if (data == null)
        {
            isPaidOut = false;
            RefreshPayOutPreview();
            return;
        }

        cachedLeftLife = Mathf.Max(0, data.cachedLeftLife);
        cachedInterest = Mathf.Max(0, data.cachedInterest);
        cachedTotal = Mathf.Max(0, data.cachedTotal);
        isPaidOut = data.paidOut;

        if (leftLifeAmountText != null) leftLifeAmountText.text = cachedLeftLife.ToString();
        if (interestAmountText != null) interestAmountText.text = cachedInterest.ToString();
        if (totalAmountText != null) totalAmountText.text = cachedTotal.ToString();
        if (payOutButton != null) payOutButton.interactable = !isPaidOut;
    }

    public void ResetRuntimeState()
    {
        cachedLeftLife = 0;
        cachedInterest = 0;
        cachedTotal = 0;
        isPaidOut = false;
    }

    void BindPayOutButton()
    {
        if (payOutButton == null)
        {
            return;
        }

        payOutButton.onClick.RemoveListener(PayOut);
        payOutButton.onClick.AddListener(PayOut);
    }

    void FindMissingReferences()
    {
        if (roundManager == null)
        {
            roundManager = FindFirstObjectByType<RoundManager>(FindObjectsInactive.Include);
        }

        if (goldManager == null)
        {
            goldManager = FindFirstObjectByType<GoldManager>(FindObjectsInactive.Include);
        }
    }
}
