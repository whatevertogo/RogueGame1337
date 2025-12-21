using Character;
using UnityEngine;
[CreateAssetMenu(
    fileName = "Alive Filter",
    menuName = "Skill/Targeting/Filters/Alive")]
public class AliveFliter : TargetFilterSO
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