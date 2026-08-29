using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class UpgradeShopTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    UpgradeShopUI owner;
    ShopBallTooltipUI tooltipUI;
    bool isHovered;

    public void Initialize(UpgradeShopUI upgradeShopUI)
    {
        owner = upgradeShopUI;
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
        if (owner == null)
        {
            HideTooltip();
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        tooltipUI = ShopBallTooltipUI.GetOrCreate(canvas);
        tooltipUI?.Show(owner.TooltipTitle, owner.TooltipDescription, this);
    }
}
