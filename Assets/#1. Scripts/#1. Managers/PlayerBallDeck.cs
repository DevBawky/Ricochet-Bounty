using System.Collections.Generic;
using UnityEngine;

public class PlayerBallDeck : MonoBehaviour
{
    [Header("Deck")]
    [SerializeField] List<BallDataSO> startingDeck = new List<BallDataSO>();

    [Header("Runtime")]
    [SerializeField] List<BallDataSO> drawPile = new List<BallDataSO>();
    [SerializeField] List<BallDataSO> discardPile = new List<BallDataSO>();
    [SerializeField] List<BallDataSO> currentCylinder = new List<BallDataSO>();

    public IReadOnlyList<BallDataSO> CurrentHand
    {
        get
        {
            return currentCylinder;
        }
    }

    // 이번 단계의 용어는 currentCylinder입니다.
    // 기존 코드와 인스펙터 필드(currentHand)는 유지하고, 같은 리스트를 currentCylinder처럼 사용할 수 있게 별칭을 제공합니다.
    public IReadOnlyList<BallDataSO> CurrentCylinder
    {
        get
        {
            return currentCylinder;
        }
    }

    public int DrawPileCount
    {
        get
        {
            return drawPile.Count;
        }
    }

    public int DiscardPileCount
    {
        get
        {
            return discardPile.Count;
        }
    }

    public int CurrentCylinderCount
    {
        get
        {
            return currentCylinder.Count;
        }
    }

    void Awake()
    {
        ResetDeck();
    }

    public void ResetDeck()
    {
        // 전투 시작 시 startingDeck을 drawPile로 복사합니다.
        // ScriptableObject 자체는 공유하고, 런타임 리스트의 순서와 위치만 변경합니다.
        drawPile.Clear();
        discardPile.Clear();
        currentCylinder.Clear();

        for (int i = 0; i < startingDeck.Count; i++)
        {
            if (startingDeck[i] == null)
            {
                Debug.LogWarning($"[PlayerBallDeck] startingDeck의 {i}번 칸이 비어 있습니다.", this);
                continue;
            }

            drawPile.Add(startingDeck[i]);
        }

        Shuffle(drawPile);
        Debug.Log($"[PlayerBallDeck] 덱 초기화 완료. drawPile: {drawPile.Count}, discardPile: {discardPile.Count}, currentCylinder: {currentCylinder.Count}", this);
    }

    public void DrawBalls(int count)
    {
        if (count <= 0)
        {
            Debug.LogWarning($"[PlayerBallDeck] 뽑을 개수는 1 이상이어야 합니다. 입력값: {count}", this);
            return;
        }

        if (currentCylinder.Count > 0)
        {
            Debug.LogWarning($"[PlayerBallDeck] currentCylinder에 탄환이 남아 있어 새로 뽑지 않습니다. currentCylinder: {currentCylinder.Count}", this);
            LogPileState("Draw skipped");
            return;
        }

        Debug.Log($"[PlayerBallDeck] 탄환 뽑기 시작. 요청 수: {count}", this);

        for (int i = 0; i < count; i++)
        {
            if (!EnsureDrawablePile())
            {
                Debug.LogWarning("[PlayerBallDeck] 더 이상 뽑을 탄환이 없습니다.", this);
                break;
            }

            BallDataSO drawnBall = drawPile[0];
            drawPile.RemoveAt(0);
            currentCylinder.Add(drawnBall);

            string drawnName = drawnBall != null ? drawnBall.name : "NULL";
            Debug.Log($"[PlayerBallDeck] 이번에 뽑힌 탄환: {drawnName}", this);
            LogPileState("Draw one");
        }

        LogPileState("Draw complete");
    }

    public void DiscardCurrentHand()
    {
        // 발사가 끝난 currentCylinder를 discardPile로 옮기고 currentCylinder를 비웁니다.
        if (currentCylinder.Count <= 0)
        {
            Debug.Log("[PlayerBallDeck] discardPile로 옮길 currentCylinder 탄환이 없습니다.", this);
            LogPileState("Discard skipped");
            return;
        }

        for (int i = 0; i < currentCylinder.Count; i++)
        {
            if (currentCylinder[i] == null)
            {
                continue;
            }

            discardPile.Add(currentCylinder[i]);
            Debug.Log($"[PlayerBallDeck] 발사 후 discardPile 이동: {currentCylinder[i].name}", this);
        }

        currentCylinder.Clear();
        LogPileState("Discard complete");
    }

    public void AddBallToDeck(BallDataSO ballData)
    {
        if (ballData == null)
        {
            Debug.LogWarning("[PlayerBallDeck] Cannot add null BallDataSO to deck.", this);
            return;
        }

        discardPile.Add(ballData);
        Debug.Log($"[PlayerBallDeck] Purchased ball added to discardPile: {ballData.name}", this);
        LogPileState("Add purchased ball");
    }

    bool EnsureDrawablePile()
    {
        if (drawPile.Count > 0)
        {
            return true;
        }

        if (discardPile.Count <= 0)
        {
            Debug.Log("[PlayerBallDeck] drawPile이 비었지만 discardPile도 비어 있어 재사용할 탄환이 없습니다.", this);
            return false;
        }

        // drawPile이 부족하면 discardPile을 섞어서 다시 drawPile로 사용합니다.
        Debug.Log($"[PlayerBallDeck] drawPile 부족. discardPile을 섞어서 재사용합니다. 재사용 전 discardPile: {discardPile.Count}", this);
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);

        LogPileState("Reshuffle discard into draw");
        return drawPile.Count > 0;
    }

    void Shuffle(List<BallDataSO> targetPile)
    {
        for (int i = targetPile.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            BallDataSO temp = targetPile[i];
            targetPile[i] = targetPile[randomIndex];
            targetPile[randomIndex] = temp;
        }
    }

    void LogPileState(string context)
    {
        Debug.Log($"[PlayerBallDeck] {context} | drawPile: {drawPile.Count}, discardPile: {discardPile.Count}, currentCylinder: {currentCylinder.Count}", this);
    }
}
