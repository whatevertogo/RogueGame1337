
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
public class FactionFilter : TargetFilterSO
{
    [SerializeField] private TargetFaction targetFaction = TargetFaction.Enemy;

    public override bool IsValid(SkillTargetContext ctx, CharacterBase target)
    {
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