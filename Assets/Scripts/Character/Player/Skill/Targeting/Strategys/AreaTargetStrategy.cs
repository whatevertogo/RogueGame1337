


using System.Collections.Generic;
using Card.SkillSystem.TargetingSystem;
using Character;
using Character.Player.Skill.Targeting;
using UnityEngine;





[CreateAssetMenu(fileName = "Area Target Strategy", menuName = "Card System/Skill System/Targeting System/Strategies/Area Target Strategy")]
public class AreaTargetStrategy : TargetAcquireSO
{
    [SerializeField] private float Radius;
    [SerializeField] private LayerMask targetMask;
    public override List<CharacterBase> Acquire(SkillTargetContext ctx)
    {
        // 保护性检查：优先确保 ctx.Caster 或 AimPoint 合理（ctx 为 struct）
        Vector3 aimPoint = ctx.AimPoint;

        Collider[] hitColliders = Physics.OverlapSphere(aimPoint, Radius, targetMask);
        List<CharacterBase> targets = new List<CharacterBase>();

        foreach (var collider in hitColliders)
        {
            if (collider == null) continue;
            CharacterBase character = collider.GetComponent<CharacterBase>();
            if (character != null)
            {
                targets.Add(character);
            }
        }

        return targets;
    }


}