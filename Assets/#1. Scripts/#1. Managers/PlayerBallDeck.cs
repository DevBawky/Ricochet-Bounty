using System.Collections.Generic;
using UnityEngine;

public class PlayerBallDeck : MonoBehaviour
{
    [Header("Deck")]
    [SerializeField] List<BallDataSO> startingDeck = new List<BallDataSO>();

    [Header("Runtime")]
    [SerializeField] List<BallDataSO> drawPile = new List<BallDataSO>();
    [SerializeField] List<BallDataSO> discardPile = new List<BallDataSO>();
    [SerializeField] List<BallDataSO> currentHand = new List<BallDataSO>();

    public IReadOnlyList<BallDataSO> CurrentHand
    {
        get
        {
            return currentHand;
        }
    }

    void Awake()
    {
        ResetDeck();
    }

    public void ResetDeck()
    {
        // 전투 시작 시 사용할 덱을 런타임 드로우 더미로 복사합니다.
        // ScriptableObject 자체는 공유하고, 리스트 순서만 런타임에서 변경합니다.
        drawPile.Clear();
        discardPile.Clear();
        currentHand.Clear();

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
        Debug.Log($"[PlayerBallDeck] 덱 초기화 완료. drawPile: {drawPile.Count}", this);
    }

    public void DrawBalls(int count)
    {
        if (count <= 0)
        {
            Debug.LogWarning($"[PlayerBallDeck] 뽑을 개수는 1 이상이어야 합니다. 입력값: {count}", this);
            return;
        }

        currentHand.Clear();

        for (int i = 0; i < count; i++)
        {
            if (!EnsureDrawablePile())
            {
                Debug.LogWarning("[PlayerBallDeck] 더 이상 뽑을 탄환이 없습니다.", this);
                break;
            }

            BallDataSO drawnBall = drawPile[0];
            drawPile.RemoveAt(0);
            currentHand.Add(drawnBall);

            Debug.Log($"[PlayerBallDeck] 탄환 뽑기: {drawnBall.name}", this);
        }

        Debug.Log($"[PlayerBallDeck] 현재 손패: {currentHand.Count}, drawPile: {drawPile.Count}, discardPile: {discardPile.Count}", this);
    }

    public void DiscardCurrentHand()
    {
        // 발사가 끝난 손패를 버림 더미로 옮기고 손패를 비웁니다.
        for (int i = 0; i < currentHand.Count; i++)
        {
            if (currentHand[i] == null)
            {
                continue;
            }

            discardPile.Add(currentHand[i]);
            Debug.Log($"[PlayerBallDeck] discardPile로 이동: {currentHand[i].name}", this);
        }

        currentHand.Clear();
        Debug.Log($"[PlayerBallDeck] 손패 정리 완료. discardPile: {discardPile.Count}", this);
    }

    bool EnsureDrawablePile()
    {
        if (drawPile.Count > 0)
        {
            return true;
        }

        if (discardPile.Count <= 0)
        {
            return false;
        }

        // drawPile이 부족하면 discardPile을 섞어서 재사용합니다.
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);

        Debug.Log($"[PlayerBallDeck] discardPile을 섞어 drawPile로 재사용합니다. drawPile: {drawPile.Count}", this);
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
}
