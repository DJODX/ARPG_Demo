using UnityEngine;

/// <summary>
/// 任务目标基类（ScriptableObject）
/// 只承载"配置 + 判定逻辑"，不保存任何进度：
/// 进度统一存在 QuestRuntime 里，这样同一份目标资产可被多个任务、多个存档复用而互不干扰
///
/// 新增目标类型：继承本类重写关心的事件方法即可，QuestManager 无需改动
/// </summary>
public abstract class QuestObjectiveSO : ScriptableObject
{
    [Header("目标配置")]
    [Tooltip("目标描述，如“击杀兽人”")]
    public string description;

    [Tooltip("需要达成的数量")]
    [Min(1)] public int requiredCount = 1;

    /// <summary>敌人被玩家击杀时由 QuestManager 分发</summary>
    /// <returns>进度是否发生变化</returns>
    public virtual bool OnEnemyKilled(QuestRuntime runtime, int index, string killedEnemyId) => false;

    /// <summary>物品进入背包时由 QuestManager 分发</summary>
    /// <returns>进度是否发生变化</returns>
    public virtual bool OnItemAdded(QuestRuntime runtime, int index, int itemId, int count) => false;

    /// <summary>玩家进入到达点触发区域时由 QuestManager 分发</summary>
    /// <returns>进度是否发生变化</returns>
    public virtual bool OnReachPoint(QuestRuntime runtime, int index, string reachedPointId) => false;
}