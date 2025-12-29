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


        #region 技能释放

        /// <summary>
        /// 检查指定槽位的技能冷却是否就绪
        /// </summary>
        private bool IsCooldownReady(int slotIndex)
        {
            if (SkillLimiter.IsNoCooldown) return true;

            var rt = _playerSkillSlots[slotIndex]?.Runtime;
            if (rt?.Skill == null) return false;

            var baseCd = rt.Skill.cooldown;
            var stats = GetComponent<CharacterStats>();
            var reduction = stats?.SkillCooldownReductionRate.Value ?? 0f;
            var effectiveCd = Mathf.Max(0f, baseCd * (1f - reduction));

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

            // 房间规则限制
            if (!SkillLimiter.CanUseSkill(slotIndex)) return false;

            // 冷却检查
            if (!IsCooldownReady(slotIndex)) return false;

            // 能量检查（使用缓存配置）
            var config = rt.CachedActiveConfig;
            if (config?.requiresCharge == true)
            {
                if (_inventory == null || string.IsNullOrEmpty(rt.InstanceId)) return false;

                var state = _inventory.GetActiveCardState(rt.InstanceId);
                if (state == null || state.CurrentCharges < config.energyThreshold) return false;
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

            // 使用缓存配置检查是否需要消耗能量
            var config = rt.CachedActiveConfig;
            bool requiresCharge = config?.requiresCharge == true;

            if (requiresCharge)
            {
                // 消耗技能能量
                if (_inventory == null || string.IsNullOrEmpty(rt.InstanceId)) yield break;

                if (!_inventory.ConsumeSkillEnergy(rt.InstanceId))
                {
                    yield break;
                }
            }

            // 标记为本房间已使用（房间内一次性规则）并记录使用时间（用于冷却计算）
            // 使用 SkillLimiter 而不是直接设置 rt.UsedInCurrentRoom
            SkillLimiter.MarkSkillUsed(slotIndex);
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
            yield return DelayedExecute(def, ctx.AimPoint, slotIndex);

            // 清理运行时协程引用
            rt.RunningCoroutine = null;
            yield break;
        }

        /// <summary>
        /// 延迟执行技能
        /// </summary>
        private IEnumerator DelayedExecute(SkillDefinition def, Vector3? aimPoint, int slotIndex)
        {
            // 关键检查：技能定义必须存在
            if (def == null)
            {
                yield break; 
            }

            // 等待检测延迟
            if (def.detectionDelay > 0f)
                yield return new WaitForSeconds(def.detectionDelay);

            // 计算目标点（若未指定，则使用施法者位置）
            Vector3 origin = aimPoint ?? transform.position;

            // 获取施法者（已在组件初始化时验证，运行时应存在）
            var caster = GetComponent<CharacterBase>();
            if (caster == null)
            {
                CDTU.Utils.CDLogger.LogError("[PlayerSkillComponent] CharacterBase component not found");
                yield break;
            }

            // 获取运行时状态以应用修改器
            var rt = _playerSkillSlots[slotIndex]?.Runtime;

            // 创建技能上下文
            var ctx = new SkillTargetContext
            {
                Caster = caster,
                AimPoint = origin,
                AimDirection = (origin - transform.position).normalized,
                PowerMultiplier = 1.0f // 可扩展：从 Buff 或技能等级读取
            };

            // 应用技能修改器（如果存在）
            rt?.ApplyAllModifiers(ref ctx);

            // 播放 VFX（如果有）
            if (def.vfxPrefab != null)
            {
                // TODO-临时用transform.position，后续可改为技能定义中的挂点
                var vfx = Instantiate(def.vfxPrefab, transform.position, Quaternion.identity);
                var vfxsr = vfx.GetComponent<SpriteRenderer>();
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

        #endregion
    }
}
