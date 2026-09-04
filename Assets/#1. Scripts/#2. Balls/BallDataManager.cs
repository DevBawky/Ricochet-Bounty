using UnityEngine;

// BallDataManager는 요청서의 BallDataHolder와 같은 역할을 하는 기존 컴포넌트입니다.
// 실제 공 프리팹에 붙어서 "이 공이 어떤 BallDataSO를 사용할지"를 들고 있습니다.
// BallDataSO 자체에는 현재 내구도나 쿨다운 같은 런타임 값을 저장하지 않습니다.
public class BallDataManager : MonoBehaviour
{
    [SerializeField] BallDataSO ballData;

    WorldEffectPool worldEffectPool;

    public BallDataSO BallData
    {
        get
        {
            return ballData;
        }
    }

    public void SetBallData(BallDataSO newBallData)
    {
        // 덱에서 뽑은 BallDataSO를 생성된 공에 주입합니다.
        // 요청서의 BallDataHolder.SetBallData와 같은 역할을 기존 구조에 확장한 메서드입니다.
        ballData = newBallData;

        if (ballData == null)
        {
            Debug.LogWarning($"[BallDataManager] {name}에 null BallDataSO가 설정되었습니다.", this);
            return;
        }

        Debug.Log($"[BallDataManager] {name}에 BallDataSO가 설정되었습니다: {ballData.name}", this);
    }

    public bool ValidateData()
    {
        if (ballData != null)
        {
            return true;
        }

        Debug.LogWarning($"[BallDataManager] {name}에 BallDataSO가 연결되어 있지 않습니다.", this);
        return false;
    }

    internal void SetWorldEffectPool(WorldEffectPool newWorldEffectPool)
    {
        worldEffectPool = newWorldEffectPool;
    }

    public void SpawnCollisionEffect(Vector3 worldPosition)
    {
        SpawnEffect(
            ballData != null ? ballData.CollisionEffectPrefab : null,
            worldPosition,
            ballData != null ? ballData.VisualEffectLifetime : 0f
        );
    }

    public void SpawnDurabilityDepletedEffect(Vector3 worldPosition)
    {
        SpawnEffect(
            ballData != null ? ballData.DurabilityDepletedEffectPrefab : null,
            worldPosition,
            ballData != null ? ballData.VisualEffectLifetime : 0f
        );
    }

    void SpawnEffect(GameObject effectPrefab, Vector3 worldPosition, float lifetime)
    {
        if (effectPrefab == null)
        {
            return;
        }

        ResolveWorldEffectPool();
        if (worldEffectPool != null)
        {
            worldEffectPool.Play(effectPrefab, worldPosition, lifetime);
            return;
        }

        // A scene-placed/non-pooled ball can still use the existing public API safely.
        GameObject effectInstance = Instantiate(effectPrefab, worldPosition, Quaternion.identity);
        if (lifetime > 0f)
        {
            Destroy(effectInstance, lifetime);
        }
    }

    void ResolveWorldEffectPool()
    {
        if (worldEffectPool != null)
        {
            return;
        }

        BallPoolHandle poolHandle = GetComponent<BallPoolHandle>();
        if (poolHandle != null && poolHandle.Owner != null)
        {
            BallSpawner spawner = FindFirstObjectByType<BallSpawner>();
            if (spawner != null)
            {
                worldEffectPool = spawner.EffectPool;
            }

            return;
        }

        BallSpawner fallbackSpawner = FindFirstObjectByType<BallSpawner>();
        if (fallbackSpawner != null)
        {
            worldEffectPool = fallbackSpawner.EffectPool;
        }
    }
}
