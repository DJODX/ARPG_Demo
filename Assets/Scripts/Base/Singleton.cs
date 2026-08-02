/// <summary>
/// 普通 C# 类单例基类
/// 非 MonoBehaviour，适用于纯逻辑管理类（如数据管理、配置管理）
/// </summary>
public class Singleton<T> where T : class, new()
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
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 构造函数受保护，禁止外部直接 new
    /// </summary>
    protected Singleton()
    {
        if (_instance != null)
        {
            throw new System.InvalidOperationException(
                $"{typeof(T).Name} 是单例类，请通过 Instance 属性访问");
        }
    }
}
