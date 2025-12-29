using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Character.Components;
using Character.Components.Interface;
using Character.Player.Skill.Targeting;
using Character.Player.Skill.Runtime;
using Core.Events;
using RogueGame.Events;

namespace Character.Player
{
    /// <summary>
    /// PlayerSkillComponent - 技能释放部分
    /// </summary>
    public partial class PlayerSkillComponent : MonoBehaviour, ISkillComponent
    {
        private EffectFactory _effectFactory = new EffectFactory();

        // 目标列表池（避免重复创建 List<CharacterBase>）
        private readonly List<CharacterBase> _targetListPool = new List<CharacterBase>();


        #region 技能释放

        /// <summary>
        /// 检查指定槽位的技能冷却是否就绪
        /// </summary>
        private bool IsCooldownReady(int slotIndex)
        {
            if (_noCooldownMode) return true;

            var rt = _playerSkillSlots[slotIndex]?.Runtime;
            if (rt?.Skill == null) return false;

            // 统一使用 EffectiveCooldown 作为冷却依据（已包含修改器影响）
            var effectiveCd = rt.EffectiveCooldown;

            return Time.time - rt.LastUseTime >= effectiveCd;
        }

        /// <summary>
        /// 检查技能是否可用（统一的可用性检查入口）
        /// </summary>
        public bool CanUseSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return false;
            var rt = _playerSkillSlots[slotIndex]?.Runtime;
            if (rt == null || rt.Skill == null) return false;

            // 冷却检查
            if (!IsCooldownReady(slotIndex)) return false;

            // 能量检查（使用缓存配置）
            var config = rt.CachedActiveConfig;
            if (config?.requiresCharge == true)
            {
                if (string.IsNullOrEmpty(rt.InstanceId)) return false;

                var state = _inventory.GetActiveCardState(rt.InstanceId);
                if (state == null || state.CurrentEnergy < config.energyThreshold) return false;
            }

            return true;
        }

        /// <summary>
        /// 使用技能
        /// </summary>
        public void UseSkill(int slotIndex, Vector3? aimPoint = null)
        {
            if (!CanUseSkill(slotIndex)) return;

            var slot = _playerSkillSlots[slotIndex];
            var rt = slot?.Runtime;
            if (rt == null || rt.Skill == null) return;

            var def = rt.Skill;

            // 计算目标点（若未指定，则使用施法者位置）
            Vector3 origin = aimPoint ?? transform.position;

            // 创建上下文占位（Targets 在实际执行时计算）
            var ctx = new SkillTargetContext
            {
                Caster = GetComponent<CharacterBase>(),
                AimPoint = origin,
            };

            // 启动协程处理（包含手动选择 / 消耗 / 冷却 / 延迟执行）
            // 取消并替换已有同槽位的流程，避免并发执行
            if (rt.RunningCoroutine != null)
            {
                StopCoroutine(rt.RunningCoroutine);
                // 取消旧协程时退还能量（如果已消耗），并立刻重置标志，避免重复退还
                if (rt.EnergyConsumed)
                {
                    RestoreEnergyIfConsumed(rt);
                    rt.EnergyConsumed = false;
                }
                rt.RunningCoroutine = null;
            }

            rt.RunningCoroutine = StartCoroutine(ManualSelectAndExecute(def, ctx, slotIndex));
        }

        /// <summary>
        /// 协程化的交互选择 + 执行流程：
        /// - 运行 targetingModule.ManualSelectionCoroutine(ctx)（供模块显示 UI / 高亮 / 等待点击）
        /// - 手动选择完成后尝试消耗卡牌充能（若卡片需要）
        /// - 标记冷却并执行技能（立即或延迟）
        /// </summary>
        private IEnumerator ManualSelectAndExecute(SkillDefinition def, SkillTargetContext ctx, int slotIndex)
        {
            if (def == null) yield break;

            // 确保槽位与 runtime 可用（防御式）
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) yield break;
            var slot = _playerSkillSlots[slotIndex];
            var rt = slot?.Runtime;
            if (rt == null || rt.Skill == null) yield break;

            // 初始化技能上下文并应用修改器（必须在能量消耗之前）
            var caster = GetComponent<CharacterBase>();
            if (caster == null)
            {
                CDTU.Utils.CDLogger.LogError("[PlayerSkillComponent] CharacterBase component not found");
                yield break;
            }

            // 创建技能上下文
            ctx = new SkillTargetContext
            {
                Caster = caster,
                AimPoint = ctx.AimPoint,
                AimDirection = (ctx.AimPoint - transform.position).normalized,
                SlotIndex = slotIndex,

                // 使用嵌套结构的默认值
                Targeting = TargetingConfig.Default,
                EnergyCost = EnergyCostConfig.Default,
                Damage = DamageResult.Default,
            };

            // 应用技能修改器（必须在能量消耗前生效）
            rt.ApplyAllModifiers(ref ctx);

