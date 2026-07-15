using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerBallDeck : MonoBehaviour
{
    public const int MaxOwnedBallCount = 15;
    public const int MinimumOwnedBallCount = 5;

    [Header("Deck")]
    [SerializeField] List<BallDataSO> startingDeck = new List<BallDataSO>();

    [Header("Owned Balls")]
    [SerializeField] List<BallDataSO> ownedBalls = new List<BallDataSO>();

    [Header("Runtime")]
    [SerializeField] List<BallDataSO> drawPile = new List<BallDataSO>();
    [SerializeField] List<BallDataSO> discardPile = new List<BallDataSO>();
    [SerializeField] List<BallDataSO> currentCylinder = new List<BallDataSO>();

    [Header("Events")]
    [SerializeField] UnityEvent onDeckChanged = new UnityEvent();

    public IReadOnlyList<BallDataSO> CurrentHand => currentCylinder;
    public IReadOnlyList<BallDataSO> CurrentCylinder => currentCylinder;
    public IReadOnlyList<BallDataSO> OwnedBalls => ownedBalls;
    public IReadOnlyList<BallDataSO> StartingDeck => startingDeck;
    public IReadOnlyList<BallDataSO> DrawPile => drawPile;
    public IReadOnlyList<BallDataSO> DiscardPile => discardPile;
    public UnityEvent OnDeckChanged => onDeckChanged;
    public int DrawPileCount => drawPile.Count;
    public int DiscardPileCount => discardPile.Count;
    public int CurrentCylinderCount => currentCylinder.Count;
    public int OwnedBallCount => ownedBalls.Count;
    public bool IsAtCapacity => OwnedBallCount >= MaxOwnedBallCount;
    public bool CanDeleteOwnedBall => OwnedBallCount > MinimumOwnedBallCount;

    void Awake()
    {
        ResetDeck();
    }

    public void ResetDeck()
    {
        drawPile.Clear();
        discardPile.Clear();
        currentCylinder.Clear();
        ownedBalls.Clear();

        for (int i = 0; i < startingDeck.Count; i++)
        {
            BallDataSO ballData = startingDeck[i];
            if (ballData == null)
            {
                Debug.LogWarning($"[PlayerBallDeck] Starting deck entry {i} is empty.", this);
                continue;
            }

            if (ownedBalls.Count >= MaxOwnedBallCount)
            {
                Debug.LogWarning($"[PlayerBallDeck] Starting deck exceeds the maximum of {MaxOwnedBallCount}. Extra balls were ignored.", this);
                break;
            }

            ownedBalls.Add(ballData);
            drawPile.Add(ballData);
        }

        Shuffle(drawPile);
        OnDeckChanged.Invoke();
        LogPileState("Reset complete");
    }

    public bool RestoreDeck(
        IList<BallDataSO> restoredOwnedBalls,
        IList<BallDataSO> restoredDrawPile,
        IList<BallDataSO> restoredDiscardPile,
        IList<BallDataSO> restoredCurrentCylinder)
    {
        if (restoredOwnedBalls == null || restoredDrawPile == null ||
            restoredDiscardPile == null || restoredCurrentCylinder == null)
        {
            Debug.LogError("[PlayerBallDeck] Restore failed because one or more saved piles are null.", this);
            return false;
        }

        if (restoredOwnedBalls.Count > MaxOwnedBallCount)
        {
            Debug.LogError($"[PlayerBallDeck] Restore failed. Owned ball count exceeds {MaxOwnedBallCount}.", this);
            return false;
        }

        ownedBalls.Clear();
        drawPile.Clear();
        discardPile.Clear();
        currentCylinder.Clear();
        ownedBalls.AddRange(restoredOwnedBalls);
        drawPile.AddRange(restoredDrawPile);
        discardPile.AddRange(restoredDiscardPile);
        currentCylinder.AddRange(restoredCurrentCylinder);
        OnDeckChanged.Invoke();
        LogPileState("Restore complete");
        return true;
    }

    public void DrawBalls(int count)
    {
        if (count <= 0)
        {
            Debug.LogWarning($"[PlayerBallDeck] Draw count must be greater than 0. Count: {count}", this);
            return;
        }

        if (currentCylinder.Count > 0)
        {
            Debug.LogWarning($"[PlayerBallDeck] Cannot draw while currentCylinder is not empty. Count: {currentCylinder.Count}", this);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (!EnsureDrawablePile())
            {
                break;
            }

            BallDataSO drawnBall = drawPile[0];
            drawPile.RemoveAt(0);
            currentCylinder.Add(drawnBall);
        }

        LogPileState("Draw complete");
    }

    public void DiscardCurrentHand()
    {
        for (int i = 0; i < currentCylinder.Count; i++)
        {
            if (currentCylinder[i] != null)
            {
                discardPile.Add(currentCylinder[i]);
            }
        }

        currentCylinder.Clear();
        LogPileState("Discard complete");
    }

    public void AddBallToDeck(BallDataSO ballData)
    {
        TryAddBallToDeck(ballData);
    }

    public bool TryAddBallToDeck(BallDataSO ballData)
    {
        if (ballData == null)
        {
            Debug.LogWarning("[PlayerBallDeck] Cannot add null BallDataSO to deck.", this);
            return false;
        }

        if (IsAtCapacity)
        {
            Debug.Log($"[PlayerBallDeck] Cannot add {ballData.name}. Owned ball limit reached: {MaxOwnedBallCount}.", this);
            return false;
        }

        ownedBalls.Add(ballData);
        discardPile.Add(ballData);
        OnDeckChanged.Invoke();
        LogPileState("Add purchased ball");
        return true;
    }

    public bool IsOwnedBallAt(int index, BallDataSO expectedBallData)
    {
        return index >= 0 && index < ownedBalls.Count && ownedBalls[index] == expectedBallData;
    }

    public bool TryRemoveOwnedBallAt(int index, BallDataSO expectedBallData)
    {
        if (!CanDeleteOwnedBall)
        {
            Debug.Log($"[PlayerBallDeck] Cannot delete a ball while owning {MinimumOwnedBallCount} or fewer balls.", this);
            return false;
        }

        if (!IsOwnedBallAt(index, expectedBallData))
        {
            return false;
        }

        ownedBalls.RemoveAt(index);
        RemoveOneFromRuntimePiles(expectedBallData);
        OnDeckChanged.Invoke();
        Debug.Log($"[PlayerBallDeck] Removed owned ball at index {index}: {expectedBallData.name}", this);
        return true;
    }

    public bool TryInsertOwnedBall(int index, BallDataSO ballData)
    {
        if (ballData == null || IsAtCapacity)
        {
            return false;
        }

        ownedBalls.Insert(Mathf.Clamp(index, 0, ownedBalls.Count), ballData);
        discardPile.Add(ballData);
        OnDeckChanged.Invoke();
        return true;
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

        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);
        return drawPile.Count > 0;
    }

    void RemoveOneFromRuntimePiles(BallDataSO ballData)
    {
        if (drawPile.Remove(ballData) || discardPile.Remove(ballData))
        {
            return;
        }

        currentCylinder.Remove(ballData);
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
        Debug.Log($"[PlayerBallDeck] {context} | owned: {ownedBalls.Count}, drawPile: {drawPile.Count}, discardPile: {discardPile.Count}, currentCylinder: {currentCylinder.Count}", this);
    }
}
