using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 场景切换确认面板：显示目标场景并提供确认/取消按钮
/// 由 SceneSwitchTrigger 显示后调用 Setup(sceneName, spawnPointName) 注入目标场景与出生点
///
/// 玩家由三个节点组成：PlayerArmature（玩家本体）、MainCamera（渲染相机 + CinemachineBrain）、
/// PlayerFollowCamera（Cinemachine 虚拟相机）。确认后三件套整体跨场景保留并迁入新场景；
/// 目标场景若已存在同类节点，按“传送方优先”策略销毁目标场景的旧节点
///
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

    // 场景加载期间面板实例可能已被销毁，待携带的节点与出生点信息用静态字段暂存
    private static readonly List<GameObject> _carriedRoots = new List<GameObject>();
    private static Transform _playerRig;
    private static Transform _mainCamera;
    private static Transform _followCamera;
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

        // Single 加载会销毁旧场景全部物体，先把三件套移到 DontDestroyOnLoad 跨场景保留
        CollectPlayerRig();
        _pendingSpawnPointName = _spawnPointName;

        IsSwitching = true;
        _confirmButton.interactable = false;

        // 面板挂在 DontDestroyOnLoad 的 Canvas 上，切换前先关闭并恢复鼠标锁定
        UIManager.Instance.HidePanel<SceneSwitchConfirmPanel>();
        UIInputBinder.SetCursorLocked(true);

        // 新场景加载完成后：销毁冲突节点、迁入三件套、放到出生点，并兜底恢复鼠标锁定
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        if (SceneManager.LoadSceneAsync(_sceneName) == null)
        {
            Debug.LogError($"[SceneSwitch] 场景「{_sceneName}」加载失败");
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            MoveCarriedRoots(SceneManager.GetActiveScene());
            ResetCarriedState();
        }
    }

    private void HandleCancelClicked()
    {
        UIManager.Instance.HidePanel<SceneSwitchConfirmPanel>();
        UIInputBinder.SetCursorLocked(true); // 关闭面板后恢复鼠标锁定
    }

    // ==================== 传送前：收集玩家三件套 ====================

    /// <summary>
    /// 按 tag + 组件收集玩家三件套并设为跨场景保留：
    /// 玩家 = Player tag 且带 PlayerProgression / CharacterController（排除同样打了 Player tag 的相机）；
    /// 主相机 = MainCamera tag 且带 Camera 组件；
    /// 跟随相机 = CinemachineVirtualCamera（优先 Follow 指向玩家的那台）
    /// </summary>
    private static void CollectPlayerRig()
    {
        ResetCarriedState();

        _playerRig = FindPlayerRoot();
        _mainCamera = FindMainCamera();
        _followCamera = FindFollowCamera(_playerRig);

        if (_playerRig != null) AddCarried(_playerRig);
        else Debug.LogWarning($"[SceneSwitch] 未找到 tag 为「{PlayerTag}」的玩家，玩家不会被带到新场景");

        if (_mainCamera != null) AddCarried(_mainCamera.root);
        else Debug.LogWarning($"[SceneSwitch] 未找到 tag 为「{MainCameraTag}」的主相机，相机不会被带到新场景");

        if (_followCamera != null) AddCarried(_followCamera.root);
        else Debug.LogWarning("[SceneSwitch] 未找到 Cinemachine 跟随相机，相机不会被带到新场景");

        foreach (GameObject root in _carriedRoots) DontDestroyOnLoad(root);
    }

    /// <summary>多个对象都带 Player tag 时，用玩家组件锁定真正的玩家根对象</summary>
    private static Transform FindPlayerRoot()
    {
        GameObject[] tagged = GameObject.FindGameObjectsWithTag(PlayerTag);

        foreach (GameObject go in tagged)
        {
            if (go == null) continue;
            Transform root = go.transform.root;
            if (root.GetComponentInChildren<PlayerProgression>(true) != null) return root;
        }

        foreach (GameObject go in tagged)
        {
            if (go == null) continue;
            Transform root = go.transform.root;
            if (root.GetComponentInChildren<CharacterController>(true) != null) return root;
        }
        return null;
    }

    private static Transform FindMainCamera()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(MainCameraTag))
        {
            if (go != null && go.GetComponent<Camera>() != null) return go.transform;
        }

        // 兜底：tag 配置不规范时，用挂 CinemachineBrain 的渲染相机
        CinemachineBrain brain = Object.FindObjectOfType<CinemachineBrain>();
        return brain != null ? brain.transform : null;
    }

    private static Transform FindFollowCamera(Transform playerRoot)
    {
        CinemachineVirtualCamera fallback = null;

        foreach (CinemachineVirtualCamera vcam in Object.FindObjectsOfType<CinemachineVirtualCamera>())
        {
            if (fallback == null) fallback = vcam;

            Transform follow = vcam.Follow;
            if (playerRoot == null || follow == null) continue;
            if (follow == playerRoot || follow.IsChildOf(playerRoot)) return vcam.transform;
        }
        return fallback != null ? fallback.transform : null;
    }

    /// <summary>登记携带对象，按根对象去重</summary>
    private static void AddCarried(Transform root)
    {
        if (root == null) return;
        if (!_carriedRoots.Contains(root.gameObject)) _carriedRoots.Add(root.gameObject);
    }

    // ==================== 加载后：冲突处理与迁入 ====================

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        DestroySceneConflicts(scene);
        MoveCarriedRoots(scene);

        if (_playerRig != null)
        {
            Transform spawn = FindSpawnPoint(scene, _pendingSpawnPointName);
            if (spawn != null) TeleportPlayer(_playerRig, spawn);
            else
                Debug.LogWarning($"[SceneSwitch] 新场景中未找到出生点「{_pendingSpawnPointName}」，"
                                 + "玩家保留原位置");
        }

        RebindFollowTarget();

        ResetCarriedState();
        UIInputBinder.SetCursorLocked(true);
    }

    /// <summary>
    /// 传送方优先：新场景中已存在的玩家 / 主相机 / 跟随相机根对象直接销毁，
    /// 避免出现两个玩家、两台渲染相机或两套跟随逻辑
    /// </summary>
    private static void DestroySceneConflicts(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root == null) continue;

            bool conflict =
                (_playerRig != null && IsPlayerRoot(root)) ||
                (_mainCamera != null && IsMainCameraRoot(root)) ||
                (_followCamera != null && IsFollowCameraRoot(root));

            if (conflict) Object.Destroy(root);
        }
    }

    private static bool IsPlayerRoot(GameObject root)
    {
        if (root.GetComponentInChildren<PlayerProgression>(true) != null) return true;

        // 只有 CharacterController 时还要求带 Player 标签，避免误伤场景中的其他角色
        return root.CompareTag(PlayerTag)
               && root.GetComponentInChildren<CharacterController>(true) != null;
    }

    private static bool IsMainCameraRoot(GameObject root)
    {
        foreach (Camera cam in root.GetComponentsInChildren<Camera>(true))
        {
            if (cam.CompareTag(MainCameraTag)) return true;
        }
        return root.GetComponentInChildren<CinemachineBrain>(true) != null;
    }

    private static bool IsFollowCameraRoot(GameObject root)
    {
        return root.GetComponentInChildren<CinemachineVirtualCamera>(true) != null;
    }

    private static void MoveCarriedRoots(Scene scene)
    {
        foreach (GameObject root in _carriedRoots)
        {
            if (root != null) SceneManager.MoveGameObjectToScene(root, scene);
        }
    }

    private static void ResetCarriedState()
    {
        _carriedRoots.Clear();
        _playerRig = null;
        _mainCamera = null;
        _followCamera = null;
        _pendingSpawnPointName = null;
        IsSwitching = false;
    }

    // ==================== 落点与相机绑定 ====================

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
    /// 主相机独立于玩家时同步到出生点旁的相同相对偏移，避免停在上一个场景的世界坐标
    /// </summary>
    private static void TeleportPlayer(Transform playerRoot, Transform spawn)
    {
        bool separateCamera = _mainCamera != null && _mainCamera.root != playerRoot;
        Vector3 cameraOffset = separateCamera ? _mainCamera.position - playerRoot.position : Vector3.zero;

        CharacterController controller = playerRoot.GetComponentInChildren<CharacterController>();
        bool controllerEnabled = controller != null && controller.enabled;
        if (controllerEnabled) controller.enabled = false;

        playerRoot.SetPositionAndRotation(spawn.position, spawn.rotation);
        Physics.SyncTransforms(); // 立即同步，避免后续查询拿到旧位置

        if (controllerEnabled) controller.enabled = true;

        if (separateCamera)
            _mainCamera.SetPositionAndRotation(spawn.position + cameraOffset, spawn.rotation);
    }

    /// <summary>
    /// 跟随相机的 Follow / LookAt 若跨场景后失效（指向的旧场景物体被销毁），重绑到玩家，
    /// 避免 Cinemachine 因目标为空而不再跟随
    /// </summary>
    private static void RebindFollowTarget()
    {
        if (_followCamera == null || _playerRig == null) return;

        CinemachineVirtualCamera vcam = _followCamera.GetComponentInChildren<CinemachineVirtualCamera>();
        if (vcam == null) return;

        Transform follow = vcam.Follow;
        if (follow == null || (follow != _playerRig && !follow.IsChildOf(_playerRig)))
            vcam.Follow = _playerRig;

        Transform lookAt = vcam.LookAt;
        if (lookAt != null && lookAt != _playerRig && !lookAt.IsChildOf(_playerRig))
            vcam.LookAt = _playerRig;
    }
}