            // 使用缓存配置检查是否需要消耗能量
            var config = rt.CachedActiveConfig;
            bool requiresCharge = config?.requiresCharge == true;

            if (requiresCharge)
            {
                // 消耗技能能量（使用修改器配置后的能量消耗）
                if (string.IsNullOrEmpty(rt.InstanceId)) yield break;

                if (!_inventory.ConsumeSkillEnergy(rt.InstanceId, ctx.EnergyCost))
                {
                    yield break;
                }

                // 计算并保存实际消耗的能量值（用于退还）
                var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(rt.CardId);
                int baseCost = cardDef?.activeCardConfig?.energyThreshold ?? 0;
                rt.ActualEnergyConsumed = ctx.EnergyCost.CalculateFinalCost(baseCost);
                rt.EnergyConsumed = true;
            }

            rt.LastUseTime = Time.time;

            // 触发已使用事件（UI/上层监听）
            OnSkillUsed?.Invoke(slotIndex);

            // 发布技能释放事件（用于被动卡牌触发，例如暴风骤雨）
            var playerRuntimeState = PlayerManager.Instance?.GetPlayerRuntimeStateByController(GetComponent<PlayerController>());
            if (playerRuntimeState != null)
            {
                EventBus.Publish(new PlayerSkillCastEvent(playerRuntimeState.PlayerId, slotIndex, rt.CardId));
            }

            // 开始执行（处理 detectionDelay 和 executor）
            // 将 DelayedExecute 嵌入到当前协程内，保证单一协程句柄可用于取消

            // 保存当前协程引用（用于检测是否被取消）
            var currentCoroutine = rt.RunningCoroutine;
            yield return DelayedExecute(def, ctx, slotIndex);

            // 检查协程是否被取消（RunningCoroutine 被替换为新协程）
            // 即 使用新技能视为取消旧协程
            if (rt.RunningCoroutine != currentCoroutine)
            {
                // 协程被取消，退还能量
                if (rt.EnergyConsumed) RestoreEnergyIfConsumed(rt);
            }

            // 清理运行时协程引用
            rt.RunningCoroutine = null;
            rt.EnergyConsumed = false;
            yield break;
        }

        /// <summary>
        /// 延迟执行技能
        /// </summary>
        private IEnumerator DelayedExecute(SkillDefinition def, SkillTargetContext ctx, int slotIndex)
        {
            // 关键检查：技能定义必须存在
            if (def == null)
            {
                yield break;
            }

            // 等待检测延迟
            if (def.detectionDelay > 0f)
                yield return new WaitForSeconds(def.detectionDelay);

            // 获取施法者（已在组件初始化时验证，运行时应存在）
            var caster = GetComponent<CharacterBase>();
            if (caster == null)
            {
                CDTU.Utils.CDLogger.LogError("[PlayerSkillComponent] CharacterBase component not found");
                yield break;
            }

            var rt = _playerSkillSlots[slotIndex]?.Runtime;

            // 播放 VFX（如果有）
            if (def.vfxPrefab != null)
            {
                // TODO-临时用transform.position，后续可改为技能定义中的挂点
                Instantiate(def.vfxPrefab, transform.position, Quaternion.identity);
            }

            // 获取目标
            var targets = new List<CharacterBase>();
            if (def.TargetAcquireSO != null)
            {
                targets = def.TargetAcquireSO.Acquire(ctx);
            }

            // 应用过滤器（如果存在）
            var validTargets = targets;
            if (def.TargetFilters != null && def.TargetFilters.filters != null && def.TargetFilters.filters.Count > 0)
            {
                validTargets = targets.FindAll(t => def.TargetFilters.IsValid(ctx, t));
            }

            // 如果没有目标则直接返回
            if (validTargets == null || validTargets.Count == 0)
            {
                yield break;
            }

            // 应用效果到所有有效目标
            if (def.Effects == null || def.Effects.Count == 0)
            {
                yield break;
            }

            foreach (var target in validTargets)
            {
                if (target == null) continue;

                var statusComp = target.GetComponent<StatusEffectComponent>();
                if (statusComp == null)
                {
                    continue;
                }

                // 应用所有效果
                foreach (var effectDef in def.Effects)
                {
                    if (effectDef == null) continue;

                    var effectInstance = _effectFactory.CreateInstance(effectDef, caster);
                    if (effectInstance == null)
                    {
                        continue;
                    }

                    statusComp.AddEffect(effectInstance);
                }
            }

            yield break;
        }

        /// <summary>
        /// 退还能量（协程取消时调用）
        /// </summary>
        private void RestoreEnergyIfConsumed(ActiveSkillRuntime rt)
        {
            if (string.IsNullOrEmpty(rt.InstanceId) || rt.ActualEnergyConsumed <= 0) return;

            _inventory.AddEnergy(rt.InstanceId, rt.ActualEnergyConsumed);
            rt.ActualEnergyConsumed = 0;
        }

        #endregion
    }
}
