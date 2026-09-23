using System;
using UnityEngine;

/// <summary>
/// 玩家成长数据：金币与经验
/// 作为奖励系统的接收方，数值变化通过事件通知 UI
/// 等级/经验曲线属于后续阶段，当前只做数值累积
/// </summary>
public class PlayerProgression : MonoBehaviour
{
    [Header("成长数据")]
    [Tooltip("金币")]
    [SerializeField] private int gold;

    [Tooltip("经验值")]
    [SerializeField] private int exp;

    /// <summary>当前金币</summary>
    public int Gold => gold;

    /// <summary>当前经验值</summary>
    public int Exp => exp;

    /// <summary>金币变化事件（参数为当前金币），UI 监听刷新显示</summary>
    public event Action<int> OnGoldChanged;

    /// <summary>经验变化事件（参数为当前经验），UI 监听刷新显示</summary>
    public event Action<int> OnExpChanged;

    /// <summary>增加金币</summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        gold += amount;
        OnGoldChanged?.Invoke(gold);
    }

    /// <summary>增加经验</summary>
    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        exp += amount;
        OnExpChanged?.Invoke(exp);
    }
}