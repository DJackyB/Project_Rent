using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;     // Odin相关，可去除

public class ObjectPooler : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;            // 对象的标签
        public GameObject prefab;     // 对象的预制件
        public int initialSize;       // 池的初始大小
        public bool canGrow = true;   // 池是否可以动态扩展
    }

    public static ObjectPooler Instance; // 单例实例

    // Odin相关，可去除
    [InfoBox("Make sure:" +
             "\nTransform.position = (0,0,0);" +
             "\nTransform.rotation = (0,0,0);" +
             "\nTransform.scale = (1,1,1);", InfoMessageType.Error, "ShouldShowTransformWarning")]
    // Odin相关

    public List<Pool> poolsToCreate;

    // Odin相关，可去除
    private bool ShouldShowTransformWarning()
    {
        if (transform == null) return false;
        return transform.position != Vector3.zero
               || transform.rotation != Quaternion.identity
               || transform.localScale != Vector3.one;
    }
    // Odin相关

    // 核心存储：字典，键是tag，值是该tag对应的对象队列
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    // 存储每个tag对应的父级Transform
    private Dictionary<string, Transform> poolParents = new Dictionary<string, Transform>();

    // 通过GameObject实例找到其所属池的tag，方便归还
    private Dictionary<GameObject, string> spawnedObjectsTag = new Dictionary<GameObject, string>();

    // ★ CHANGED：缓存运行时与Inspector的池配置，统一查询来源
    private Dictionary<string, Pool> _poolConfigCache = new Dictionary<string, Pool>();  // ★ CHANGED

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // 防止重复实例
            return;
        }

        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolParents = new Dictionary<string, Transform>();

        // 根据Inspector中配置的poolsToCreate来预热池
        foreach (Pool pool in poolsToCreate)
        {
            if (pool.prefab == null)
            {
                Debug.LogError($"Pool with tag '{pool.tag}' has a null prefab. Skipping this pool.");
                continue;
            }

            CreateAndFillPool(pool.tag, pool.prefab, pool.initialSize, pool.canGrow); // ★ CHANGED: 内部会写入缓存
        }

        if (transform.position != Vector3.zero
            || transform.rotation != Quaternion.identity
            || transform.localScale != Vector3.one)
        {
            Debug.LogError("Please Make sure:" +
                           "Transform.position = (0,0,0);" +
                           "Transform.rotation = (0,0,0);" +
                           "Transform.scale = (1,1,1);");
        }
    }

    // ★ CHANGED：统一的配置查询（优先缓存→再查Inspector）
    private bool TryGetPoolConfig(string tag, out Pool poolConfig)    // ★ CHANGED
    {
        poolConfig = null;
        if (!string.IsNullOrEmpty(tag))
        {
            if (_poolConfigCache != null && _poolConfigCache.TryGetValue(tag, out poolConfig) && poolConfig != null)
                return true;

            if (poolsToCreate != null)
            {
                poolConfig = poolsToCreate.Find(p => p.tag == tag);
                if (poolConfig != null) return true;
            }
        }
        return false;
    }

    // 创建并填充一个池（如果不存在）
    private void CreateAndFillPool(string tag, GameObject prefab, int initialSize, bool canGrowConfigValue)
    {
        if (poolDictionary.ContainsKey(tag)) return;

        // 1. 创建父级GameObject
        GameObject parentGO = new GameObject(tag + " Pool");
        Transform parentTransform = parentGO.transform;

        // 2. 将其父级设置为ObjectPooler，保持Hierarchy整洁
        parentTransform.SetParent(this.transform);

        // 3. 重置其相对于父级(ObjectPooler)的局部Transform
        parentTransform.localPosition = Vector3.zero;
        parentTransform.localRotation = Quaternion.identity;
        parentTransform.localScale = Vector3.one;

        poolParents[tag] = parentTransform;

        // 4. 创建对象队列并填充
        Queue<GameObject> objectQueue = new Queue<GameObject>();
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            obj.transform.SetParent(parentTransform);
            objectQueue.Enqueue(obj);
        }
        poolDictionary[tag] = objectQueue;

        // ★ CHANGED：将Inspector来源的配置也写入缓存，统一管理
        _poolConfigCache[tag] = new Pool                     // ★ CHANGED
        {
            tag = tag,
            prefab = prefab,
            initialSize = initialSize,
            canGrow = canGrowConfigValue
        };
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation, Transform desiredParent = null)
    {
        if (string.IsNullOrEmpty(tag))
        {
            Debug.LogError("SpawnFromPool called with an empty or null tag.");
            return null;
        }

        // 1) 确保池存在；若不存在，尝试按配置创建
        if (!poolDictionary.ContainsKey(tag) || poolDictionary[tag] == null)
        {
            // 先从统一接口拿配置
            if (TryGetPoolConfig(tag, out var cfg))
            {
                CreateAndFillPool(cfg.tag, cfg.prefab, cfg.initialSize, cfg.canGrow);  // ★ CHANGED: 用缓存或Inspector配置
            }
            else
            {
                Debug.LogError(
                    $"No valid configuration found to create a new pool for tag '{tag}'.");
                return null;
            }

            // 再校验一次
            if (!poolDictionary.ContainsKey(tag) || poolDictionary[tag] == null)
            {
                Debug.LogError($"Failed to create pool for tag '{tag}' even after attempt.");
                return null;
            }
        }

        Queue<GameObject> currentPoolQueue = poolDictionary[tag];
        GameObject objectToSpawn = null;

        // 获取该tag的父级
        Transform poolSpecificParent = poolParents.ContainsKey(tag) ? poolParents[tag] : this.transform;

        if (currentPoolQueue.Count > 0)
        {
            objectToSpawn = currentPoolQueue.Dequeue();

            // 保险同步父级
            if (objectToSpawn.transform.parent != poolSpecificParent)
            {
                objectToSpawn.transform.SetParent(poolSpecificParent);
            }
        }
        else
        {
            // ★ CHANGED：空池扩容 → 统一从配置源获取（支持运行时创建的池）
            if (!TryGetPoolConfig(tag, out var cfg))
            {
                Debug.LogError($"No valid config found for tag '{tag}' to grow the pool."); // ★ CHANGED
                return null;
            }

            if (cfg.canGrow && cfg.prefab != null)
            {
                objectToSpawn = Instantiate(cfg.prefab);
                objectToSpawn.transform.SetParent(poolSpecificParent);
                objectToSpawn.SetActive(false);
            }
            else
            {
                // 不能扩容，直接返回 null（按你的原始语义）
                return null;
            }
        }

        if (objectToSpawn != null)
        {
            // 只有当调用者明确提供了一个有效的 desiredParent (不是null)，才改变父对象
            if (desiredParent != null)
            {
                objectToSpawn.transform.SetParent(desiredParent);
            }
            // 若 desiredParent 为 null，则仍保留在 poolSpecificParent 下（其变换矩阵须为单位）

            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
            objectToSpawn.SetActive(true);

            // 记录激活对象所属tag
            if (!spawnedObjectsTag.ContainsKey(objectToSpawn))
            {
                spawnedObjectsTag.Add(objectToSpawn, tag);
            }
            else
            {
                spawnedObjectsTag[objectToSpawn] = tag;
            }

            // 回调可池化生命周期
            var poolables = objectToSpawn.GetComponents<IPoolableObject>();
            foreach (var p in poolables)
            {
                p.OnObjectSpawn();
            }
        }

        return objectToSpawn;
    }

    public void ReturnToPool(GameObject objectToReturn)
    {
        if (objectToReturn == null)
            return;

        if (!objectToReturn.activeSelf && spawnedObjectsTag.ContainsKey(objectToReturn))
        {
            // 已经inactive但被追踪 → 继续归还流程，确保入队
        }
        else if (!objectToReturn.activeSelf && !spawnedObjectsTag.ContainsKey(objectToReturn))
        {
            // 已inactive且不被追踪 → 认为已处理过/不属于本池
            return;
        }

        if (spawnedObjectsTag.TryGetValue(objectToReturn, out string tag))
        {
            if (poolDictionary.ContainsKey(tag) && poolParents.ContainsKey(tag))
            {
                var poolables = objectToReturn.GetComponents<IPoolableObject>();
                foreach (var p in poolables)
                {
                    p.OnObjectDespawn();
                }

                
                objectToReturn.SetActive(false);
                objectToReturn.transform.SetParent(poolParents[tag]);

                if (!poolDictionary[tag].Contains(objectToReturn))
                {
                    poolDictionary[tag].Enqueue(objectToReturn);
                }

                spawnedObjectsTag.Remove(objectToReturn);
            }
            else
            {
                Debug.LogWarning(
                    $"Pool dictionary or parent for tag '{tag}' (object: {objectToReturn.name}) not found. Destroying object.");
                Destroy(objectToReturn);
            }
        }
        else
        {
            if (objectToReturn.activeSelf)
            {
                Debug.LogWarning(
                    $"Active object {objectToReturn.name} was attempted to be returned but not found in spawnedObjectsTag. Destroying object.");
                Destroy(objectToReturn);
            }
            else
            {
                // 已inactive且不被追踪 → 忽略
                return;
            }
        }
    }

    // (推荐) 定义一个接口，让可池化对象实现自己的Spawn和Despawn逻辑
    public interface IPoolableObject
    {
        void OnObjectSpawn(); // 当对象从池中取出并激活时调用
        void OnObjectDespawn(); // (可选) 当对象归还到池中并反激活前调用
    }

    #region RuntimePool

    /// <summary>
    /// 运行时创建新的对象池（用于代码侧按需建池）
    /// </summary>
    /// <returns>如果成功创建或池已存在，返回true。</returns>
    public bool CreateRuntimePool(string tag, GameObject prefab, int initialSize = 1, bool canGrow = true)
    {
        if (prefab == null)
        {
            Debug.LogError($"[ObjectPooler] 尝试为Tag '{tag}' 创建运行时池失败：Prefab为空。");
            return false;
        }

        if (poolDictionary.ContainsKey(tag))
        {
            // 若池已存在，更新缓存配置（以防后续需要扩容时读取最新配置） ★ CHANGED（非必需，但更稳）
            _poolConfigCache[tag] = new Pool { tag = tag, prefab = prefab, initialSize = initialSize, canGrow = canGrow }; // ★ CHANGED
            return true;
        }

        var newPoolConfig = new Pool
        {
            tag = tag,
            prefab = prefab,
            initialSize = initialSize,
            canGrow = canGrow
        };
        CreateAndRegisterPool(newPoolConfig); // 内部会写入缓存
        return true;
    }

    private void CreateAndRegisterPool(Pool poolConfig)
    {
        string tag = poolConfig.tag;
        if (poolDictionary.ContainsKey(tag)) return;

        GameObject parentGO = new GameObject(tag + " Pool");
        parentGO.transform.SetParent(this.transform);
        parentGO.transform.localPosition = Vector3.zero;
        parentGO.transform.localRotation = Quaternion.identity;
        parentGO.transform.localScale = Vector3.one;

        poolParents[tag] = parentGO.transform;

        Queue<GameObject> objectQueue = new Queue<GameObject>();
        for (int i = 0; i < poolConfig.initialSize; i++)
        {
            GameObject obj = Instantiate(poolConfig.prefab);
            obj.SetActive(false);
            obj.transform.SetParent(parentGO.transform);
            objectQueue.Enqueue(obj);
        }

        poolDictionary[tag] = objectQueue;

        _poolConfigCache[tag] = poolConfig; // ★ 已存在：运行时创建会入缓存
    }

    #endregion
}
