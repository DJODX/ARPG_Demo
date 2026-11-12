using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 场景切换确认面板：显示目标场景并提供确认/取消按钮
/// 由 SceneSwitchTrigger 显示后调用 Setup(sceneName, spawnPointName) 注入目标场景与出生点
/// 确认后按 Tag 携带 Player 与 MainCamera 跨场景保留，并把玩家放到新场景指定名字的空对象处
/// 预制体需配置为 Addressable，地址 = "UI/SceneSwitchConfirmPanel"（与 UIManager 约定一致）
/// </summary>
public class SceneSwitchConfirmPanel : PanelBase
{
    private const string PlayerTag = "Player";
    private const string MainCameraTag = "MainCamera";

    [SerializeField, Tooltip("提示文本格式（{0} 会被替换为场景名）")]
    private string _messageFormat = "是否前往「{0}」？";

    [SerializeField, Tooltip("提示文本组件")]
    private Text _messageText;

    [SerializeField, Tooltip("确认按钮")]
    private Button _confirmButton;

    [SerializeField, Tooltip("取消按钮")]
    private Button _cancelButton;

    /// <summary>是否正在切换场景（避免多个传送门重复触发）</summary>
    public static bool IsSwitching { get; private set; }

    // 场景加载期间面板实例可能已被销毁，待携带的对象与出生点信息用静态字段暂存
    private static readonly List<GameObject> _carriedObjects = new List<GameObject>();
    private static Transform _carriedPlayer;
    private static Transform _carriedCamera;
    private static string _pendingSpawnPointName;

    private string _sceneName;
    private string _spawnPointName;

    protected override void Init()
    {
        _confirmButton.onClick.AddListener(HandleConfirmClicked);
        _cancelButton.onClick.AddListener(HandleCancelClicked);
        Refresh();
    }

    /// <summary>注入要跳转的场景名与出生点空对象名（在 ShowPanel 之后调用）</summary>
    public void Setup(string sceneName, string spawnPointName)
    {
        _sceneName = sceneName;
        _spawnPointName = spawnPointName;
        Refresh();
    }

    private void Refresh()
    {
        bool has = !string.IsNullOrEmpty(_sceneName);
        _messageText.text = has ? string.Format(_messageFormat, _sceneName) : "未配置目标场景";
        _confirmButton.interactable = has;
    }

    private void HandleConfirmClicked()
    {
        if (IsSwitching || string.IsNullOrEmpty(_sceneName)) return;

        // 先校验场景是否可用，失败时保留面板让玩家可以取消
        if (!Application.CanStreamedLevelBeLoaded(_sceneName))
        {
            Debug.LogError($"[SceneSwitch] 场景「{_sceneName}」无法加载，请检查是否已加入 Build Settings");
            return;
        }

        // Single 加载会销毁旧场景全部物体，先把玩家与主相机移到 DontDestroyOnLoad 跨场景保留
        CarryByTag();
        _pendingSpawnPointName = _spawnPointName;

        IsSwitching = true;
        _confirmButton.interactable = false;

        // 面板挂在 DontDestroyOnLoad 的 Canvas 上，切换前先关闭并恢复鼠标锁定
        UIManager.Instance.HidePanel<SceneSwitchConfirmPanel>();
        UIInputBinder.SetCursorLocked(true);

        // 新场景加载完成后：把携带对象移入新场景、玩家放到出生点，并兜底恢复鼠标锁定
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        if (SceneManager.LoadSceneAsync(_sceneName) == null)
        {
            Debug.LogError($"[SceneSwitch] 场景「{_sceneName}」加载失败");
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            RestoreCarriedObjects(SceneManager.GetActiveScene());
            ResetCarriedState();
        }
    }

    private void HandleCancelClicked()
    {
        UIManager.Instance.HidePanel<SceneSwitchConfirmPanel>();
        UIInputBinder.SetCursorLocked(true); // 关闭面板后恢复鼠标锁定
    }

