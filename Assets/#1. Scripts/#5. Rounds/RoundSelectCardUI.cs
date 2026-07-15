using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundSelectCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Image panelImage;
    [SerializeField] Image stageIcon;
    [SerializeField] TMP_Text stageTypeText;
    [SerializeField] TMP_Text stageDescriptionText;
    [SerializeField] Button selectButton;
    [SerializeField] TMP_Text selectButtonText;

    WaveType displayedWaveType;
    RoundSelectManager roundSelectManager;

    public WaveType DisplayedWaveType => displayedWaveType;

    void Awake()
    {
        roundSelectManager = GetComponentInParent<RoundSelectManager>();
        BindButton();
    }

    void OnDestroy()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnSelectClicked);
        }
    }

    public void Configure(WaveTypeSettings settings, RoundSelectManager owner)
    {
        if (settings == null)
        {
            Debug.LogWarning("[RoundSelectCardUI] WaveType settings are missing.", this);
            return;
        }

        roundSelectManager = owner;
        displayedWaveType = settings.WaveType;
        gameObject.SetActive(true);

        if (panelImage != null)
        {
            panelImage.color = settings.PanelColor;
        }

        if (stageIcon != null)
        {
            stageIcon.sprite = settings.Icon;
            stageIcon.enabled = settings.Icon != null;
        }

        if (stageTypeText != null)
        {
            stageTypeText.text = settings.DisplayName;
        }

        if (stageDescriptionText != null)
        {
            stageDescriptionText.text = settings.Description;
        }

        if (selectButtonText != null)
        {
            selectButtonText.text = settings.DisplayName;
        }

        if (selectButton != null)
        {
            selectButton.interactable = true;
        }

        BindButton();
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    void BindButton()
    {
        if (selectButton == null)
        {
            return;
        }

        selectButton.onClick.RemoveListener(OnSelectClicked);
        selectButton.onClick.AddListener(OnSelectClicked);
    }

    void OnSelectClicked()
    {
        if (roundSelectManager == null)
        {
            roundSelectManager = GetComponentInParent<RoundSelectManager>();
        }

        if (roundSelectManager != null)
        {
            roundSelectManager.SelectWaveType(displayedWaveType);
        }
    }
}
