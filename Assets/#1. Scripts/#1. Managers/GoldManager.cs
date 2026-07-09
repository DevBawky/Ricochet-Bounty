using UnityEngine;
using UnityEngine.Events;

// Tracks the player's gold total and notifies listeners when it changes.
public class GoldManager : MonoBehaviour
{
    [Header("Initial Values")]
    [SerializeField] int initialGold;

    [Header("Events")]
    [SerializeField] UnityEvent onGoldChanged = new UnityEvent();

    int currentGold;

    public UnityEvent OnGoldChanged
    {
        get
        {
            if (onGoldChanged == null)
            {
                onGoldChanged = new UnityEvent();
            }

            return onGoldChanged;
        }
    }

    public int CurrentGold
    {
        get
        {
            return currentGold;
        }
    }

    void Awake()
    {
        ResetGold();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[GoldManager] AddGold ignored. Amount must be greater than 0. Amount: {amount}", this);
            return;
        }

        currentGold += amount;
        OnGoldChanged.Invoke();

        Debug.Log($"[GoldManager] Gold Added: +{amount} / Current Gold: {currentGold}", this);
    }

    public void ResetGold()
    {
        currentGold = Mathf.Max(0, initialGold);
        OnGoldChanged.Invoke();

        Debug.Log($"[GoldManager] Gold Reset. Gold: {currentGold}", this);
    }
}
