using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 存档根模型：所有需要持久化的数据集中在这里，配合 JsonUtility 直接读写
/// 原则：只存 ID 与数值，不持有 ScriptableObject / 场景对象引用；
///       读档时由各系统按 ID 重新解析，避免存档绑定资产实例
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>当前存档格式版本（结构变化时 +1，读档时按版本做迁移）</summary>
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;

    /// <summary>保存时间（UTC Ticks，展示时再转本地时间）</summary>
    public long savedAtTicks;

    /// <summary>存档时所在场景名</summary>
    public string sceneName;

    /// <summary>玩家数值与位置</summary>
    public PlayerSaveData player = new PlayerSaveData();

    /// <summary>背包格子（只存 itemId + 数量，读档时由物品库重新解析）</summary>
    public List<ItemStack> inventory = new List<ItemStack>();

    /// <summary>装备槽物品 ID，下标与 EquipmentType 对应，0 表示空槽</summary>
    public int[] equipment = new int[EquipmentSlots.SlotCount];

    /// <summary>任务进度（QuestRuntime 本身可序列化，definition 不参与）</summary>
    public List<QuestRuntime> quests = new List<QuestRuntime>();

    /// <summary>音量与静音设置</summary>
    public AudioSaveData audio = new AudioSaveData();
}

/// <summary>
/// 玩家存档数据：位置、朝向与核心数值
/// 只记录"恢复后不会与配置冲突"的运行时值，最大生命等基础属性仍以配置为准
/// </summary>
[Serializable]
public class PlayerSaveData
{
    public Vector3 position;
    public float rotationY;
    public float hp;
    public float mp;
    public int gold;
    public int exp;
}

/// <summary>
/// 音效设置存档：音量与静音状态
/// 默认值与 AudioManager 的初始配置一致，旧存档缺少该字段时按默认值处理
/// </summary>
[Serializable]
public class AudioSaveData
{
    public float bgmVolume = 0.5f;
    public float sfxVolume = 1f;
    public bool bgmMuted;
    public bool sfxMuted;
}
