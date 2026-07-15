using System.Collections.Generic;
using UnityEngine;

// Scene-owned registry for every live Ball, including balls created by effects.
// Add this component to an existing scene manager; it is never created at runtime.
public class BallRegistry : MonoBehaviour
{
    [SerializeField, Min(1)] int maxActiveBallCount = 12;

    readonly HashSet<Ball> activeBalls = new HashSet<Ball>();

    public int ActiveBallCount
    {
        get
        {
            RemoveDestroyedEntries();
            return activeBalls.Count;
        }
    }

    public int MaxActiveBallCount => Mathf.Max(1, maxActiveBallCount);
    public int AvailableSlots => Mathf.Max(0, MaxActiveBallCount - ActiveBallCount);

    public bool Register(Ball ball)
    {
        if (ball == null)
        {
            return false;
        }

        // HashSet.Add returns false for duplicate registrations.
        return activeBalls.Add(ball);
    }

    public bool Unregister(Ball ball)
    {
        if (ball == null)
        {
            return false;
        }

        // HashSet.Remove returns false for duplicate removals.
        return activeBalls.Remove(ball);
    }

    public bool HasCapacity(int additionalBallCount = 1)
    {
        return additionalBallCount >= 0 && ActiveBallCount + additionalBallCount <= MaxActiveBallCount;
    }

    public void Clear()
    {
        activeBalls.Clear();
    }

    void RemoveDestroyedEntries()
    {
        activeBalls.RemoveWhere(ball => ball == null);
    }
}
