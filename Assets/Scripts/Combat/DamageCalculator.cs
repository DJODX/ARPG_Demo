using UnityEngine;

/// <summary>
/// 伤害计算器（纯静态类，无状态）
/// 独立于任何 MonoBehaviour，便于单元测试和后续扩展（减伤/护盾/属性克制）
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// 计算最终伤害
    /// 公式：damage = max(1, rawDamage - targetDef * 0.5) * 随机浮动(0.9~1.1) * (暴击 ? 暴击倍率 : 1)
    /// </summary>
    /// <param name="rawDamage">原始伤害（攻击力或技能伤害）</param>
    /// <param name="targetDef">目标防御力</param>
    /// <param name="isCrit">是否暴击</param>
    /// <param name="critMult">暴击倍率</param>
    /// <returns>取整后的最终伤害，最小为 1</returns>
    public static float Calculate(float rawDamage, float targetDef, bool isCrit, float critMult)
    {
        // 防御减免，保底 1 点伤害，避免出现 0 伤害导致"打不动"的挫败感
        float baseDamage = Mathf.Max(1f, rawDamage - targetDef * 0.5f);

        // 随机浮动，让伤害更有手感
        float randomFactor = Random.Range(0.9f, 1.1f);
        float finalDamage = baseDamage * randomFactor;

        // 暴击加成
        if (isCrit)
        {
            finalDamage *= critMult;
        }

        return Mathf.Round(finalDamage);
    }
}
