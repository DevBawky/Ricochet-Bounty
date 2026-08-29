using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class ShopBallTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    ShopBallUI owner;
    ShopBallTooltipUI tooltipUI;
    bool isHovered;

    public void Initialize(ShopBallUI shopBallUI)
    {
        owner = shopBallUI;
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
        if (owner == null || owner.CurrentBallData == null)
        {
            HideTooltip();
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        tooltipUI = ShopBallTooltipUI.GetOrCreate(canvas);
        tooltipUI?.Show(owner.CurrentBallData, this);
    }
}
