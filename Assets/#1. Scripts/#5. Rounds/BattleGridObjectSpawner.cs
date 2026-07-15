using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BattleGridObjectSpawner : MonoBehaviour
{
    [Header("Floor")]
    [SerializeField] Tilemap floorTilemap;

    [Header("Spawn Objects")]
    [SerializeField] List<GameObject> spawnPrefabs = new List<GameObject>();
    [SerializeField, Min(0)] int minSpawnCount = 3;
    [SerializeField, Min(0)] int maxSpawnCount = 6;
    [SerializeField, Min(0f)] float minDistanceBetweenObjects = 1.5f;

    [Header("Collision Check")]
    [SerializeField, Min(0f)] float overlapCheckRadius = 0.45f;
    [SerializeField] LayerMask blockedLayerMask;

    [Header("Placement")]
    [SerializeField, Min(1)] int maxPlacementAttempts = 100;
    [SerializeField] Transform spawnedObjectParent;

    [Header("Debug")]
    [SerializeField] bool showGizmos = true;
    [SerializeField] Color validPositionGizmoColor = new Color(0.1f, 0.8f, 1f, 0.75f);
    [SerializeField] Color minDistanceGizmoColor = new Color(1f, 0.85f, 0.1f, 0.2f);

    readonly List<GameObject> spawnedObjects = new List<GameObject>();
    readonly List<Vector3> spawnedPositions = new List<Vector3>();
    int lastRequestedCount;
    int lastFailedAttempts;

    public IReadOnlyList<GameObject> SpawnedObjects
    {
        get
        {
            return spawnedObjects;
        }
    }

    public List<BattleGridObjectSaveData> CaptureSaveData()
    {
        List<BattleGridObjectSaveData> result = new List<BattleGridObjectSaveData>();
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            GameObject spawnedObject = spawnedObjects[i];
            if (spawnedObject == null)
            {
                continue;
            }

            Bumper bumper = spawnedObject.GetComponent<Bumper>();
            result.Add(new BattleGridObjectSaveData
            {
                prefabId = spawnedObject.name,
                position = spawnedObject.transform.position,
                rotationEuler = spawnedObject.transform.eulerAngles,
                localScale = bumper != null ? bumper.OriginalScale : spawnedObject.transform.localScale,
                active = spawnedObject.activeSelf
            });
        }

        return result;
    }

    public bool CanRestoreObjects(IList<BattleGridObjectSaveData> savedObjects, out string error)
    {
        error = string.Empty;
        if (savedObjects == null)
        {
            error = "Battle Grid object list is null.";
            return false;
        }

        for (int i = 0; i < savedObjects.Count; i++)
        {
            BattleGridObjectSaveData savedObject = savedObjects[i];
            if (savedObject == null || string.IsNullOrWhiteSpace(savedObject.prefabId))
            {
                error = $"Battle Grid object {i} has no prefab ID.";
                return false;
            }

            if (FindSpawnPrefab(savedObject.prefabId) == null)
            {
                error = $"Battle Grid object prefab ID '{savedObject.prefabId}' was not found.";
                return false;
            }
        }

        return true;
    }

    public bool RestoreObjects(IList<BattleGridObjectSaveData> savedObjects)
    {
        if (!CanRestoreObjects(savedObjects, out string error))
        {
            Debug.LogError($"[BattleGridObjectSpawner] Restore failed. {error}", this);
            return false;
        }

        ClearSpawnedObjects();
        Transform parent = spawnedObjectParent != null ? spawnedObjectParent : transform;
        for (int i = 0; i < savedObjects.Count; i++)
        {
            BattleGridObjectSaveData savedObject = savedObjects[i];
            GameObject prefab = FindSpawnPrefab(savedObject.prefabId);
            GameObject instance = Instantiate(
                prefab,
                savedObject.position,
                Quaternion.Euler(savedObject.rotationEuler),
                parent);
            instance.name = prefab.name;
            instance.transform.localScale = savedObject.localScale;
            instance.SetActive(savedObject.active);
            spawnedObjects.Add(instance);
            spawnedPositions.Add(savedObject.position);
        }

        lastRequestedCount = spawnedObjects.Count;
        lastFailedAttempts = 0;
        return true;
    }

    public GameObject FindSpawnPrefab(string prefabId)
    {
        if (spawnPrefabs == null || string.IsNullOrWhiteSpace(prefabId))
        {
            return null;
        }

        for (int i = 0; i < spawnPrefabs.Count; i++)
        {
            GameObject prefab = spawnPrefabs[i];
            if (prefab != null && prefab.name == prefabId)
            {
                return prefab;
            }
        }

        return null;
    }

    public void SpawnObjects()
    {
        ClearSpawnedObjects();

        if (!CanSpawn())
        {
            Debug.LogWarning("[BattleGridObjectSpawner] Spawn skipped because required settings are missing.", this);
            return;
        }

        List<Vector3Int> floorCells = GetFloorCells();
        if (floorCells.Count <= 0)
        {
            Debug.LogWarning("[BattleGridObjectSpawner] Spawn skipped because floor tilemap has no valid floor tiles.", this);
            return;
        }

        int minCount = Mathf.Max(0, minSpawnCount);
        int maxCount = Mathf.Max(minCount, maxSpawnCount);
        lastRequestedCount = Random.Range(minCount, maxCount + 1);
        lastFailedAttempts = 0;

        for (int i = 0; i < lastRequestedCount; i++)
        {
            if (!TryFindSpawnPosition(floorCells, out Vector3 spawnPosition))
            {
                Debug.LogWarning($"[BattleGridObjectSpawner] Failed to find valid position for object {i + 1}/{lastRequestedCount}. Attempts: {maxPlacementAttempts}", this);
                break;
            }

            GameObject prefab = PickRandomPrefab();
            if (prefab == null)
            {
                Debug.LogWarning("[BattleGridObjectSpawner] Null prefab was selected. Spawn stopped.", this);
                break;
            }

            Transform parent = spawnedObjectParent != null ? spawnedObjectParent : transform;
            GameObject spawnedObject = Instantiate(prefab, spawnPosition, Quaternion.identity, parent);
            spawnedObject.name = prefab.name;
            spawnedObjects.Add(spawnedObject);
            spawnedPositions.Add(spawnPosition);
        }

        ValidateSpawnedObjects();
    }

    [ContextMenu("Validate Spawned Objects")]
    public void ValidateSpawnedObjects()
    {
        bool isValid = true;

        if (floorTilemap == null)
        {
            Debug.LogWarning("[BattleGridObjectSpawner] Validation failed. Floor Tilemap is missing.", this);
            return;
        }

        if (spawnPrefabs == null || spawnPrefabs.Count <= 0)
        {
            Debug.LogWarning("[BattleGridObjectSpawner] Validation warning. Spawn prefab list is empty.", this);
            isValid = false;
        }

        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            GameObject spawnedObject = spawnedObjects[i];
            if (spawnedObject == null)
            {
                Debug.LogWarning($"[BattleGridObjectSpawner] Validation failed. Spawned object {i} is null.", this);
                isValid = false;
                continue;
            }

            Vector3 position = spawnedObject.transform.position;
            if (!IsPositionOnFloor(position))
            {
                Debug.LogWarning($"[BattleGridObjectSpawner] Validation failed. {spawnedObject.name} is not on a floor tile. Position: {position}", spawnedObject);
                isValid = false;
            }

            if (OverlapsBlockedCollider(position, spawnedObject))
            {
                Debug.LogWarning($"[BattleGridObjectSpawner] Validation failed. {spawnedObject.name} overlaps a blocked collider. Position: {position}", spawnedObject);
                isValid = false;
            }

            for (int j = i + 1; j < spawnedObjects.Count; j++)
            {
                GameObject otherObject = spawnedObjects[j];
                if (otherObject == null)
                {
                    continue;
                }

                float distance = Vector2.Distance(position, otherObject.transform.position);
                if (distance < minDistanceBetweenObjects)
                {
                    Debug.LogWarning($"[BattleGridObjectSpawner] Validation failed. {spawnedObject.name} and {otherObject.name} are too close. Distance: {distance}, Required: {minDistanceBetweenObjects}", this);
                    isValid = false;
                }

                if (ObjectsOverlap(spawnedObject, otherObject))
                {
                    Debug.LogWarning($"[BattleGridObjectSpawner] Validation failed. {spawnedObject.name} overlaps {otherObject.name}.", this);
                    isValid = false;
                }
            }
        }

        if (lastRequestedCount > 0 && spawnedObjects.Count != lastRequestedCount)
        {
            Debug.LogWarning($"[BattleGridObjectSpawner] Validation warning. Requested: {lastRequestedCount}, Actual: {spawnedObjects.Count}, Failed Attempts: {lastFailedAttempts}", this);
            isValid = false;
        }

        if (isValid)
        {
            Debug.Log($"[BattleGridObjectSpawner] Validation succeeded. Requested: {lastRequestedCount}, Actual: {spawnedObjects.Count}, Failed Attempts: {lastFailedAttempts}", this);
        }
    }

    [ContextMenu("Clear Spawned Objects")]
    public void ClearSpawnedObjects()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            GameObject spawnedObject = spawnedObjects[i];
            if (spawnedObject == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                spawnedObject.SetActive(false);
                Destroy(spawnedObject);
            }
            else
            {
                DestroyImmediate(spawnedObject);
            }
        }

        spawnedObjects.Clear();
        spawnedPositions.Clear();
        lastRequestedCount = 0;
        lastFailedAttempts = 0;
    }

    [ContextMenu("Clear And Respawn Objects")]
    public void ClearAndRespawnObjects()
    {
        SpawnObjects();
    }

    bool TryFindSpawnPosition(List<Vector3Int> floorCells, out Vector3 spawnPosition)
    {
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            Vector3Int cell = floorCells[Random.Range(0, floorCells.Count)];
            Vector3 candidate = floorTilemap.GetCellCenterWorld(cell);

            if (IsValidSpawnPosition(candidate))
            {
                spawnPosition = candidate;
                return true;
            }

            lastFailedAttempts++;
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    bool IsValidSpawnPosition(Vector3 position)
    {
        if (!IsPositionOnFloor(position))
        {
            return false;
        }

        if (!HasMinimumDistance(position))
        {
            return false;
        }

        if (OverlapsBlockedCollider(position, null))
        {
            return false;
        }

        if (OverlapsSpawnedObject(position))
        {
            return false;
        }

        return true;
    }

    public bool IsPositionOnFloor(Vector3 worldPosition)
    {
        if (floorTilemap == null)
        {
            return false;
        }

        Vector3Int cell = floorTilemap.WorldToCell(worldPosition);
        return floorTilemap.HasTile(cell);
    }

    bool HasMinimumDistance(Vector3 position)
    {
        float minDistanceSqr = minDistanceBetweenObjects * minDistanceBetweenObjects;

        for (int i = 0; i < spawnedPositions.Count; i++)
        {
            if ((spawnedPositions[i] - position).sqrMagnitude < minDistanceSqr)
            {
                return false;
            }
        }

        return true;
    }

    bool OverlapsBlockedCollider(Vector3 position, GameObject ignoredObject)
    {
        if (overlapCheckRadius <= 0f)
        {
            return false;
        }

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, overlapCheckRadius, blockedLayerMask);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D overlap = overlaps[i];
            if (overlap == null)
            {
                continue;
            }

            if (ignoredObject != null && overlap.transform.IsChildOf(ignoredObject.transform))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    bool OverlapsSpawnedObject(Vector3 position)
    {
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            GameObject spawnedObject = spawnedObjects[i];
            if (spawnedObject == null)
            {
                continue;
            }

            Collider2D[] colliders = spawnedObject.GetComponentsInChildren<Collider2D>();
            for (int j = 0; j < colliders.Length; j++)
            {
                Collider2D collider = colliders[j];
                if (collider == null)
                {
                    continue;
                }

                Vector2 closestPoint = collider.ClosestPoint(position);
                if (Vector2.Distance(closestPoint, position) < overlapCheckRadius)
                {
                    return true;
                }
            }
        }

        return false;
    }

    bool ObjectsOverlap(GameObject firstObject, GameObject secondObject)
    {
        Collider2D[] firstColliders = firstObject.GetComponentsInChildren<Collider2D>();
        Collider2D[] secondColliders = secondObject.GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < firstColliders.Length; i++)
        {
            Collider2D first = firstColliders[i];
            if (first == null)
            {
                continue;
            }

            for (int j = 0; j < secondColliders.Length; j++)
            {
                Collider2D second = secondColliders[j];
                if (second == null)
                {
                    continue;
                }

                if (first.bounds.Intersects(second.bounds))
                {
                    return true;
                }
            }
        }

        return false;
    }

    List<Vector3Int> GetFloorCells()
    {
        List<Vector3Int> floorCells = new List<Vector3Int>();
        if (floorTilemap == null)
        {
            return floorCells;
        }

        BoundsInt bounds = floorTilemap.cellBounds;
        foreach (Vector3Int position in bounds.allPositionsWithin)
        {
            if (floorTilemap.HasTile(position))
            {
                floorCells.Add(position);
            }
        }

        return floorCells;
    }

    GameObject PickRandomPrefab()
    {
        if (spawnPrefabs == null || spawnPrefabs.Count <= 0)
        {
            return null;
        }

        for (int attempts = 0; attempts < spawnPrefabs.Count; attempts++)
        {
            GameObject prefab = spawnPrefabs[Random.Range(0, spawnPrefabs.Count)];
            if (prefab != null)
            {
                return prefab;
            }
        }

        for (int i = 0; i < spawnPrefabs.Count; i++)
        {
            if (spawnPrefabs[i] != null)
            {
                return spawnPrefabs[i];
            }
        }

        return null;
    }

    bool CanSpawn()
    {
        if (floorTilemap == null)
        {
            return false;
        }

        if (spawnPrefabs == null || spawnPrefabs.Count <= 0)
        {
            return false;
        }

        for (int i = 0; i < spawnPrefabs.Count; i++)
        {
            if (spawnPrefabs[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    void OnValidate()
    {
        minSpawnCount = Mathf.Max(0, minSpawnCount);
        maxSpawnCount = Mathf.Max(minSpawnCount, maxSpawnCount);
        minDistanceBetweenObjects = Mathf.Max(0f, minDistanceBetweenObjects);
        overlapCheckRadius = Mathf.Max(0f, overlapCheckRadius);
        maxPlacementAttempts = Mathf.Max(1, maxPlacementAttempts);

        if (floorTilemap == null)
        {
            Debug.LogWarning("[BattleGridObjectSpawner] Floor Tilemap is not connected.", this);
        }

        if (spawnPrefabs == null || spawnPrefabs.Count <= 0)
        {
            Debug.LogWarning("[BattleGridObjectSpawner] Spawn Prefabs list is empty.", this);
        }
        else
        {
            for (int i = 0; i < spawnPrefabs.Count; i++)
            {
                if (spawnPrefabs[i] == null)
                {
                    Debug.LogWarning($"[BattleGridObjectSpawner] Spawn Prefabs contains a null entry at index {i}.", this);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos)
        {
            return;
        }

        Gizmos.color = validPositionGizmoColor;
        for (int i = 0; i < spawnedPositions.Count; i++)
        {
            Gizmos.DrawSphere(spawnedPositions[i], Mathf.Max(0.05f, overlapCheckRadius * 0.2f));
        }

        Gizmos.color = minDistanceGizmoColor;
        for (int i = 0; i < spawnedPositions.Count; i++)
        {
            Gizmos.DrawWireSphere(spawnedPositions[i], minDistanceBetweenObjects);
        }
    }
}
