using Character;
using UnityEngine;
[CreateAssetMenu(
    fileName = "Alive Filter",
    menuName = "Skill/Targeting/Filters/Alive")]
public class AliveFilter : TargetFilterSO
{
    public override bool IsValid(SkillTargetContext ctx, CharacterBase target)
    {
        return !target.GetComponent<HealthComponent>().IsDead;
    }
}