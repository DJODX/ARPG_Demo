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
    }

    private void OnDisable()
    {
        _inputs.OnInventoryPressed -= ToggleBackpack;
    }

    private void ToggleBackpack()
    {
        bool opening = UIManager.Instance.GetPanel<BackpackPanel>() == null;
        _ = UIManager.Instance.TogglePanel<BackpackPanel>();

        // 开面板解锁鼠标（方便点击），关面板恢复锁定；同步 cursorLocked 保证窗口焦点切换后行为一致
        _inputs.cursorLocked = !opening;
        _inputs.SetCursorState(!opening);
    }
}
