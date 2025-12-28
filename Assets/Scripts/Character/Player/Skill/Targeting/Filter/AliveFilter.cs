using Character;
using Character.Player.Skill.Targeting;
using UnityEngine;
[CreateAssetMenu(
    fileName = "Alive Filter",
    menuName = "Skill/Targeting/Filters/Alive")]
/// <summary>
/// 存活过滤器：仅允许存活的目标通过
/// </summary>
public class AliveFilter : TargetFilterSO
{
    public override bool IsValid(SkillTargetContext ctx, CharacterBase target)
    {
        // 添加空值检查以避免空引用
        if (target == null) return false;
        var health = target.GetComponent<HealthComponent>();
        if (health == null) return false;
        return !health.IsDead;
    }
}
