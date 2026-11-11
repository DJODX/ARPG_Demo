using StarterAssets;
using UnityEngine;

/// <summary>
/// 输入 → UI 桥接：把 StarterAssetsInputs 的 UI 类按键事件转发给 UIManager
/// 挂在 Player 身上（与 StarterAssetsInputs 同一物体）
/// </summary>
public class UIInputBinder : MonoBehaviour
{
    private StarterAssetsInputs _inputs;

    private void Awake()
    {
        _inputs = GetComponent<StarterAssetsInputs>();
    }

    private void OnEnable()
    {
        _inputs.OnInventoryPressed += ToggleBackpack;
        _inputs.OnQuestPressed += ToggleQuestPanel;
    }

    private void OnDisable()
    {
        _inputs.OnInventoryPressed -= ToggleBackpack;
        _inputs.OnQuestPressed -= ToggleQuestPanel;
    }

    private void ToggleBackpack()
    {
        bool opening = UIManager.Instance.GetPanel<BackpackPanel>() == null;
        _ = UIManager.Instance.TogglePanel<BackpackPanel>();

        // 有面板打开时解锁鼠标（显示）；全部关闭后才恢复锁定（隐藏）
        ApplyCursorState(!(opening || UIManager.Instance.GetPanel<QuestPanel>() != null));
    }

    private void ToggleQuestPanel()
    {
        bool opening = UIManager.Instance.GetPanel<QuestPanel>() == null;
        _ = UIManager.Instance.TogglePanel<QuestPanel>();

        ApplyCursorState(!(opening || UIManager.Instance.GetPanel<BackpackPanel>() != null));
    }

    /// <summary>开面板解锁鼠标（方便点击），关面板恢复锁定；同步 cursorLocked 保证窗口焦点切换后行为一致</summary>
    private void ApplyCursorState(bool locked)
    {
        _inputs.cursorLocked = locked;
        _inputs.SetCursorState(locked);
    }

    /// <summary>供面板内关闭按钮恢复鼠标锁定（找不到玩家输入时忽略）</summary>
    public static void SetCursorLocked(bool locked)
    {
        StarterAssetsInputs inputs = FindObjectOfType<StarterAssetsInputs>();
        if (inputs == null) return;

        inputs.cursorLocked = locked;
        inputs.SetCursorState(locked);
    }
}
