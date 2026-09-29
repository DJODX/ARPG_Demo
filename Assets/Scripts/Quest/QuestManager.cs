using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 任务管理器（单例）
/// 职责：任务接取与状态管理、集中订阅各游戏事件并分发到目标任务、
///       目标全部达成后自动完成并通过奖励系统发奖、对外广播变化供 UI 订阅
///
/// 事件分发集中在本类，而不是让各目标自行订阅：
/// 目标资产是共享的 ScriptableObject，若它自己持有订阅与进度，会造成跨任务、跨存档的状态污染
/// </summary>
public class QuestManager : MonoSingleton<QuestManager>
{
    [Header("任务配置（所有可接任务）")]
    [Tooltip("任务库，运行时按 questId 建立索引；ID 为空或重复的会被跳过并告警")]
    public List<QuestDefinition> questDatabase = new List<QuestDefinition>();

    [Header("调试：进入运行时自动接取（尚无任务 UI 时用于验证）")]
    public List<QuestDefinition> autoAcceptOnStart = new List<QuestDefinition>();

    private readonly Dictionary<string, QuestDefinition> _lookup = new Dictionary<string, QuestDefinition>();
    private readonly Dictionary<string, QuestRuntime> _active = new Dictionary<string, QuestRuntime>();

    /// <summary>任务被接取（参数为任务运行时状态）</summary>
    public event Action<QuestRuntime> OnQuestAccepted;

    /// <summary>任务进度或状态变化（UI 订阅刷新）</summary>
    public event Action<QuestRuntime> OnQuestUpdated;

    /// <summary>任务完成、奖励已发放</summary>
    public event Action<QuestRuntime> OnQuestCompleted;

    /// <summary>已接取的任务（含已完成）</summary>
    public IReadOnlyCollection<QuestRuntime> ActiveQuests => _active.Values;

    protected override void OnSingletonAwake()
    {
        BuildLookup();

        // 单点订阅：击杀与入包从这里分发；到达由 ReachPoint 主动调用 NotifyReachPoint
        RewardManager.Instance.OnEnemyKilled += HandleEnemyKilled;
        InventoryManager.Instance.OnItemAdded += HandleItemAdded;

        // 调试用：无 UI 阶段先自动接取，便于直接进 Play 验证
        foreach (QuestDefinition definition in autoAcceptOnStart)
        {
            if (definition != null) AcceptQuest(definition);
        }
    }

    // ==================== 接取与查询 ====================

    /// <summary>按 ID 接取任务</summary>
    public bool AcceptQuest(string questId)
    {
        if (!_lookup.TryGetValue(questId, out QuestDefinition definition))
        {
            Debug.LogWarning($"[Quest] 任务库中不存在任务：{questId}");
            return false;
        }
        return AcceptQuest(definition);
    }

    /// <summary>接取任务（同一任务不可重复接取）</summary>
    public bool AcceptQuest(QuestDefinition definition)
    {
        if (definition == null) return false;

        if (string.IsNullOrEmpty(definition.questId))
        {
            Debug.LogWarning($"[Quest] 任务「{definition.name}」未配置 questId，无法接取");
            return false;
        }
        if (_active.ContainsKey(definition.questId)) return false;

        QuestRuntime runtime = new QuestRuntime { questId = definition.questId };
        runtime.Bind(definition);
        _active[definition.questId] = runtime;

        OnQuestAccepted?.Invoke(runtime);
        OnQuestUpdated?.Invoke(runtime);
        return true;
    }

    /// <summary>是否已接取（含已完成）</summary>
    public bool IsAccepted(string questId) => _active.ContainsKey(questId);

    /// <summary>是否已完成</summary>
    public bool IsCompleted(string questId)
    {
        return _active.TryGetValue(questId, out QuestRuntime runtime)
               && runtime.status == QuestStatus.Completed;
    }

    /// <summary>取任务运行时状态（未接取返回 null）</summary>
    public QuestRuntime GetRuntime(string questId)
    {
        _active.TryGetValue(questId, out QuestRuntime runtime);
        return runtime;
    }

    // ==================== 事件入口 ====================

    /// <summary>敌人被玩家击杀：取敌人标识后分发给击杀类目标</summary>
    private void HandleEnemyKilled(PlayerProgression receiver, GameObject victim)
    {
        if (victim == null) return;

        EnemyIdentity identity = victim.GetComponent<EnemyIdentity>();
        if (identity == null || string.IsNullOrEmpty(identity.enemyId)) return;

        string enemyId = identity.enemyId;
        Dispatch((objective, runtime, index) => objective.OnEnemyKilled(runtime, index, enemyId));
    }

