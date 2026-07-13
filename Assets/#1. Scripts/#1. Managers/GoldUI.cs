using TMPro;
using UnityEngine;

public class GoldUI : MonoBehaviour
{
    [SerializeField] GoldManager goldManager;
    [SerializeField] TMP_Text currentMoneyText;

    void Awake()
    {
        FindMissingReferences();
    }

    void OnEnable()
    {
        FindMissingReferences();

        if (goldManager != null)
        {
            goldManager.OnGoldChanged.RemoveListener(RefreshMoneyText);
            goldManager.OnGoldChanged.AddListener(RefreshMoneyText);
        }

        RefreshMoneyText();
    }

    void OnDisable()
    {
        if (goldManager != null)
        {
            goldManager.OnGoldChanged.RemoveListener(RefreshMoneyText);
        }
    }

    public void RefreshMoneyText()
    {
        if (currentMoneyText == null)
        {
            Debug.LogWarning("[GoldUI] Current money text is not connected.", this);
            return;
        }

        int currentGold = goldManager != null ? goldManager.CurrentGold : 0;
        currentMoneyText.text = $"${currentGold}";
    }

    void FindMissingReferences()
    {
        if (goldManager == null)
        {
            goldManager = FindFirstObjectByType<GoldManager>();
        }
    }
}
