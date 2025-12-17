using UnityEngine;

/// <summary>
/// Simple test MonoBehaviour that holds references to the new module types
/// to help verify the compiler can resolve the module types across the codebase.
/// Drop this onto a GameObject in the scene to ensure the types are visible to the compiler.
/// </summary>
public class TestRef : MonoBehaviour
{
    // Fully qualified type references to avoid relying on 'using' directives
    public CardSystem.SkillSystem.Targeting.TargetingModuleSO targetingModule;
    public CardSystem.SkillSystem.Execution.ExecutionModuleSO executionModule;

    void Start()
    {
        Debug.Log($"[TestRef] targetingModule is {(targetingModule == null ? "null" : "not null")}, executionModule is {(executionModule == null ? "null" : "not null")}");
    }
}
