using UnityEngine;

namespace BaoZuPo.Core
{
    /// <summary>
    /// 泛型单例基类（MonoBehaviour）。
    /// 子类只需继承即可自动拥有单例能力，例如：
    ///     public class GameManager : Singleton<GameManager> { }
    ///
    /// 特点：
    /// - 线程安全：使用 lock 保护多线程访问
    /// - 延迟初始化：第一次访问 Instance 时才通过 FindFirstObjectByType 查找
    /// - 重复检查：Awake 时检测重复实例并销毁
    /// - 应用退出保护：退出时标记 _applicationIsQuitting，防止访问已销毁的实例
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        /// <summary>
        /// 获取该类型的单例实例。
        /// 首次访问时通过 FindFirstObjectByType 在场景中查找；
        /// 如果已离开应用，返回 null 并输出警告。
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance of {typeof(T)} already destroyed on application quit.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindFirstObjectByType<T>();

                        if (_instance == null)
                        {
                            Debug.LogError($"[Singleton] No instance of {typeof(T)} found in scene!");
                        }
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Awake 时检查重复实例。如果已存在单例，则销毁新的实例。
        /// </summary>
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[Singleton] Duplicate instance of {typeof(T)} found. Destroying this one.");
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
        }

        /// <summary>
        /// 应用退出时标记标志，防止在析构阶段访问已销毁的实例。
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        /// <summary>
        /// 销毁时清空静态引用。
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _applicationIsQuitting = false;
            }
        }
    }
}
