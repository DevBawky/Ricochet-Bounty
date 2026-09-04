using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public sealed class BallPool : IDisposable
{
    readonly GameObject prefab;
    readonly Transform poolRoot;
    readonly WorldEffectPool worldEffectPool;
    readonly ObjectPool<GameObject> pool;
    readonly HashSet<GameObject> instances = new HashSet<GameObject>();
    readonly List<GameObject> pendingReleases = new List<GameObject>();

    bool isDisposed;

    public BallPool(GameObject prefab, Transform poolRoot, WorldEffectPool worldEffectPool)
    {
        this.prefab = prefab;
        this.poolRoot = poolRoot;
        this.worldEffectPool = worldEffectPool;

        pool = new ObjectPool<GameObject>(
            CreateInstance,
            OnGet,
            OnRelease,
            OnDestroyInstance,
            true,
            8,
            64);
    }

    public GameObject GetPrimary(
        BallDataSO ballData,
        Vector3 position,
        Vector2 direction,
        float speed)
    {
        return GetConfigured(
            ballData,
            position,
            direction,
            speed,
            ballData != null ? $"{ballData.name} Ball" : "Ball",
            null,
            false,
            false,
            true);
    }

    public GameObject GetSplit(
        GameObject source,
        Vector3 position,
        Vector2 direction,
        float speed,
        bool isColonyChild)
    {
        if (source == null)
        {
            return null;
        }

        BallDataManager sourceDataManager = source.GetComponent<BallDataManager>();
        BallDataSO ballData = sourceDataManager != null ? sourceDataManager.BallData : null;
        return GetConfigured(
            ballData,
            position,
            direction,
            speed,
            $"{source.name} Split",
            source,
            true,
            isColonyChild,
            false);
    }

    public bool Release(GameObject instance)
    {
        if (isDisposed || instance == null)
        {
            return false;
        }

        BallPoolHandle handle = instance.GetComponent<BallPoolHandle>();
        if (handle == null || !handle.TryMarkReturned(this))
        {
            return false;
        }

        DeactivateForRelease(instance);
        pendingReleases.Add(instance);
        return true;
    }

    public void FlushReleases()
    {
        if (isDisposed || pendingReleases.Count == 0)
        {
            return;
        }

        for (int i = 0; i < pendingReleases.Count; i++)
        {
            GameObject instance = pendingReleases[i];
            if (instance != null)
            {
                pool.Release(instance);
            }
        }

        pendingReleases.Clear();
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        pendingReleases.Clear();
        GameObject[] snapshot = new GameObject[instances.Count];
        instances.CopyTo(snapshot);
        instances.Clear();

        for (int i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] != null)
            {
                UnityEngine.Object.Destroy(snapshot[i]);
            }
        }
    }

    GameObject GetConfigured(
        BallDataSO ballData,
        Vector3 position,
        Vector2 direction,
        float speed,
        string instanceName,
        GameObject identitySource,
        bool isSplitBall,
        bool isColonyChild,
        bool triggerSpawnEffects)
    {
        if (isDisposed || prefab == null || ballData == null)
        {
            return null;
        }

        GameObject instance = pool.Get();
        BallPoolHandle handle = instance.GetComponent<BallPoolHandle>();

        try
        {
            instance.transform.SetParent(null, true);
            instance.transform.SetPositionAndRotation(position, Quaternion.identity);
            instance.name = instanceName;

            if (identitySource != null)
            {
                int ballLayer = LayerMask.NameToLayer("Ball");
                CopyIdentity(
                    identitySource.transform,
                    instance.transform,
                    ballLayer >= 0 ? ballLayer : identitySource.layer);
            }

            BallDataManager dataManager = instance.GetComponent<BallDataManager>();
            if (dataManager != null)
            {
                dataManager.SetWorldEffectPool(worldEffectPool);
                dataManager.SetBallData(ballData);
            }

            handle?.ApplyVisual(ballData);

            Ball ball = instance.GetComponent<Ball>();
            ball?.PrepareForPoolSpawn();

            BallRuntimeStatus runtimeStatus = instance.GetComponent<BallRuntimeStatus>();
            if (runtimeStatus != null)
            {
                if (isSplitBall && isColonyChild)
                {
                    runtimeStatus.InitializeAsChild(1);
                }
                else
                {
                    runtimeStatus.Initialize();
                }
            }

            BallEffectController effectController = instance.GetComponent<BallEffectController>();
            effectController?.PrepareForPoolSpawn(isSplitBall, isColonyChild);

            SetActiveWithoutAutomaticSpawnEffect(instance, true);

            if (triggerSpawnEffects && effectController != null)
            {
                effectController.TriggerSpawnEffects();
            }

            if (handle == null || !handle.IsRentedFrom(this) || !instance.activeInHierarchy)
            {
                return instance;
            }

            if (ball != null)
            {
                if (isSplitBall)
                {
                    ball.InitializeAsSplitBall(direction, speed);
                }
                else
                {
                    ball.Launch(direction, speed);
                }
            }
            else
            {
                Rigidbody2D rigidbody2D = instance.GetComponent<Rigidbody2D>();
                if (rigidbody2D != null)
                {
                    rigidbody2D.linearVelocity = direction * Mathf.Max(0f, speed);
                }
            }

            return instance;
        }
        catch
        {
            if (handle != null && handle.IsRentedFrom(this))
            {
                Release(instance);
            }

            throw;
        }
    }

    GameObject CreateInstance()
    {
        GameObject instance;
        BallEffectController.SuppressSpawnEffectsOnEnable = true;
        try
        {
            instance = UnityEngine.Object.Instantiate(prefab);
        }
        finally
        {
            BallEffectController.SuppressSpawnEffectsOnEnable = false;
        }

        instance.SetActive(false);
        instance.transform.SetParent(poolRoot, false);

        BallPoolHandle handle = instance.GetComponent<BallPoolHandle>();
        if (handle == null)
        {
            handle = instance.AddComponent<BallPoolHandle>();
        }

        handle.CaptureDefaults();
        instances.Add(instance);
        return instance;
    }

    void OnGet(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        BallPoolHandle handle = instance.GetComponent<BallPoolHandle>();
        handle?.MarkRented(this);
    }

    void OnRelease(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(poolRoot, false);
    }

    void DeactivateForRelease(GameObject instance)
    {
        Ball ball = instance.GetComponent<Ball>();
        ball?.PrepareForPoolRelease();
        instance.SetActive(false);
        instance.transform.SetParent(poolRoot, false);
    }

    void OnDestroyInstance(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        instances.Remove(instance);
        UnityEngine.Object.Destroy(instance);
    }

    static void SetActiveWithoutAutomaticSpawnEffect(GameObject instance, bool isActive)
    {
        BallEffectController.SuppressSpawnEffectsOnEnable = true;
        try
        {
            instance.SetActive(isActive);
        }
        finally
        {
            BallEffectController.SuppressSpawnEffectsOnEnable = false;
        }
    }

    static void CopyIdentity(Transform source, Transform target, int ballLayer)
    {
        target.gameObject.tag = source.gameObject.tag;
        target.gameObject.layer = ballLayer;

        int childCount = Mathf.Min(source.childCount, target.childCount);
        for (int i = 0; i < childCount; i++)
        {
            CopyIdentity(source.GetChild(i), target.GetChild(i), ballLayer);
        }
    }
}

public sealed class BallPoolHandle : MonoBehaviour
{
    BallPool owner;
    SpriteRenderer spriteRenderer;
    Sprite defaultSprite;
    bool defaultsCaptured;
    bool isRented;

    public BallPool Owner => owner;

    public bool Release()
    {
        return owner != null && owner.Release(gameObject);
    }

    internal void CaptureDefaults()
    {
        if (defaultsCaptured)
        {
            return;
        }

        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        defaultSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        defaultsCaptured = true;
    }

    internal void ApplyVisual(BallDataSO ballData)
    {
        CaptureDefaults();
        if (spriteRenderer == null || ballData == null)
        {
            return;
        }

        spriteRenderer.color = ballData.BallColor;
        spriteRenderer.sprite = ballData.BallSprite != null ? ballData.BallSprite : defaultSprite;
    }

    internal void MarkRented(BallPool newOwner)
    {
        owner = newOwner;
        isRented = true;
    }

    internal bool TryMarkReturned(BallPool expectedOwner)
    {
        if (!isRented || owner != expectedOwner)
        {
            return false;
        }

        isRented = false;
        return true;
    }

    internal bool IsRentedFrom(BallPool expectedOwner)
    {
        return isRented && owner == expectedOwner;
    }
}
