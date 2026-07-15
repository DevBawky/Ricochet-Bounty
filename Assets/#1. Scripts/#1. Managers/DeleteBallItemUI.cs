using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeleteBallItemUI : MonoBehaviour
{
    [SerializeField] Image ballImage;
    [SerializeField] Button deleteButton;
    [SerializeField] TMP_Text deletePriceText;

    DeleteBallPanelUI owner;
    BallDataSO displayedBallData;
    int ownedBallIndex = -1;

    void Awake()
    {
        BindDeleteButton();
    }

    public void SetBallData(BallDataSO ballData, int deckIndex, int deleteCost, bool canDelete, DeleteBallPanelUI panelOwner)
    {
        displayedBallData = ballData;
        ownedBallIndex = deckIndex;
        owner = panelOwner;

        if (ballImage != null)
        {
            ballImage.sprite = ballData != null ? ballData.BallSprite : null;
            ballImage.color = ballData != null ? ballData.BallColor : Color.clear;
            ballImage.enabled = ballData != null;
        }

        if (deletePriceText != null)
        {
            deletePriceText.text = $"${Mathf.Max(0, deleteCost)}";
        }

        if (deleteButton != null)
        {
            deleteButton.interactable = ballData != null && canDelete;
        }

        BindDeleteButton();
    }

    public void DeleteDisplayedBall()
    {
        if (owner == null || displayedBallData == null || ownedBallIndex < 0)
        {
            return;
        }

        owner.TryDeleteBall(ownedBallIndex, displayedBallData);
    }

    void BindDeleteButton()
    {
        if (deleteButton == null)
        {
            return;
        }

        deleteButton.onClick.RemoveListener(DeleteDisplayedBall);
        deleteButton.onClick.AddListener(DeleteDisplayedBall);
    }
}
