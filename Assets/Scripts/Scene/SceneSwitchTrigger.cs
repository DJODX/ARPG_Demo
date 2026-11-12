using UnityEngine;

/// <summary>
/// 场景切换触发区：挂在带 Collider 的物体上并勾选 IsTrigger
/// 玩家进入时弹出确认面板，确认后带着玩家跳转到 Inspector 中配置的场景与出生点（均可随时替换）
/// 取消后需先离开再重新进入才会再次弹窗
/// </summary>
[RequireComponent(typeof(Collider))]
public class SceneSwitchTrigger : MonoBehaviour
{
    [SerializeField, Tooltip("要跳转的场景名（需已加入 Build Settings，可直接修改替换）")]
    private string _sceneName;

    [SerializeField, Tooltip("新场景中作为玩家落点的空对象名（可任意命名，留空则玩家保持原位置）")]
    private string _spawnPointName = "PlayerSpawnPoint";

    private bool _playerInside;

    private void Reset()
    {
        // 挂载时自动勾选 IsTrigger，避免把玩家挡在区域外
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_playerInside) return;

        // 只认玩家：与 ReachPoint / 任务触发区保持一致，按组件判定而非标签
        if (other.GetComponentInParent<PlayerProgression>() == null) return;

        _playerInside = true;
        OpenConfirmPanel();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerProgression>() == null) return;
        _playerInside = false;
    }

    private async void OpenConfirmPanel()
    {
        if (string.IsNullOrEmpty(_sceneName))
        {
            Debug.LogWarning($"[SceneSwitch] 「{name}」未配置目标场景名，无法跳转");
            return;
        }

        // 已在切换中或面板已打开时不重复加载（UIManager 会告警并忽略）
        if (SceneSwitchConfirmPanel.IsSwitching) return;
        if (UIManager.Instance.GetPanel<SceneSwitchConfirmPanel>() != null) return;

        UIInputBinder.SetCursorLocked(false); // 显示鼠标，便于点击按钮
        await UIManager.Instance.ShowPanel<SceneSwitchConfirmPanel>();
        UIManager.Instance.GetPanel<SceneSwitchConfirmPanel>()?.Setup(_sceneName, _spawnPointName);
    }
}
