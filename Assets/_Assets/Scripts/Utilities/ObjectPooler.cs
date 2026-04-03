using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对象池管理器。
/// 用于复用 GameObject 实例，避免频繁 Instantiate/Destroy 造成的性能开销（GC 压力）。
///
/// 使用方式：
/// 1. Editor 中配置 poolsToCreate 列表，指定各池的标签、预制体、初始大小、是否可增长
/// 2. 运行时通过 SpawnFromPool(tag, pos, rot) 获取对象实例
/// 3. 用完后通过 ReturnToPool(obj) 归还对象
/// 4. 对象实现 IPoolableObject 接口以在 Spawn/Despawn 时进行初始化/清理
///
/// 扩展性：
/// - 支持运行时创建新池（CreateRuntimePool）
/// - 支持自动增长（如果 canGrow=true，池空时会新建实例）
/// - 支持层级管理（每个池创建独立的 Parent 容器，保持编辑器层级清洁）
/// </summary>
public class ObjectPooler : MonoBehaviour
{
    /// <summary>
    /// 对象池配置类。
    /// </summary>
    [System.Serializable]
    public class Pool
    {
        public string tag;                  // 池的唯一标识
        public GameObject prefab;           // 该池使用的预制体
        public int initialSize;             // 池的初始大小
        public bool canGrow = true;         // 池空时是否自动创建新实例
    }

    /// <summary>
    /// 池对象必须实现的接口。
    /// 用于在 Spawn 和 Despawn 时进行对象初始化/清理。
    /// </summary>
    public interface IPoolableObject
    {
        /// <summary>
        /// 对象从池中取出时调用。用于重置对象状态。
        /// </summary>
        void OnObjectSpawn();

        /// <summary>
        /// 对象归还到池中时调用。用于清理状态、移除事件监听等。
        /// </summary>
        void OnObjectDespawn();
    }

    public static ObjectPooler Instance;

    [SerializeField] public List<Pool> poolsToCreate = new();

    private readonly Dictionary<string, Queue<GameObject>> _poolDictionary = new();
    private readonly Dictionary<string, Transform> _poolParents = new();
    private readonly Dictionary<GameObject, string> _spawnedObjectsTag = new();
    private readonly Dictionary<string, Pool> _poolConfigCache = new();

    /// <summary>
    /// Awake 时初始化所有预配置的对象池。
    /// </summary>
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

        // 从 Inspector 配置的池列表创建对象池
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

    /// <summary>
    /// 从指定池中取出一个对象实例。
    /// 如果池中没有可用对象且 canGrow=true，则创建新实例。
    /// 如果池不存在，会尝试在运行时创建它（需要池配置存在）。
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation, Transform desiredParent = null)
    {
        if (string.IsNullOrEmpty(tag))
        {
            Debug.LogError("[ObjectPooler] SpawnFromPool called with an empty tag.");
            return null;
        }

        // 如果池不存在，尝试创建它
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

        // 从池中取对象或创建新的
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

        // 配置对象的位置、旋转、父级
        objectToSpawn.transform.SetParent(desiredParent != null ? desiredParent : poolParent, false);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        // 记录对象属于哪个池，并通知对象已被取出
        _spawnedObjectsTag[objectToSpawn] = tag;
        NotifySpawn(objectToSpawn);
        return objectToSpawn;
    }

    /// <summary>
    /// 将对象归还到对应的池中。
    /// 会自动调用 IPoolableObject.OnObjectDespawn()。
    /// 如果对象未被此池跟踪，会销毁它。
    /// </summary>
    public void ReturnToPool(GameObject objectToReturn)
    {
        if (objectToReturn == null)
        {
            return;
        }

        // 查找对象属于哪个池
        if (!_spawnedObjectsTag.TryGetValue(objectToReturn, out var tag))
        {
            if (objectToReturn.activeSelf)
            {
                Debug.LogWarning($"[ObjectPooler] Object '{objectToReturn.name}' was not tracked by the pooler. Destroying it.");
                Destroy(objectToReturn);
            }

            return;
        }

        // 验证池存在
        if (!_poolDictionary.TryGetValue(tag, out var queue) || !_poolParents.TryGetValue(tag, out var parent))
        {
            Debug.LogWarning($"[ObjectPooler] Missing pool for tag '{tag}'. Destroying '{objectToReturn.name}'.");
            Destroy(objectToReturn);
            _spawnedObjectsTag.Remove(objectToReturn);
            return;
        }

        // 清理对象状态、禁用、移回池父级
        NotifyDespawn(objectToReturn);
        objectToReturn.SetActive(false);
        objectToReturn.transform.SetParent(parent, false);

        // 归还到队列
        if (!queue.Contains(objectToReturn))
        {
            queue.Enqueue(objectToReturn);
        }

        _spawnedObjectsTag.Remove(objectToReturn);
    }

    /// <summary>
    /// 在运行时创建新的对象池。
    /// 用于动态添加不在 Inspector 中预配置的池。
    /// </summary>
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
