using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// UI 面板管理器（纯 C# 单例 + Addressables 异步加载）
/// 约定：面板预制体配置为 Addressable，地址 = "UI/" + 面板类名（如 UI/InventoryPanel）
/// 显示/隐藏全部走此处，避免面板重复实例化
/// </summary>
public class UIManager : Singleton<UIManager>
{
    private const string CanvasAddress = "UI/Canvas";

    private readonly Dictionary<string, PanelBase> _panelDict = new Dictionary<string, PanelBase>();
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _panelHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();

    private Canvas _canvas;
    private AsyncOperationHandle<GameObject> _canvasHandle;
    private Task _canvasLoadTask;
    private string _lastHidePanelName;

    /// <summary>显示面板（Addressables 异步加载同名地址的预制体）</summary>
    public async Task ShowPanel<T>() where T : PanelBase
    {
        await ShowPanelByName(typeof(T).Name);
    }

    /// <summary>重新显示最近一次隐藏的面板</summary>
    public async Task ShowLastPanel()
    {
        if (string.IsNullOrEmpty(_lastHidePanelName))
        {
            Debug.LogWarning("[UIManager] 没有可重新显示的面板（上次未隐藏过面板）");
            return;
        }
        await ShowPanelByName(_lastHidePanelName);
    }

    private async Task ShowPanelByName(string panelName)
    {
        await EnsureCanvasAsync();
        if (_canvas == null) return;

        if (_panelDict.ContainsKey(panelName))
        {
            Debug.LogWarning($"[UIManager] 面板 {panelName} 已显示，忽略重复打开");
            return;
        }

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync("UI/" + panelName, _canvas.transform);
        GameObject panelObj = await handle.Task;

        // 异步窗口期再次检查：避免并发打开同一面板
        if (panelObj == null || _panelDict.ContainsKey(panelName))
        {
            if (panelObj != null) Addressables.ReleaseInstance(panelObj);
            Debug.LogWarning($"[UIManager] 面板 {panelName} 加载失败或已被打开");
            return;
        }

        PanelBase panel = panelObj.GetComponent<PanelBase>();
        if (panel == null)
        {
            Debug.LogError($"[UIManager] 预制体 {panelName} 上未挂 PanelBase 组件");
            Addressables.ReleaseInstance(panelObj);
            return;
        }

        _panelHandles[panelName] = handle; // 持有句柄，防止实例化操作被回收
        panel.Show();
        _panelDict.Add(panelName, panel);
    }

    /// <summary>隐藏面板（淡出结束后销毁并移除记录）</summary>
    public void HidePanel<T>() where T : PanelBase
    {
        string panelName = typeof(T).Name;
        if (!_panelDict.TryGetValue(panelName, out PanelBase panel))
        {
            Debug.LogWarning($"[UIManager] 面板 {panelName} 未显示，无法隐藏");
            return;
        }

        _lastHidePanelName = panelName;
        panel.Hide(() =>
        {
            if (panel != null && panel.gameObject != null)
                Addressables.ReleaseInstance(panel.gameObject); // 销毁实例并归还引用计数
            _panelHandles.Remove(panelName);
            _panelDict.Remove(panelName);
        });
    }

    /// <summary>获取已显示的面板（未显示返回 null）</summary>
    public T GetPanel<T>() where T : PanelBase
    {
        string panelName = typeof(T).Name;
        return _panelDict.TryGetValue(panelName, out PanelBase panel) ? panel as T : null;
    }

    /// <summary>确保 Canvas 已加载（并发去重：多次调用共享同一加载任务）</summary>
    private Task EnsureCanvasAsync()
    {
        if (_canvas != null) return Task.CompletedTask;
        if (_canvasLoadTask == null)
            _canvasLoadTask = LoadCanvasAsync();
        return _canvasLoadTask;
    }

    private async Task LoadCanvasAsync()
    {
        _canvasHandle = Addressables.LoadAssetAsync<GameObject>(CanvasAddress);
        GameObject canvasPrefab = await _canvasHandle.Task;
        if (canvasPrefab == null)
        {
            Debug.LogError($"[UIManager] 找不到 Addressables 资源 {CanvasAddress}");
            return;
        }
        GameObject canvasObj = Object.Instantiate(canvasPrefab);
        _canvas = canvasObj.GetComponent<Canvas>();
        if (_canvas == null)
        {
            Debug.LogError($"[UIManager] {CanvasAddress} 预制体上未挂 Canvas 组件");
            Object.Destroy(canvasObj);
            return;
        }
        Object.DontDestroyOnLoad(canvasObj);
    }
}
