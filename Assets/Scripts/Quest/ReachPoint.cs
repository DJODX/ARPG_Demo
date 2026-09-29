using UnityEngine;

/// <summary>
/// 到达点触发区域：挂在带 Collider 的物体上并勾选 IsTrigger
/// 玩家进入时上报任务系统，由到达类目标的 pointId 匹配推进
/// </summary>
[RequireComponent(typeof(Collider))]
public class ReachPoint : MonoBehaviour
{
    [Tooltip("区域标识，需与到达类任务目标中填写的 ID 完全一致")]
    public string pointId;

    [Tooltip("是否只触发一次（多数到达类任务只需进入一次）")]
    public bool triggerOnce = true;

    private bool _triggered;

    private void Reset()
    {
        // 挂载时自动勾选 IsTrigger，避免忘记配置把玩家挡在区域外
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && _triggered) return;

        // 只认玩家：与奖励系统一致，按组件判定而非标签
        if (other.GetComponentInParent<PlayerProgression>() == null) return;

        QuestManager.Instance.NotifyReachPoint(pointId);
        _triggered = true;
    }
}