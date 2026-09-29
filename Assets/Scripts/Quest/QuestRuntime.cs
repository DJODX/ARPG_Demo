using System;
using UnityEngine;

/// <summary>任务状态</summary>
public enum QuestStatus
{
    /// <summary>进行中</summary>
    InProgress,

    /// <summary>已完成（奖励已发放）</summary>
    Completed,
}

/// <summary>
/// 任务运行时状态（普通可序列化类，非 MonoBehaviour）
/// 只保存"存档需要的数据"：任务 ID、各目标进度、状态；
/// 任务配置通过 definition 引用持有但不参与序列化，读档时按 questId 重新绑定
/// </summary>
[Serializable]
public class QuestRuntime
{
    [Tooltip("任务 ID，与 QuestDefinition.questId 对应")]
    public string questId;

    /// <summary>各目标进度，下标与 QuestDefinition.objectives 一一对应</summary>
    public int[] objectiveProgress = new int[0];

    [Tooltip("任务状态")]
    public QuestStatus status = QuestStatus.InProgress;

    /// <summary>任务配置（运行时引用，不参与序列化）</summary>
    [NonSerialized] public QuestDefinition definition;

    /// <summary>
    /// 绑定任务配置（接取或读档时调用），并按需补齐进度数组长度
    /// 读档时存档里的进度数组长度可能与当前配置不一致，这里做一次安全对齐
    /// </summary>
    public void Bind(QuestDefinition def)
    {
        definition = def;
        if (def == null) return;

        int count = def.objectives.Count;
        if (objectiveProgress != null && objectiveProgress.Length == count) return;

        int[] resized = new int[count];
        if (objectiveProgress != null)
        {
            Array.Copy(objectiveProgress, resized, Mathf.Min(objectiveProgress.Length, count));
        }
        objectiveProgress = resized;
    }

    /// <summary>指定目标是否已达成</summary>
    public bool IsObjectiveComplete(int index)
    {
        if (definition == null) return false;
        if (index < 0 || index >= definition.objectives.Count) return false;
        return objectiveProgress[index] >= definition.objectives[index].requiredCount;
    }

    /// <summary>是否所有目标均已达成（未绑定配置或没有目标时返回 false，避免误判完成）</summary>
    public bool AllObjectivesComplete
    {
        get
        {
            if (definition == null || definition.objectives.Count == 0) return false;

            for (int i = 0; i < definition.objectives.Count; i++)
            {
                if (!IsObjectiveComplete(i)) return false;
            }
            return true;
        }
    }

    /// <summary>取指定目标的当前进度（越界返回 0）</summary>
    public int GetProgress(int index)
    {
        if (objectiveProgress == null) return 0;
        return index >= 0 && index < objectiveProgress.Length ? objectiveProgress[index] : 0;
    }

    /// <summary>
    /// 增加目标进度（钳制到 requiredCount，不会溢出）
    /// </summary>
    /// <returns>进度是否真的发生变化，供上层决定是否广播刷新</returns>
    public bool AddProgress(int index, int amount)
    {
        if (definition == null || amount <= 0) return false;
        if (index < 0 || index >= objectiveProgress.Length) return false;

        int max = definition.objectives[index].requiredCount;
        int before = objectiveProgress[index];
        if (before >= max) return false;

        objectiveProgress[index] = Mathf.Min(max, before + amount);
        return objectiveProgress[index] != before;
    }
}