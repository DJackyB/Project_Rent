using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize;
        public bool canGrow = true;
    }

    public interface IPoolableObject
    {
        void OnObjectSpawn();
        void OnObjectDespawn();
    }

    public static ObjectPooler Instance;

    [SerializeField] public List<Pool> poolsToCreate = new();

    private readonly Dictionary<string, Queue<GameObject>> _poolDictionary = new();
    private readonly Dictionary<string, Transform> _poolParents = new();
    private readonly Dictionary<GameObject, string> _spawnedObjectsTag = new();
    private readonly Dictionary<string, Pool> _poolConfigCache = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (var pool in poolsToCreate)
        {
            if (pool == null || string.IsNullOrEmpty(pool.tag) || pool.prefab == null)
            {
                Debug.LogWarning("[ObjectPooler] Skipped an invalid pool config.");
                continue;
            }

            CreateAndFillPool(pool.tag, pool.prefab, pool.initialSize, pool.canGrow);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation, Transform desiredParent = null)
    {
        if (string.IsNullOrEmpty(tag))
        {
            Debug.LogError("[ObjectPooler] SpawnFromPool called with an empty tag.");
            return null;
        }

        if (!_poolDictionary.ContainsKey(tag) || _poolDictionary[tag] == null)
        {
            if (!TryGetPoolConfig(tag, out var config) || config.prefab == null)
            {
                Debug.LogError($"[ObjectPooler] No pool config found for tag '{tag}'.");
                return null;
            }

            CreateAndFillPool(config.tag, config.prefab, config.initialSize, config.canGrow);
        }

        var queue = _poolDictionary[tag];
        var poolParent = _poolParents.TryGetValue(tag, out var parent) ? parent : transform;

        GameObject objectToSpawn = null;
        if (queue.Count > 0)
        {
            objectToSpawn = queue.Dequeue();
        }
        else if (TryGetPoolConfig(tag, out var config) && config.canGrow && config.prefab != null)
        {
            objectToSpawn = Instantiate(config.prefab);
            objectToSpawn.SetActive(false);
        }
        else
        {
            return null;
        }

        if (objectToSpawn == null)
        {
            return null;
        }

        objectToSpawn.transform.SetParent(desiredParent != null ? desiredParent : poolParent, false);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        _spawnedObjectsTag[objectToSpawn] = tag;
        NotifySpawn(objectToSpawn);
        return objectToSpawn;
    }

    public void ReturnToPool(GameObject objectToReturn)
    {
        if (objectToReturn == null)
        {
            return;
        }

        if (!_spawnedObjectsTag.TryGetValue(objectToReturn, out var tag))
        {
            if (objectToReturn.activeSelf)
            {
                Debug.LogWarning($"[ObjectPooler] Object '{objectToReturn.name}' was not tracked by the pooler. Destroying it.");
                Destroy(objectToReturn);
            }

            return;
        }

        if (!_poolDictionary.TryGetValue(tag, out var queue) || !_poolParents.TryGetValue(tag, out var parent))
        {
            Debug.LogWarning($"[ObjectPooler] Missing pool for tag '{tag}'. Destroying '{objectToReturn.name}'.");
            Destroy(objectToReturn);
            _spawnedObjectsTag.Remove(objectToReturn);
            return;
        }

        NotifyDespawn(objectToReturn);
        objectToReturn.SetActive(false);
        objectToReturn.transform.SetParent(parent, false);

        if (!queue.Contains(objectToReturn))
        {
            queue.Enqueue(objectToReturn);
        }

        _spawnedObjectsTag.Remove(objectToReturn);
    }

    public bool CreateRuntimePool(string tag, GameObject prefab, int initialSize = 1, bool canGrow = true)
    {
        if (prefab == null)
        {
            Debug.LogError($"[ObjectPooler] Failed to create runtime pool for tag '{tag}': prefab is null.");
            return false;
        }

        if (_poolDictionary.ContainsKey(tag))
        {
            _poolConfigCache[tag] = new Pool
            {
                tag = tag,
                prefab = prefab,
                initialSize = initialSize,
                canGrow = canGrow
            };
            return true;
        }

        CreateAndRegisterPool(new Pool
        {
            tag = tag,
            prefab = prefab,
            initialSize = initialSize,
            canGrow = canGrow
        });
        return true;
    }

    private bool TryGetPoolConfig(string tag, out Pool poolConfig)
    {
        poolConfig = null;

        if (!string.IsNullOrEmpty(tag) && _poolConfigCache.TryGetValue(tag, out poolConfig) && poolConfig != null)
        {
            return true;
        }

        if (poolsToCreate != null)
        {
            poolConfig = poolsToCreate.Find(p => p != null && p.tag == tag);
            if (poolConfig != null)
            {
                return true;
            }
        }

        return false;
    }

    private void CreateAndFillPool(string tag, GameObject prefab, int initialSize, bool canGrowConfigValue)
    {
        if (_poolDictionary.ContainsKey(tag))
        {
            return;
        }

        var parentObject = new GameObject($"{tag} Pool");
        parentObject.transform.SetParent(transform, false);
        _poolParents[tag] = parentObject.transform;

        var objectQueue = new Queue<GameObject>();
        for (int i = 0; i < initialSize; i++)
        {
            var obj = Instantiate(prefab);
            obj.SetActive(false);
            obj.transform.SetParent(parentObject.transform, false);
            objectQueue.Enqueue(obj);
        }

        _poolDictionary[tag] = objectQueue;
        _poolConfigCache[tag] = new Pool
        {
            tag = tag,
            prefab = prefab,
            initialSize = initialSize,
            canGrow = canGrowConfigValue
        };
    }

    private void CreateAndRegisterPool(Pool poolConfig)
    {
        if (poolConfig == null || string.IsNullOrEmpty(poolConfig.tag) || poolConfig.prefab == null)
        {
            return;
        }

        CreateAndFillPool(poolConfig.tag, poolConfig.prefab, poolConfig.initialSize, poolConfig.canGrow);
    }

    private static void NotifySpawn(GameObject spawnedObject)
    {
        var poolables = spawnedObject.GetComponents<IPoolableObject>();
        foreach (var poolable in poolables)
        {
            poolable.OnObjectSpawn();
        }
    }

    private static void NotifyDespawn(GameObject objectToReturn)
    {
        var poolables = objectToReturn.GetComponents<IPoolableObject>();
        foreach (var poolable in poolables)
        {
            poolable.OnObjectDespawn();
        }
    }
}
