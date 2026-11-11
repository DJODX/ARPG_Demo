using UnityEngine;

/// <summary>
/// 任务接取触发区：挂在带 Collider 的物体上并勾选 IsTrigger
/// 玩家进入时打开接取任务面板，任务通过 QuestDefinition（SO）配置，可直接替换
/// </summary>
[RequireComponent(typeof(Collider))]
public class QuestGiverTrigger : MonoBehaviour
{
    [SerializeField, Tooltip("要接取的任务配置，替换该 SO 即可更换任务")]
    private QuestDefinition _quest;

    [SerializeField, Tooltip("是否只触发一次")]
    private bool _triggerOnce = true;

    private bool _triggered;

    private void Reset()
    {
        // 挂载时自动勾选 IsTrigger，避免把玩家挡在区域外
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_quest == null) return;
        if (_triggerOnce && _triggered) return;

        // 只认玩家：与 ReachPoint / 奖励系统保持一致，按组件判定而非标签
        if (other.GetComponentInParent<PlayerProgression>() == null) return;

        // 已接取过的任务不再弹窗
        if (QuestManager.Instance.IsAccepted(_quest.questId)) return;

        _triggered = true;
        OpenAcceptPanel();
    }

    private async void OpenAcceptPanel()
    {
        // 接取面板已打开时不重复加载（UIManager 会告警并忽略）
        if (UIManager.Instance.GetPanel<QuestAcceptPanel>() != null) return;

        UIInputBinder.SetCursorLocked(false); // 显示鼠标，便于点击按钮
        await UIManager.Instance.ShowPanel<QuestAcceptPanel>();
        UIManager.Instance.GetPanel<QuestAcceptPanel>()?.Setup(_quest);
    }
}
