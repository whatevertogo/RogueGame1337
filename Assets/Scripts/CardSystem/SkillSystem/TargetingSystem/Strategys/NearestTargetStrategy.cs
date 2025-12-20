
using System.Collections.Generic;
using Character;
using UnityEngine;





[CreateAssetMenu(fileName = "Nearest Target Strategy", menuName = "Card System/Skill System/Targeting System/Strategies/Nearest Target Strategy")]
public class NearestTargetStrategy : TargetAcquireSO
{
    [SerializeField] private float Range;
    [SerializeField] private LayerMask targetMask;
    public override List<CharacterBase> Acquire(SkillTargetContext ctx)
    {
        Collider[] hitColliders = Physics.OverlapSphere(ctx.Caster.transform.position, Range, targetMask);
        CharacterBase nearestTarget = null;
        float nearestDistanceSqr = float.MaxValue;

        foreach (var collider in hitColliders)
        {
            CharacterBase character = collider.GetComponent<CharacterBase>();
            if (character != null)
            {
                float distanceSqr = (character.transform.position - ctx.Caster.transform.position).sqrMagnitude;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearestTarget = character;
                }
            }
        }

        return nearestTarget != null ? new List<CharacterBase> { nearestTarget } : new List<CharacterBase>();
    }
}