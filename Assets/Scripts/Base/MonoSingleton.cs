using UnityEngine;

/// <summary>
/// Unity MonoBehaviour 单例基类
/// 继承此类的组件将自动成为全局唯一实例，场景加载时自动持久化
/// </summary>
public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();

    /// <summary>
    /// 全局唯一实例访问入口
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance = FindObjectOfType<T>();

                    if (_instance == null)
                    {
                        var go = new GameObject(typeof(T).Name);
                        _instance = go.AddComponent<T>();
                        DontDestroyOnLoad(go);
                    }
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 子类可重写此方法替代 Awake，避免与基类逻辑冲突
    /// </summary>
    protected virtual void OnSingletonAwake() { }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        OnSingletonAwake();
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
