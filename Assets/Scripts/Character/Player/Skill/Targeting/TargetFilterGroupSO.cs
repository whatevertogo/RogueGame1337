using System.Collections.Generic;
using System.Linq;
using Character;
using Character.Player.Skill.Targeting;
using UnityEngine;


[CreateAssetMenu(
    fileName = "TargetFilterGroup",
    menuName = "RogueGame/Skill/Targeting/Filters/TargetFilterGroup")]
public class TargetFilterGroupSO : ScriptableObject
{
    public List<TargetFilterSO> filters;

    public bool IsValid(SkillContext ctx, CharacterBase target)
    {
        // 空列表检查：如果没有过滤器，默认通过
        if (filters == null || !filters.Any()) 
            return true;
        
        foreach (var filter in filters)
        {
            if (filter != null && !filter.IsValid( ctx, target))
                return false;
        }
        return true;
    }
}
