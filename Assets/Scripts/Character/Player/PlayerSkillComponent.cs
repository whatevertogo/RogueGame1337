using System;
using UnityEngine;
using Character.Components;
using Character.Components.Interface;
using System.Collections;
using RogueGame.Events;

namespace Character.Player
{
    /// <summary>
    /// 玩家技能组件：管理技能槽位、冷却与触发
    /// </summary>
    public class PlayerSkillComponent : MonoBehaviour, ISkillComponent
    {
        [SerializeField, ReadOnly]
        private SkillSlot[] _playerSkillSlots = new SkillSlot[2];

        public SkillSlot[] PlayerSkillSlots => _playerSkillSlots;

        // 保留事件签名以兼容现有绑定，但不触发任何事件
        public event Action<int, float> OnEnergyChanged;
        public event Action<int> OnSkillUsed;
        public event Action<int, string> OnSkillEquipped;
        public event Action<int> OnSkillUnequipped;
        private void Awake()
        {
            // 确保数组内的 SkillSlot 实例已初始化，避免 Inspector VS 运行期不一致
            if (_playerSkillSlots != null)
            {
                for (int i = 0; i < _playerSkillSlots.Length; i++)
                {
                    if (_playerSkillSlots[i] == null)
                        _playerSkillSlots[i] = new SkillSlot();
                }
            }
            EventBus.Subscribe<ClearAllSlotsRequestedEvent>(evt => 
            {
                // 清理所有槽位
                for (int i = 0; i < _playerSkillSlots.Length; i++)
                {
                    UnequipActiveCardBySlotIndex(i);
                }
            });

            EventBus.Subscribe<PlayerSlotCardChangedEvent>(OnPlayerSlotCardChanged);
        }

        private void OnPlayerSlotCardChanged(PlayerSlotCardChangedEvent @event)
        {
            var ps = PlayerManager.Instance?.GetLocalPlayerState();
            if (ps == null) return;
            if (@event.PlayerId != ps.PlayerId) return;

            int slotIndex = @event.SlotIndex;
            string newCardId = @event.NewCardId;

            if (string.IsNullOrEmpty(newCardId))
            {
                // 取消装备
                UnequipActiveCardBySlotIndex(slotIndex);
            }
            else
            {
                // 装备新卡
                EquipActiveCardToSlotIndex(slotIndex, newCardId);
            }
        }

        public bool CanUseSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return false;
            var rt = _playerSkillSlots[slotIndex]?.Runtime;
            if (rt == null || rt.Skill == null) return false;
            // 房间内只能使用一次
            if (rt.UsedInCurrentRoom) return false;
            // 冷却判断
            var last = rt.LastUseTime;
            var baseCd = rt.Skill != null ? rt.Skill.cooldown : 0f;
            // 从角色属性获取冷却减速率（例如 0.2 表示冷却减少 20% -> 实际冷却 80%）
            var stats = GetComponent<CharacterStats>();
            var reduction = stats != null ? stats.SkillCooldownReductionRate.Value : 0f;
            var effectiveCd = Mathf.Max(0f, baseCd * (1f - reduction));
            if (Time.time - last < effectiveCd) return false;

            //TODO完善

            return true;
        }

