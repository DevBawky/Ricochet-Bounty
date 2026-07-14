using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Battle > Panel | Cylinder > BallList에 현재 실린더의 탄환을 표시합니다.
// Canvas 프리팹에 저장된 기존 5개 슬롯 참조를 그대로 사용합니다.
public class CylinderUI : MonoBehaviour
{
    [Header("Ball Slots")]
    [SerializeField] Image[] ballIconSlots;
    [SerializeField] Color emptySlotColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] Color activeSlotColor = Color.white;
    [SerializeField] Sprite emptyChamberSprite;
    [SerializeField] TMP_Text leftBulletText;

    [Header("Layout and Animation")]
    [SerializeField] float cylinderRadius = 70f;
    [SerializeField] float startAngle = 90f;
    [SerializeField, Min(0.05f)] float rotationDuration = 0.2f;
    [SerializeField] bool rotateClockwise = true;

    PlayerBallDeck deck;
    BallSpawner ballSpawner;
    Coroutine[] slotAnimations;
    bool[] firedSlots;
    int remainingBallCount;

    void Awake()
    {
        PrepareRuntimeState();
        LayoutSlots();
    }

    void OnEnable()
    {
        FindMissingReferences();
        SubscribeEvents();
        RefreshCylinder();
    }

    void OnDisable()
    {
        UnsubscribeEvents();
    }

    void HandleCylinderDrawn()
    {
        RefreshCylinder();
    }

    void HandleBallFired(int cylinderIndex)
    {
        if (ballIconSlots == null || cylinderIndex < 0 || cylinderIndex >= ballIconSlots.Length)
        {
            return;
        }

        PrepareRuntimeState();
        if (firedSlots[cylinderIndex])
        {
            return;
        }

        firedSlots[cylinderIndex] = true;
        remainingBallCount = Mathf.Max(0, remainingBallCount - 1);
        UpdateBulletText();

        if (slotAnimations[cylinderIndex] != null)
        {
            StopCoroutine(slotAnimations[cylinderIndex]);
        }

        slotAnimations[cylinderIndex] = StartCoroutine(DisappearSlotRoutine(cylinderIndex));
    }

    void RefreshCylinder()
    {
        PrepareRuntimeState();

        int cylinderCount = deck != null && deck.CurrentCylinder != null
            ? Mathf.Min(deck.CurrentCylinder.Count, ballIconSlots.Length)
            : 0;

        remainingBallCount = cylinderCount;

        for (int i = 0; i < ballIconSlots.Length; i++)
        {
            if (slotAnimations[i] != null)
            {
                StopCoroutine(slotAnimations[i]);
                slotAnimations[i] = null;
            }

            firedSlots[i] = false;
            Image slot = ballIconSlots[i];
            if (slot == null)
            {
                continue;
            }

            slot.rectTransform.localScale = Vector3.one;
            BallDataSO ballData = i < cylinderCount ? deck.CurrentCylinder[i] : null;
            SetSlotBall(slot, ballData);
        }

        UpdateBulletText();
    }

    IEnumerator DisappearSlotRoutine(int cylinderIndex)
    {
        Image slot = ballIconSlots[cylinderIndex];
        if (slot == null)
        {
            yield break;
        }

        Color startColor = slot.color;
        Vector3 startScale = slot.rectTransform.localScale;
        float duration = Mathf.Max(0.05f, rotationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = progress * progress;

            Color fadedColor = startColor;
            fadedColor.a = Mathf.Lerp(startColor.a, 0f, easedProgress);
            slot.color = fadedColor;
            slot.rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one * 0.2f, easedProgress);

            yield return null;
        }

        SetSlotEmpty(slot);
        slot.rectTransform.localScale = Vector3.one;
        slotAnimations[cylinderIndex] = null;
    }

    void SetSlotBall(Image slot, BallDataSO ballData)
    {
        if (ballData == null || ballData.BallSprite == null)
        {
            SetSlotEmpty(slot);
            return;
        }

        slot.sprite = ballData.BallSprite;
        slot.preserveAspect = true;
        slot.color = ballData.BallColor * activeSlotColor;
        slot.enabled = true;
    }

    void SetSlotEmpty(Image slot)
    {
        slot.sprite = emptyChamberSprite;
        slot.preserveAspect = true;
        slot.color = emptySlotColor;
        slot.enabled = emptyChamberSprite != null;
    }

    void UpdateBulletText()
    {
        if (leftBulletText == null)
        {
            return;
        }

        int capacity = ballIconSlots != null ? ballIconSlots.Length : 0;
        leftBulletText.text = $"{remainingBallCount}/{capacity}";
    }

    void LayoutSlots()
    {
        if (ballIconSlots == null || ballIconSlots.Length <= 0)
        {
            return;
        }

        float angleStep = 360f / ballIconSlots.Length;
        float direction = rotateClockwise ? -1f : 1f;

        for (int i = 0; i < ballIconSlots.Length; i++)
        {
            if (ballIconSlots[i] == null)
            {
                continue;
            }

            float angle = (startAngle + direction * angleStep * i) * Mathf.Deg2Rad;
            ballIconSlots[i].rectTransform.anchoredPosition = new Vector2(
                Mathf.Cos(angle) * cylinderRadius,
                Mathf.Sin(angle) * cylinderRadius);
        }
    }

    void PrepareRuntimeState()
    {
        int slotCount = ballIconSlots != null ? ballIconSlots.Length : 0;
        if (slotAnimations == null || slotAnimations.Length != slotCount)
        {
            slotAnimations = new Coroutine[slotCount];
        }

        if (firedSlots == null || firedSlots.Length != slotCount)
        {
            firedSlots = new bool[slotCount];
        }
    }

    void FindMissingReferences()
    {
        if (deck == null)
        {
            deck = FindFirstObjectByType<PlayerBallDeck>();
        }

        if (ballSpawner == null)
        {
            ballSpawner = FindFirstObjectByType<BallSpawner>();
        }
    }

    void SubscribeEvents()
    {
        if (ballSpawner == null)
        {
            return;
        }

        ballSpawner.CylinderDrawn -= HandleCylinderDrawn;
        ballSpawner.BallFired -= HandleBallFired;
        ballSpawner.CylinderDrawn += HandleCylinderDrawn;
        ballSpawner.BallFired += HandleBallFired;
    }

    void UnsubscribeEvents()
    {
        if (ballSpawner == null)
        {
            return;
        }

        ballSpawner.CylinderDrawn -= HandleCylinderDrawn;
        ballSpawner.BallFired -= HandleBallFired;
    }
}
