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

        private EffectFactory effectFactory = new EffectFactory();
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

            EventBus.Subscribe<OnPlayerSkillEquippedEvent>(OnPlayerSlotCardChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnPlayerSkillEquippedEvent>(OnPlayerSlotCardChanged);
        }

        private void OnPlayerSlotCardChanged(OnPlayerSkillEquippedEvent @event)
        {
            // 获取拥有此组件的 PlayerController
            var pc = GetComponent<PlayerController>();
            if (pc == null) return;

            // 获取该 Controller 对应的运行时状态
            var pr = PlayerManager.Instance?.GetPlayerRuntimeStateByController(pc);
            // 如果找不到或者事件的 PlayerId 不匹配，则忽略
            if (pr == null || @event.PlayerId != pr.PlayerId) return;

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

        /// <summary>
        /// 检查技能是否可用（统一的可用性检查入口）
        /// </summary>
        public bool CanUseSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return false;
            var rt = _playerSkillSlots[slotIndex]?.Runtime;
            if (rt == null || rt.Skill == null) return false;

            // 检查 1: 房间内是否已使用
            if (rt.UsedInCurrentRoom) return false;

            // 检查 2: 冷却判断
            var baseCd = rt.Skill.cooldown;
            var stats = GetComponent<CharacterStats>();
            var reduction = stats != null ? stats.SkillCooldownReductionRate.Value : 0f;
            var effectiveCd = Mathf.Max(0f, baseCd * (1f - reduction));
            if (Time.time - rt.LastUseTime < effectiveCd) return false;

            // 检查 3: 充能检查（如果卡牌需要充能）
            var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(rt.CardId);
            if (cardDef != null && cardDef.activeCardConfig != null && cardDef.activeCardConfig.requiresCharge)
            {
                var inv = InventoryManager.Instance;
                if (inv == null || string.IsNullOrEmpty(rt.InstanceId)) return false;

                var state = inv.GetActiveCardState(rt.InstanceId);
                if (state == null || state.CurrentCharges < 1) return false;
            }

            // 检查 4: 状态效果（眩晕、沉默等）- 预留扩展
            var effectComp = GetComponent<StatusEffectComponent>();
            if (effectComp != null)
            {
                // TODO: 检查是否被沉默（无法使用技能）
                // if (effectComp.HasEffectOfType("Silence")) return false;
            }

            return true;
        }

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
                Debug.LogError("[PlayerSkillComponent] CharacterBase component not found");
                yield break;
            }

            // 创建技能上下文
            var ctx = new SkillTargetContext
            {
                Caster = caster,
                AimPoint = origin,
                AimDirection = (origin - transform.position).normalized,
                PowerMultiplier = 1.0f // 可扩展：从 Buff 或技能等级读取
            };

            // 播放 VFX（如果有）
            if (def.vfxPrefab != null)
            {
                //TODO-临时用transform.position，后续可改为技能定义中的挂点
                var vfx = Instantiate(def.vfxPrefab, transform.position, Quaternion.identity);
                var vfxsr = vfx.GetComponent<SpriteRenderer>();
            }

            // 获取目标
            var targets = new System.Collections.Generic.List<CharacterBase>();
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

                    var effectInstance = effectFactory.CreateInstance(effectDef, caster);
                    if (effectInstance == null)
                    {
                        continue;
                    }

                    statusComp.AddEffect(effectInstance);
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
                    instanceId = existing.InstanceId;
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

            // 广播初始能量/充能状态
            if (cardDef.activeCardConfig != null && cardDef.activeCardConfig.requiresCharge)
            {
                int max = Mathf.Max(1, cardDef.activeCardConfig.maxCharges);
                int current = max; // default

                if (inv != null && !string.IsNullOrEmpty(instanceId))
                {
                    var state = inv.GetActiveCardState(instanceId);
                    if (state != null) current = state.CurrentCharges;
                }

                float norm = (float)current / max;
                OnEnergyChanged?.Invoke(slotIndex, norm);
            }
            else
            {
                // 非充能技能，默认满能量（可用）
                OnEnergyChanged?.Invoke(slotIndex, 1f);
            }

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

        /// <summary>
        /// 打断技能（用于控制效果如眩晕、击飞等）
        /// </summary>
        /// <param name="slotIndex">技能槽位索引，-1 表示打断所有正在施放的技能</param>
        /// <param name="refundCharges">是否退还充能</param>
        public void InterruptSkill(int slotIndex = -1, bool refundCharges = false)
        {
            if (slotIndex < 0)
            {
                // 打断所有技能
                for (int i = 0; i < _playerSkillSlots.Length; i++)
                {
                    InterruptSingleSkill(i, refundCharges);
                }
            }
            else
            {
                InterruptSingleSkill(slotIndex, refundCharges);
            }
        }

        /// <summary>
        /// 打断单个技能槽位
        /// </summary>
        private void InterruptSingleSkill(int slotIndex, bool refundCharges)
        {
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return;
            var rt = _playerSkillSlots[slotIndex]?.Runtime;
            if (rt == null) return;

            // 取消协程
            if (rt.RunningCoroutine != null)
            {
                try
                {
                    StopCoroutine(rt.RunningCoroutine);
                }
                catch { }
                rt.RunningCoroutine = null;
            }

            // 如果需要退还充能
            if (refundCharges && !string.IsNullOrEmpty(rt.InstanceId))
            {
                var inv = InventoryManager.Instance;
                var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(rt.CardId);
                if (inv != null && cardDef != null && cardDef.activeCardConfig != null && cardDef.activeCardConfig.requiresCharge)
                {
                    var state = inv.GetActiveCardState(rt.InstanceId);
                    if (state != null)
                    {
                        int maxCharges = cardDef.activeCardConfig.maxCharges;
                        int AddCharges = cardDef.activeCardConfig.chargesPerKill;
                        inv.AddCharges(rt.InstanceId, AddCharges, maxCharges);
                    }
                }
            }

            // 重置使用标记（允许再次使用）
            rt.UsedInCurrentRoom = false;
        }
    }
}