        private IEnumerator DelayedExecute(SkillDefinition def, Vector3? aimPoint, int slotIndex)
        {
            // 修复1: 添加技能定义空值检查日志
            if (def == null)
            {
                Debug.LogWarning("[PlayerSkillComponent] DelayedExecute: SkillDefinition is null");
                yield break;
            }

            // 等待检测延迟
            if (def.detectionDelay > 0f)
                yield return new WaitForSeconds(def.detectionDelay);

            // 计算目标点（若未指定，则使用施法者位置）
            Vector3 origin = aimPoint ?? transform.position;

            // 修复2: 改进链式调用安全性并添加日志
            string cardId = null;
            if (_playerSkillSlots != null && slotIndex >= 0 && slotIndex < _playerSkillSlots.Length)
            {
                var slot = _playerSkillSlots[slotIndex];
                if (slot?.Runtime != null)
                {
                    cardId = slot.Runtime.CardId;
                    if (string.IsNullOrEmpty(cardId))
                    {
                        Debug.LogWarning($"[PlayerSkillComponent] DelayedExecute: CardId is null or empty for slot {slotIndex}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[PlayerSkillComponent] DelayedExecute: Runtime is null for slot {slotIndex}");
                }
            }
            else
            {
                Debug.LogWarning($"[PlayerSkillComponent] DelayedExecute: Invalid slot index {slotIndex} or _playerSkillSlots is null");
            }

            // 修复3: 添加CharacterBase组件空值检查
            var caster = GetComponent<CharacterBase>();
            if (caster == null)
            {
                Debug.LogError("[PlayerSkillComponent] DelayedExecute: CharacterBase component not found on gameObject");
                yield break;
            }

            var ctx = new SkillTargetContext
            {
                Caster = caster,
                AimPoint = origin,
            };

            // 播放 VFX（如果有）
            if (def.vfxPrefab != null)
            {
                try
                {
                    GameObject.Instantiate(def.vfxPrefab, origin, Quaternion.identity);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[PlayerSkillComponent] DelayedExecute: Failed to instantiate VFX prefab: {ex.Message}");
                }
            }

            // 修复4: 改进目标获取逻辑并添加详细日志
            var targets = new System.Collections.Generic.List<CharacterBase>();
            if (def.TargetAcquireSO != null)
            {
                try
                {
                    var acquired = def.TargetAcquireSO.Acquire(ctx);
                    if (acquired != null)
                    {
                        targets = acquired;
                    }
                    else
                    {
                        Debug.LogWarning("[PlayerSkillComponent] DelayedExecute: TargetAcquireSO.Acquire returned null");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[PlayerSkillComponent] DelayedExecute: Exception during target acquisition: {ex.Message}");
                }
            }
            else
            {
                Debug.Log("[PlayerSkillComponent] DelayedExecute: No TargetAcquireSO defined");
            }

            // 修复5: 添加效果定义空值检查
            if (def.Effects == null)
            {
                Debug.LogWarning("[PlayerSkillComponent] DelayedExecute: SkillDefinition.Effects is null");
                yield break;
            }

            // 应用过滤器（若存在过滤组）
            var validTargets = targets;
            if (def.TargetFilters != null && def.TargetFilters.filters != null && def.TargetFilters.filters.Count > 0)
            {
                try
                {
                    validTargets = targets.FindAll(t => def.TargetFilters.IsValid(ctx, t));
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[PlayerSkillComponent] DelayedExecute: Exception during target filtering: {ex.Message}");
                    validTargets = targets; // 失败时使用未过滤的目标
                }
            }

            // 如果没有目标则直接返回（策略可能返回 null/空）
            if (validTargets == null || validTargets.Count == 0)
            {
                Debug.Log("[PlayerSkillComponent] DelayedExecute: No valid targets found, skipping effect application");
                yield break;
            }

            // 修复6: 添加状态效果组件空值检查
            foreach (var target in validTargets)
            {
                if (target == null)
                {
                    Debug.LogWarning("[PlayerSkillComponent] DelayedExecute: Found null target in validTargets list");
                    continue;
                }

                // 应用效果
                foreach (var effectDef in def.Effects)
                {
                    if (effectDef != null)
                    {
                        var statusComp = target.GetComponent<StatusEffectComponent>();
                        if (statusComp != null)
                        {
                            try
                            {
                                var effectInstance = effectDef.CreateInstance();
                                    if (effectInstance != null)
                                    {
                                        // 尝试在运行时为效果设置伤害来源（避免对具体实现硬编码引用）
                                        try
                                        {
                                            var mi = effectInstance.GetType().GetMethod("SetDamageSource", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                            if (mi != null)
                                            {
                                                mi.Invoke(effectInstance, new object[] { caster });
                                            }
                                        }
                                        catch (System.Exception ex)
                                        {
                                            // 仅在反射调用失败时记录警告，避免大量日志
                                            Debug.LogWarning($"[PlayerSkillComponent] Failed to set damage source via reflection: {ex.Message}");
                                        }

                                        statusComp.AddEffect(effectInstance);
                                    }
                                else
                                {
                                    Debug.LogWarning($"[PlayerSkillComponent] DelayedExecute: Failed to create effect instance for {effectDef.GetType().Name}");
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"[PlayerSkillComponent] DelayedExecute: Exception applying effect {effectDef.GetType().Name}: {ex.Message}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[PlayerSkillComponent] DelayedExecute: Target {target.name} has no StatusEffectComponent");
                        }
                    }
                }
            }

            yield break;
        }

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
            try
            {
                if (rt.RunningCoroutine != null)
                {
                    StopCoroutine(rt.RunningCoroutine);
                    rt.RunningCoroutine = null;
                }
            }
            catch { }

            rt.RunningCoroutine = StartCoroutine(ManualSelectAndExecute(def, ctx, slotIndex));
        }


        public void EquipActiveCardToSlotIndex(int slotIndex, string cardId)
        {
            // 不查 Inventory
            var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(cardId);
            if (cardDef == null)
            {
                Debug.LogWarning($"[PlayerSkillComponent] Equip failed: cardDef for id '{cardId}' is null. slotIndex={slotIndex}");
                return;
            }

            var skillDef = cardDef.activeCardConfig.skill;
            if (_playerSkillSlots[slotIndex] == null)
                _playerSkillSlots[slotIndex] = new SkillSlot();

            // 获取或创建 Inventory 中的 ActiveCardState 实例，并把实例 id 关联到 runtime
            var inv = InventoryManager.Instance;
            string instanceId = null;
            if (inv != null)
            {
                var existing = inv.GetFirstInstanceByCardId(cardId);
                if (existing != null)
                {
                    instanceId = existing.instanceId;
                }
                else
                {
                    instanceId = inv.AddActiveCardInstance(cardId, 0);
                }

                // 尝试把实例标记为被此玩家装备（若能找到 playerId）
                var pc = GetComponent<PlayerController>();
                if (pc != null)
                {
                    var pr = PlayerManager.Instance?.GetPlayerRuntimeStateByController(pc);
                    if (pr != null)
                    {
                        inv.MarkInstanceEquipped(instanceId, pr.PlayerId);
                    }
                }
            }

            _playerSkillSlots[slotIndex].Equip(new ActiveSkillRuntime(cardId, skillDef, instanceId));
            OnSkillEquipped?.Invoke(slotIndex, cardId);
        }

        public void UnequipActiveCardBySlotIndex(int slotIndex)
        {
            var slot = _playerSkillSlots[slotIndex];
            if (slot == null) return;
            var instanceId = slot.Runtime?.InstanceId;
            var cardId = slot.Runtime?.CardId;
            // 取消装备标记
            if (!string.IsNullOrEmpty(instanceId))
            {
                InventoryManager.Instance?.MarkInstanceEquipped(instanceId, null);
            }
            slot.Clear();
            OnSkillUnequipped?.Invoke(slotIndex);
        }

        /// <summary>
        /// 当进入新房间时RoomManager激活事件PlayerManager接收事件调用：重置本房间使用标记（但保留能量值）
        /// </summary>
        public void OnRoomEnter()
        {
            if (_playerSkillSlots == null) return;
            for (int i = 0; i < _playerSkillSlots.Length; i++)
            {
                var s = _playerSkillSlots[i];
                var rt = s?.Runtime;
                if (rt == null) continue;
                rt.UsedInCurrentRoom = false;
                // TODO-通知 UI 刷新（能量保留）
            }
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

            // 如果卡牌要求消耗能量（或充能），在这里检查并消耗
            bool requiresCharge = false;
            var cdLookup = GameRoot.Instance?.CardDatabase?.Resolve(rt.CardId);
            if (cdLookup != null && cdLookup.activeCardConfig != null)
            {
                requiresCharge = cdLookup.activeCardConfig.requiresCharge;
            }

            if (requiresCharge)
            {
                // 检查并消耗实例充能（由 InventoryManager 管理）
                var inv = InventoryManager.Instance;
                if (inv == null || string.IsNullOrEmpty(rt.InstanceId)) yield break;

                // 试着消耗 1 个充能（用失败表示不可施法）
                if (!inv.TryConsumeCharge(rt.InstanceId, 1, out int remaining))
                {
                    yield break;
                }

                // 通知 UI 当前剩余充能（归一化到 0..1）
                var cd2 = GameRoot.Instance?.CardDatabase?.Resolve(rt.CardId);
                int max = 1;
                if (cd2 != null && cd2.activeCardConfig != null)
                    max = Mathf.Max(1, cd2.activeCardConfig.maxCharges);
                float norm = max > 0 ? (float)remaining / max : 0f;
                OnEnergyChanged?.Invoke(slotIndex, norm);
            }

            // 标记为本房间已使用（房间内一次性规则）并记录使用时间（用于冷却计算）
            rt.UsedInCurrentRoom = true;
            rt.LastUseTime = Time.time;

            // 触发已使用事件（UI/上层监听）
            OnSkillUsed?.Invoke(slotIndex);

            // 开始执行（处理 detectionDelay 和 executor）
            // 将 DelayedExecute 嵌入到当前协程内，保证单一协程句柄可用于取消
            yield return DelayedExecute(def, ctx.AimPoint, slotIndex);

            // 清理运行时协程引用
            try { rt.RunningCoroutine = null; } catch { }
            yield break;
        }

        /// <summary>
        /// 取消指定槽位正在运行的技能协程（若有）
        /// </summary>
        public void CancelSlotCoroutine(int slotIndex)
        {
            if (_playerSkillSlots == null) return;
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return;
            var rt = _playerSkillSlots[slotIndex]?.Runtime;
            if (rt == null) return;
            if (rt.RunningCoroutine != null)
            {
                try
                {
                    StopCoroutine(rt.RunningCoroutine);
                }
                catch { }
                rt.RunningCoroutine = null;
            }
        }

        /// <summary>
        /// 取消所有槽位的技能协程（用于死亡/禁用时）
        /// </summary>
        public void CancelAllSkillCoroutines()
        {
            if (_playerSkillSlots == null) return;
            for (int i = 0; i < _playerSkillSlots.Length; i++)
            {
                CancelSlotCoroutine(i);
            }
        }
    }
}
