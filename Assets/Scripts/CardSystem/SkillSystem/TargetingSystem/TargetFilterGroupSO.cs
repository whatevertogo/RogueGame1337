using System.Collections.Generic;
using Character;
using UnityEngine;


[CreateAssetMenu(
    fileName = "Target Filter Group",
    menuName = "Skill/Targeting/Filters/Filter Group")]
public class TargetFilterGroupSO : ScriptableObject
{
    public List<TargetFilterSO> filters;

    public bool IsValid(SkillTargetContext ctx, CharacterBase target)
    {
        foreach (var filter in filters)
        {
            if (filter != null && !filter.IsValid(ctx, target))
                return false;
        }
        return true;
    }
}
