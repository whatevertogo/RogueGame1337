
using Character;
using UnityEngine;



public enum TargetFaction
{
    Enemy,
    Friendly,
    Both
}

[CreateAssetMenu(
    fileName = "Faction Filter",
    menuName = "Skill/Targeting/Filters/Faction")]
public class FactionFliter : TargetFilterSO
{
    [SerializeField] private TargetFaction targetFaction = TargetFaction.Enemy;

    public override bool IsValid(SkillTargetContext ctx, CharacterBase target)
    {
        // 保护性空检查（SkillTargetContext 是 struct，检查 Caster 与 target 是否为 null）
        if (ctx.Caster == null || target == null)
            return false;

        bool isEnemy = ctx.Caster.Team != target.Team;

        return targetFaction switch
        {
            TargetFaction.Enemy => isEnemy,
            TargetFaction.Friendly => !isEnemy,
            TargetFaction.Both => true,
            _ => false
        };
    }
}