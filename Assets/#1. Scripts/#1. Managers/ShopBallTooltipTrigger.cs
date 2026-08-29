using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class ShopBallTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    ShopBallUI owner;
    BallDataSO ballData;
    ShopBallTooltipUI tooltipUI;
    bool isHovered;

    public void Initialize(ShopBallUI shopBallUI)
    {
        owner = shopBallUI;
        ballData = null;
    }

    public void Initialize(BallDataSO displayedBallData)
    {
        owner = null;
        ballData = displayedBallData;
        RefreshTooltip();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        HideTooltip();
    }

    void OnDisable()
    {
        isHovered = false;
        HideTooltip();
    }

    public void RefreshTooltip()
    {
        if (isHovered)
        {
            ShowTooltip();
        }
    }

    public void HideTooltip()
    {
        tooltipUI?.Hide(this);
    }

    void ShowTooltip()
    {
        BallDataSO displayedBallData = owner != null ? owner.CurrentBallData : ballData;
        if (displayedBallData == null)
        {
            HideTooltip();
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        tooltipUI = ShopBallTooltipUI.GetOrCreate(canvas);
        tooltipUI?.Show(displayedBallData, this);
    }
}
