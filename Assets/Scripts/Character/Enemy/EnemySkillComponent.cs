
// using System.Collections.Generic;
// using UnityEngine;
// using Character.Components.Interface;

// namespace Character.Components
// {
//     /// <summary>
//     /// 敌人技能组件：托管多个 SkillDefinition 的槽位，负责冷却与触发
//     /// TODO: 实现怪物的技能逻辑
//     /// </summary>
//     public class EnemySkillComponent : MonoBehaviour, ISkillComponent
//     {
//         [Header("怪物拥有的技能")]
//         public List<SkillSlot> skillSlots = new();

//         protected StatusEffectComponent statusEffects;

//         protected virtual void Awake()
//         {
//             statusEffects = GetComponent<StatusEffectComponent>();
//         }

//         public virtual bool CanUseSkill(int slotIndex)
//         {
//             // TODO: 实现冷却检查
//             if (slotIndex < 0 || slotIndex >= skillSlots.Count) return false;
//             var slot = skillSlots[slotIndex];
//             if (slot == null || slot.Runtime == null || slot.Runtime.Skill == null) return false;
            
//             return true;
//         }

//         /// <summary>
//         /// 使用技能。
//         /// 可传入 aimPoint 指定目标点，否则默认使用怪物位置
//         /// </summary>
//         public virtual void UseSkill(int slotIndex, Vector3? aimPoint = null)
//         {
//             if (!CanUseSkill(slotIndex)) return;

//             var slot = skillSlots[slotIndex];
//             var rt = slot?.Runtime;
//             if (rt == null || rt.Skill == null) return;

//             var def = rt.Skill;

//             // 构建上下文
//             var ctx = new SkillTargetContext
//             {
//                 Caster = GetComponent<CharacterBase>(),
//                 AimPoint = aimPoint ?? transform.position,
//             };


//             // 记录使用时间（用于冷却）
//             rt.LastUseTime = Time.time;


//             if (def == null) return;

//             if (def.detectionDelay > 0f)
//             {
//                 StartCoroutine(DelayedExecute(def, ctx));
//             }
//             else
//             {
//                 ExecuteSkillEffects(def, ctx);
//             }
//         }

//         private System.Collections.IEnumerator DelayedExecute(SkillDefinition def, SkillTargetContext ctx)
//         {
//             yield return new WaitForSeconds(def.detectionDelay);
//             ExecuteSkillEffects(def, ctx);
//         }

//         private void ExecuteSkillEffects(SkillDefinition def, SkillTargetContext ctx)
//         {
//             if (def == null) return;

//             // 播放 VFX
//             if (def.vfxPrefab != null)
//             {
//                 try { GameObject.Instantiate(def.vfxPrefab, ctx.AimPoint, Quaternion.identity); } catch { }
//             }

//             // 获取候选目标
//             var targets = new System.Collections.Generic.List<CharacterBase>();
//             if (def.TargetAcquireSO != null)
//             {
//                 var acquired = def.TargetAcquireSO.Acquire(ctx);
//                 if (acquired != null) targets = acquired;
//             }

//             // 过滤
//             var validTargets = targets;
//             if (def.TargetFilters != null && def.TargetFilters.filters != null && def.TargetFilters.filters.Count > 0)
//                 validTargets = targets.FindAll(t => def.TargetFilters.IsValid(ctx, t));

//             // 应用效果
//             foreach (var target in validTargets)
//             {
//                 foreach (var effectDef in def.Effects)
//                 {
//                     if (effectDef == null) continue;
//                     var statusComp = target.GetComponent<StatusEffectComponent>();
//                     if (statusComp != null)
//                     {
//                         var inst = effectDef.CreateInstance();
//                         statusComp.AddEffect(inst);
//                     }
//                 }
//             }
//         }
//     }
// }
