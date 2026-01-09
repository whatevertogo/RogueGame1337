using Character;
using Character.Components;
using RogueGame.GameConfig;
using UnityEngine;

/// <summary>
/// 难度系统服务
/// 职责：统一管理难度缩放逻辑，为敌人提供属性和数量缩放
/// </summary>
public sealed class DifficultyService
{
    private readonly DifficultyCurveConfig difficultyConfig;

    public DifficultyService(DifficultyCurveConfig difficultyConfig)
    {
        this.difficultyConfig = difficultyConfig;
    }

    /// <summary>
    /// 检查难度配置是否有效
    /// </summary>
    public bool IsConfigValid()
    {
        return difficultyConfig != null;
    }

    /// <summary>
    /// 检查指定层数是否启用难度缩放
    /// </summary>
    public bool IsDifficultyEnabled(int floor)
    {
        if (!IsConfigValid())
            return false;

        return floor >= difficultyConfig.startFloor;
    }

    /// <summary>
    /// 获取生命值缩放系数
    /// </summary>
    public float GetHpMultiplier(int floor)
    {
        if (!IsConfigValid())
            return 1f;

        return difficultyConfig.GetHpMultiplier(floor);
    }

    /// <summary>
    /// 获取攻击力缩放系数
    /// </summary>
    public float GetAttackPowerMultiplier(int floor)
    {
        if (!IsConfigValid())
            return 1f;

        return difficultyConfig.GetAttackPowerMultiplier(floor);
    }

    /// <summary>
    /// 获取防御力缩放系数
    /// </summary>
    public float GetDefenseMultiplier(int floor)
    {
        if (!IsConfigValid())
            return 1f;

        return difficultyConfig.GetDefenseMultiplier(floor);
    }

    /// <summary>
    /// 获取攻击速度缩放系数
    /// </summary>
    public float GetAttackSpeedMultiplier(int floor)
    {
        if (!IsConfigValid())
            return 1f;

        return difficultyConfig.GetAttackSpeedMultiplier(floor);
    }

    /// <summary>
    /// 应用难度缩放到敌人属性
    /// </summary>
    /// <param name="enemy">敌人 GameObject</param>
    /// <param name="floor">当前层数</param>
    /// <param name="source">缩放来源（用于 StatModifier）</param>
    public void ApplyDifficultyScaling(GameObject enemy, int floor, object source)
    {
        if (enemy == null || !IsDifficultyEnabled(floor))
            return;

        var stats = enemy.GetComponent<CharacterStats>();
        if (stats == null)
        {
            CDTU.Utils.CDLogger.LogWarning(
                $"[DifficultyService] 敌人 {enemy.name} 缺少 CharacterStats 组件，跳过难度缩放"
            );
            return;
        }

        // 获取各属性缩放系数
        float hpMultiplier = GetHpMultiplier(floor);
        float atkMultiplier = GetAttackPowerMultiplier(floor);
        float armorMultiplier = GetDefenseMultiplier(floor);
        float atkSpeedMultiplier = GetAttackSpeedMultiplier(floor);

        // 应用属性缩放（使用 PercentAdd 类型）
        if (hpMultiplier > 1f)
        {
            var hpMod = new StatModifier(hpMultiplier - 1f, StatModType.PercentAdd, source);
            stats.MaxHP.AddModifier(hpMod);
        }

        if (atkMultiplier > 1f)
        {
            var atkMod = new StatModifier(atkMultiplier - 1f, StatModType.PercentAdd, source);
            stats.AttackPower.AddModifier(atkMod);
        }

        if (armorMultiplier > 1f)
        {
            var defMod = new StatModifier(armorMultiplier - 1f, StatModType.PercentAdd, source);
            stats.Armor.AddModifier(defMod);
        }

        if (atkSpeedMultiplier > 1f)
        {
            var speedMod = new StatModifier(
                atkSpeedMultiplier - 1f,
                StatModType.PercentAdd,
                source
            );
            stats.AttackSpeed.AddModifier(speedMod);
        }

        // 治疗到满血，确保生命值缩放生效
        stats.FullHeal();

        CDTU.Utils.CDLogger.Log(
            $"[DifficultyService] 已应用 {floor} 层难度缩放到 {enemy.name}："
                + $"HP×{hpMultiplier:F2} ATK×{atkMultiplier:F2} DEF×{armorMultiplier:F2} SPD×{atkSpeedMultiplier:F2}"
        );
    }

    /// <summary>
    /// 计算缩放后的敌人数量
    /// </summary>
    /// <param name="floor">当前层数</param>
    /// <param name="baseCount">基础敌人数量</param>
    /// <returns>缩放后的敌人数量</returns>
    public int GetScaledEnemyCount(int floor, int baseCount)
    {
        if (!IsConfigValid())
            return baseCount;

        return difficultyConfig.GetScaledEnemyCount(floor, baseCount);
    }

    /// <summary>
    /// 获取基础难度缩放系数（用于调试或显示）
    /// </summary>
    public float GetDifficultyScaling(int floor)
    {
        if (!IsConfigValid())
            return 1f;

        return difficultyConfig.GetDifficultyScaling(floor);
    }
}