    /// <summary>
    /// 按 Tag 收集要跨场景携带的对象：
    /// 场景中可能有多个对象带 Player tag（例如跟随相机），玩家本体按 PlayerProgression / CharacterController
    /// 所在的根对象判定，其余 Player tag 根对象与 MainCamera 一并携带
    /// </summary>
    private static void CarryByTag()
    {
        _carriedObjects.Clear();
        _carriedPlayer = null;
        _carriedCamera = null;

        GameObject[] playerTagged = GameObject.FindGameObjectsWithTag(PlayerTag);

        GameObject playerRoot = FindPlayerRoot(playerTagged);
        if (playerRoot != null)
        {
            _carriedPlayer = playerRoot.transform;
            AddCarried(playerRoot.transform);
        }
        else
        {
            Debug.LogWarning($"[SceneSwitch] 未找到 tag 为「{PlayerTag}」的玩家，玩家不会被带到新场景");
        }

        // 其余带 Player tag 的对象（例如跟随相机）也一起带走，避免随旧场景销毁
        foreach (GameObject go in playerTagged)
        {
            if (go != null) AddCarried(go.transform.root);
        }

        GameObject camera = GameObject.FindGameObjectWithTag(MainCameraTag);
        if (camera != null)
        {
            _carriedCamera = camera.transform;
            AddCarried(camera.transform.root);
        }

        // 没有 MainCamera tag 时，退而在 Player tag 中找带 Camera 组件的对象作为相机参考
        if (_carriedCamera == null)
        {
            foreach (GameObject go in playerTagged)
            {
                if (go == null) continue;
                Camera cam = go.GetComponentInChildren<Camera>();
                if (cam == null) continue;

                _carriedCamera = cam.transform;
                break;
            }
        }

        foreach (GameObject go in _carriedObjects) DontDestroyOnLoad(go);
    }

    /// <summary>登记携带对象，按根对象去重</summary>
    private static void AddCarried(Transform root)
    {
        if (root == null) return;
        if (!_carriedObjects.Contains(root.gameObject)) _carriedObjects.Add(root.gameObject);
    }

    /// <summary>从多个 Player tag 对象中找出玩家本体的根对象（排除相机等干扰对象）</summary>
    private static GameObject FindPlayerRoot(GameObject[] candidates)
    {
        // 优先：带玩家进阶组件的根对象
        foreach (GameObject go in candidates)
        {
            if (go == null) continue;
            Transform root = go.transform.root;
            if (root.GetComponentInChildren<PlayerProgression>(true) != null) return root.gameObject;
        }

        // 其次：带角色控制器的根对象
        foreach (GameObject go in candidates)
        {
            if (go == null) continue;
            Transform root = go.transform.root;
            if (root.GetComponentInChildren<CharacterController>(true) != null) return root.gameObject;
        }

        return candidates.Length > 0 && candidates[0] != null ? candidates[0].transform.root.gameObject : null;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        foreach (GameObject go in _carriedObjects)
        {
            if (go != null) SceneManager.MoveGameObjectToScene(go, scene);
        }

        if (_carriedPlayer != null)
        {
            Transform spawn = FindSpawnPoint(scene, _pendingSpawnPointName);
            if (spawn != null) TeleportPlayer(_carriedPlayer, spawn);
            else
                Debug.LogWarning($"[SceneSwitch] 新场景中未找到出生点「{_pendingSpawnPointName}」，"
                                 + "玩家保留原位置");
        }

        ResetCarriedState();
        UIInputBinder.SetCursorLocked(true);
    }

    /// <summary>加载失败时把携带对象放回当前场景，避免残留在 DontDestroyOnLoad 中</summary>
    private static void RestoreCarriedObjects(Scene scene)
    {
        foreach (GameObject go in _carriedObjects)
        {
            if (go != null) SceneManager.MoveGameObjectToScene(go, scene);
        }
    }

    private static void ResetCarriedState()
    {
        _carriedObjects.Clear();
        _carriedPlayer = null;
        _carriedCamera = null;
        _pendingSpawnPointName = null;
        IsSwitching = false;
    }

    /// <summary>在新场景的根物体中按名字查找出生点空对象（含未激活物体）</summary>
    private static Transform FindSpawnPoint(Scene scene, string spawnPointName)
    {
        if (string.IsNullOrEmpty(spawnPointName)) return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindByName(root.transform, spawnPointName);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform FindByName(Transform parent, string objectName)
    {
        if (parent.name == objectName) return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindByName(child, objectName);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// 把玩家移到出生点；短暂关闭 CharacterController 避免位置被物理回弹。
    /// 相机独立于玩家时同步到出生点旁的相同相对偏移，避免停在上一个场景的世界坐标
    /// </summary>
    private static void TeleportPlayer(Transform playerRoot, Transform spawn)
    {
        bool separateCamera = _carriedCamera != null && _carriedCamera.root != playerRoot;
        Vector3 cameraOffset = separateCamera ? _carriedCamera.position - playerRoot.position : Vector3.zero;

        CharacterController controller = playerRoot.GetComponentInChildren<CharacterController>();
        bool controllerEnabled = controller != null && controller.enabled;
        if (controllerEnabled) controller.enabled = false;

        playerRoot.SetPositionAndRotation(spawn.position, spawn.rotation);
        Physics.SyncTransforms(); // 立即同步，避免后续查询拿到旧位置

        if (controllerEnabled) controller.enabled = true;

        if (separateCamera)
            _carriedCamera.SetPositionAndRotation(spawn.position + cameraOffset, spawn.rotation);
    }
}
