using Character;
using Character.Player.Skill.Pipeline;
using Character.Player.Skill.Pipeline.Phases;
using Character.Player.Skill.Slots;
using Character.Player.Skill.Targeting;
using Cysharp.Threading.Tasks;
using RogueGame.Game.Service;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Character.Player.Skill.Core
{

    /// <summary>
    /// 技能执行器：管理技能执行的完整生命周期
    /// 职责：
    /// 1. 协程包装（处理 detectionDelay）
    /// 2. Token 管理
    /// 3. 能量退还
    /// 4. 技能使用事件回调
    /// </summary>
    public sealed class SkillExecutor
    {
        private readonly InventoryServiceManager _inventory;
        private readonly EffectFactory _effectFactory;
        private readonly SkillPhasePipeline _pipeline;
        private readonly Dictionary<int, SkillExecutionToken> _activeTokens;


        public SkillExecutor(InventoryServiceManager inventory, EffectFactory effectFactory)
        {
            _inventory = inventory;
            _effectFactory = effectFactory;
            _pipeline = BuildPipeline();
            _activeTokens = new Dictionary<int, SkillExecutionToken>();
        }

        /// <summary>
        /// 检查技能是否可执行（纯充能系统，无CD限制）
        /// </summary>
        public bool CanExecute(SkillSlot slot)
        {
            if (slot?.Runtime == null) return false;
            var rt = slot.Runtime;

            // 能量检查
            var config = rt.CachedActiveConfig;
            if (config?.requiresCharge == true)
            {
                if (string.IsNullOrEmpty(rt.InstanceId)) return false;
                var state = _inventory.ActiveCardService.GetCardByInstanceId(rt.InstanceId);
                if (state == null || state.CurrentEnergy < config.energyThreshold)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 执行技能
        /// </summary>
        public void ExecuteSkill(SkillSlot slot, CharacterBase caster, Vector3 aimPoint)
        {
            if (!CanExecute(slot)) return;

            var rt = slot.Runtime;
            var def = rt.Skill;

            // 创建 Token
            var token = new SkillExecutionToken();
            _activeTokens[slot.Index] = token;

            // 创建 Context（使用构造函数以赋值只读字段）
            var targetingConfig =  def.defaultTargeting ;
            var energyCostConfig =  def.defaultEnergyCost;

            var ctx = new SkillContext(
                caster,
                aimPoint,
                (aimPoint - caster.transform.position).normalized,
                slot.Index,
                rt,
                _inventory,
                targetingConfig,
                energyCostConfig,
                DamageResult.Default
            );

            if (caster != null)
            {
                // TODO-播放技能对应动画
                caster.GetComponent<IAnimatorController>()?.PlaySkill(def.animationTrigger);
                // 播放特效
                // TODO-先硬编码，后续改为配置化
                VFXSystem.Instance.PlayAt("BurnSkillVFXPrefab", caster.transform.position, caster.transform,2f);
                // 异步执行技能（Fire-and-Forget 模式）
                ExecuteAsync(def, ctx, token).Forget();
            }
        }

        /// <summary>
        /// 异步执行（处理 detectionDelay）
        /// </summary>
        private async UniTask ExecuteAsync(SkillDefinition def, SkillContext ctx, SkillExecutionToken token)
        {
            // 使用局部可变副本以便传 ref 给 Pipeline（async 方法参数不能是 ref）
            var localCtx = ctx;
            var rt = localCtx.Runtime;

            // 等待 detectionDelay
            if (def.detectionDelay > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(def.detectionDelay));
            }

            // 检查是否被取消
            if (token.IsCancelled)
            {
                HandleCancellation(rt);
                _activeTokens.Remove(localCtx.SlotIndex);
                rt.EnergyConsumed = false;
                return;
            }

            // 执行 Pipeline（同步，传入 ref）
            var result = _pipeline.Execute(localCtx, token);

            // 处理结果（基于 Runtime 状态）
            if (result == SkillPhaseResult.Cancel && rt.EnergyConsumed)
            {
                _inventory.AddEnergy(rt.InstanceId, rt.ActualEnergyConsumed);
                rt.ActualEnergyConsumed = 0;
            }

            // TODO-触发技能使用事件（注意语义见注释）
            // if (result == SkillPhaseResult.Continue || result == SkillPhaseResult.Cancel)
            // {
            // }

            // 清理
            _activeTokens.Remove(localCtx.SlotIndex);
            rt.EnergyConsumed = false;
        }

        /// <summary>
        /// 构建技能执行管道
        /// </summary>
        /// <returns></returns>
        private SkillPhasePipeline BuildPipeline()
        {
            return new SkillPhasePipeline()
                .Add(new EnergyConsumptionPhase(_inventory))//消耗能量
                .Add(new TargetingPhase())//目标获取
                .Add(new VFXPhase())//播放特效
                .Add(new CrossPhase())//跨阶段修改器
                .Add(new DamageCalculationPhase())//伤害计算(底层也是通过effect)
                .Add(new EffectApplicationPhase(_effectFactory));//效果应用
        }
        ///  <summary>
        /// 打断技能
        /// </summary>
        public void Interrupt(int slotIndex, bool refundCharges)
        {
            if (slotIndex < 0)
            {
                // 打断所有技能
                foreach (var kvp in _activeTokens)
                {
                    CancelToken(kvp.Key, refundCharges);
                }
            }
            else
            {
                CancelToken(slotIndex, refundCharges);
            }
        }

        /// <summary>
        /// 打断所有技能
        /// </summary>
        public void InterruptAll(bool refundCharges)
        {
            Interrupt(-1, refundCharges);
        }

        private void CancelToken(int slotIndex, bool refundCharges)
        {
            // 当前实现中，是否退还能量由 ExecuteAsync / HandleCancellation 基于 rt.EnergyConsumed 统一处理，
            // 这里的 refundCharges 仅作为对外 API 语义标记（调用方可以表达“期望退还能量”），暂不改变具体行为。
            _ = refundCharges; // 防止静态分析误报未使用参数

            if (!_activeTokens.TryGetValue(slotIndex, out var token)) return;

            token.Cancel(InterruptReason.ManualInterrupt);
        }

        private void HandleCancellation(Runtime.ActiveSkillRuntime rt)
        {
            if (rt.EnergyConsumed)
            {
                _inventory.AddEnergy(rt.InstanceId, rt.ActualEnergyConsumed);
                rt.ActualEnergyConsumed = 0;
            }
        }


        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _activeTokens.Clear();
        }
    }
}
