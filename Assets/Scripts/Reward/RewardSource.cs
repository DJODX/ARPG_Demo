using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 奖励来源：挂在敌人身上，被玩家击杀后按配置发放奖励
/// 观察 AttributeComponent.OnDeath，通过伤害来源判定击杀归属，非玩家击杀不发放
/// </summary>
[RequireComponent(typeof(AttributeComponent))]
public class RewardSource : MonoBehaviour
{
    [Tooltip("击杀奖励配置（金币/经验/道具，可配多条）")]
    [SerializeField] private List<RewardEntry> rewards = new List<RewardEntry>();

    private AttributeComponent _attribute;

    private void Awake()
    {
        _attribute = GetComponent<AttributeComponent>();
    }

    private void OnEnable()
    {
        _attribute.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        _attribute.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        // 伤害来源是攻击者的武器 Hitbox，需向上查找实体确认是玩家击杀
        GameObject attacker = _attribute.LastAttacker;
        PlayerProgression receiver = attacker != null ? attacker.GetComponentInParent<PlayerProgression>() : null;
        if (receiver == null) return;

        RewardManager.Instance.GrantAll(rewards, receiver, gameObject);
    }
}