    /// <summary>物品入包：分发给收集类目标</summary>
    private void HandleItemAdded(int itemId, int count)
    {
        Dispatch((objective, runtime, index) => objective.OnItemAdded(runtime, index, itemId, count));
    }

    /// <summary>到达点上报（由场景中的 ReachPoint 触发区调用）</summary>
    public void NotifyReachPoint(string pointId)
    {
        if (string.IsNullOrEmpty(pointId)) return;
        Dispatch((objective, runtime, index) => objective.OnReachPoint(runtime, index, pointId));
    }

    // ==================== 分发与完成 ====================

    /// <summary>
    /// 把一次游戏事件分发给所有进行中任务的每个目标，
    /// 统一处理"进度变化 → 广播"与"目标全部达成 → 完成任务"
    /// </summary>
    private void Dispatch(Func<QuestObjectiveSO, QuestRuntime, int, bool> handler)
    {
        if (_active.Count == 0) return;

        List<QuestRuntime> completed = null;

        foreach (QuestRuntime runtime in _active.Values)
        {
            if (runtime.status != QuestStatus.InProgress) continue;

            List<QuestObjectiveSO> objectives = runtime.definition.objectives;
            bool changed = false;
            for (int i = 0; i < objectives.Count; i++)
            {
                if (handler(objectives[i], runtime, i)) changed = true;
            }

            if (changed) OnQuestUpdated?.Invoke(runtime);

            if (runtime.AllObjectivesComplete)
            {
                if (completed == null) completed = new List<QuestRuntime>();
                completed.Add(runtime);
            }
        }

        // 完成处理放在遍历之后，避免遍历过程中改动状态引发副作用
        if (completed == null) return;
        foreach (QuestRuntime runtime in completed) CompleteQuest(runtime);
    }

    /// <summary>完成任务：置状态 → 走奖励系统发奖 → 广播</summary>
    private void CompleteQuest(QuestRuntime runtime)
    {
        if (runtime.status == QuestStatus.Completed) return;
        runtime.status = QuestStatus.Completed;

        List<RewardEntry> rewards = runtime.definition.rewards;
        if (rewards != null && rewards.Count > 0)
        {
            PlayerProgression receiver = FindObjectOfType<PlayerProgression>();
            if (receiver != null)
            {
                // 复用奖励系统的发放策略：任务奖励与击杀掉落共用同一套配置结构
                RewardManager.Instance.GrantAll(rewards, receiver, null);
            }
            else
            {
                Debug.LogWarning($"[Quest] 任务「{runtime.definition.title}」已完成，"
                                 + "但场景中未找到 PlayerProgression，奖励未发放");
            }
        }

        OnQuestCompleted?.Invoke(runtime);
        OnQuestUpdated?.Invoke(runtime);
    }

    // ==================== 存档接口（供后续存档系统调用） ====================

    /// <summary>导出全部任务状态（QuestRuntime 可被 JsonUtility 直接序列化）</summary>
    public List<QuestRuntime> CaptureState()
    {
        return new List<QuestRuntime>(_active.Values);
    }

    /// <summary>从存档恢复任务状态，按 questId 重新绑定任务配置</summary>
    public void RestoreState(List<QuestRuntime> saved)
    {
        _active.Clear();
        if (saved == null) return;

        foreach (QuestRuntime runtime in saved)
        {
            if (runtime == null || string.IsNullOrEmpty(runtime.questId)) continue;

            if (!_lookup.TryGetValue(runtime.questId, out QuestDefinition definition))
            {
                Debug.LogWarning($"[Quest] 读档时未找到任务配置：{runtime.questId}，已跳过");
                continue;
            }

            runtime.Bind(definition);
            _active[runtime.questId] = runtime;
            OnQuestUpdated?.Invoke(runtime);
        }
    }

    // ==================== 索引 ====================

    private void BuildLookup()
    {
        _lookup.Clear();

        foreach (QuestDefinition definition in questDatabase)
        {
            if (definition == null) continue;

            if (string.IsNullOrEmpty(definition.questId))
            {
                Debug.LogWarning($"[Quest] 任务「{definition.name}」未配置 questId，已跳过");
                continue;
            }
            if (_lookup.ContainsKey(definition.questId))
            {
                Debug.LogWarning($"[Quest] questId 重复：{definition.questId}（{definition.name}）");
                continue;
            }

            _lookup[definition.questId] = definition;
        }
    }

#if UNITY_EDITOR
    // 编辑器下修改任务库后自动重建索引，避免运行时报错
    private void OnValidate()
    {
        BuildLookup();
    }
#endif
}