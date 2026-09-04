using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public sealed class WorldEffectPool : IDisposable
{
    sealed class PrefabPool
    {
        readonly GameObject prefab;
        readonly Transform poolRoot;
        readonly ObjectPool<GameObject> pool;
        readonly HashSet<GameObject> instances = new HashSet<GameObject>();
        readonly Dictionary<GameObject, int> activeLeases = new Dictionary<GameObject, int>();

        int nextLeaseId;
        bool isDisposed;

        public PrefabPool(GameObject prefab, Transform poolRoot)
        {
            this.prefab = prefab;
            this.poolRoot = poolRoot;
            pool = new ObjectPool<GameObject>(
                CreateInstance,
                OnGet,
                OnRelease,
                OnDestroyInstance,
                true,
                4,
                64);
        }

        public GameObject Get(Vector3 position, out int leaseId)
        {
            leaseId = 0;
            if (isDisposed)
            {
                return null;
            }

            GameObject instance = pool.Get();
            instance.transform.SetParent(null, true);
            instance.transform.SetPositionAndRotation(position, Quaternion.identity);

            leaseId = ++nextLeaseId;
            activeLeases[instance] = leaseId;
            instance.SetActive(true);
            RestartEffect(instance);
            return instance;
        }

        public void Release(GameObject instance, int leaseId)
        {
            if (isDisposed || instance == null ||
                !activeLeases.TryGetValue(instance, out int activeLeaseId) ||
                activeLeaseId != leaseId)
            {
                return;
            }

            activeLeases.Remove(instance);
            pool.Release(instance);
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            activeLeases.Clear();

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

        GameObject CreateInstance()
        {
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            instance.SetActive(false);
            instance.transform.SetParent(poolRoot, false);
            instances.Add(instance);
            return instance;
        }

        void OnGet(GameObject instance)
        {
        }

        void OnRelease(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            StopEffect(instance);
            instance.SetActive(false);
            instance.transform.SetParent(poolRoot, false);
        }

        void OnDestroyInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            activeLeases.Remove(instance);
            instances.Remove(instance);
            UnityEngine.Object.Destroy(instance);
        }

        static void RestartEffect(GameObject instance)
        {
            Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                animators[i].Rebind();
                animators[i].Update(0f);
            }

            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Clear(true);
                particles[i].Play(true);
            }
        }

        static void StopEffect(GameObject instance)
        {
            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    readonly MonoBehaviour coroutineHost;
    readonly Transform poolRoot;
    readonly Dictionary<GameObject, PrefabPool> pools = new Dictionary<GameObject, PrefabPool>();

    bool isDisposed;

    public WorldEffectPool(MonoBehaviour coroutineHost, Transform poolRoot)
    {
        this.coroutineHost = coroutineHost;
        this.poolRoot = poolRoot;
    }

    public GameObject Play(GameObject prefab, Vector3 position, float lifetime)
    {
        if (isDisposed || prefab == null || coroutineHost == null)
        {
            return null;
        }

        if (!pools.TryGetValue(prefab, out PrefabPool prefabPool))
        {
            prefabPool = new PrefabPool(prefab, poolRoot);
            pools.Add(prefab, prefabPool);
        }

        GameObject instance = prefabPool.Get(position, out int leaseId);
        if (instance != null && lifetime > 0f)
        {
            coroutineHost.StartCoroutine(ReturnAfterLifetime(prefabPool, instance, leaseId, lifetime));
        }

        return instance;
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        foreach (PrefabPool prefabPool in pools.Values)
        {
            prefabPool.Dispose();
        }

        pools.Clear();
    }

    static IEnumerator ReturnAfterLifetime(
        PrefabPool prefabPool,
        GameObject instance,
        int leaseId,
        float lifetime)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, lifetime));
        prefabPool.Release(instance, leaseId);
    }
}
