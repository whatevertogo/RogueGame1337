using UnityEngine;

namespace CardSystem.SkillSystem
{
    [CreateAssetMenu(fileName = "ProjectileExecutor", menuName = "Card System/Executors/Projectile")]
    public class ProjectileExecutor : SkillExecutorSO
    {
        public GameObject projectilePrefab;

        
        // TODO: 实现投射物创建与发射逻辑
        public override void Execute(SkillDefinition skill, SkillContext ctx)
        {
            
        }
    }
}
