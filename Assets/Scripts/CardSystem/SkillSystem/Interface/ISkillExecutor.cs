

using CardSystem.SkillSystem;

public interface ISkillExecutor
{
     void Execute(SkillDefinition skill, SkillContext ctx);
}