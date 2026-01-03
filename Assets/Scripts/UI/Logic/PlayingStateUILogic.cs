using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UI;
using Core.Events;
using RogueGame.Events;
using Character.Player;

namespace Game.UI
{
    /// <summary>
    /// PlayingStateUI 纯逻辑核心（可单元测试）
    /// </summary>
    public class PlayingStateUILogicCore
    {
        protected PlayingStateUIView _view;

        private PlayerRuntimeState _myPlayerState;
        private bool _skillEventsSubscribed;

        // 槽位映射：SlotIndex → (InstanceId, MaxEnergy)
        // 以及反向映射 InstanceId -> SlotIndex，保证能量变化时 O(1) 查找槽位
        private readonly Dictionary<int, (string InstanceId, int MaxEnergy)> _slotMapping = new();
        private readonly Dictionary<string, int> _instanceToSlot = new();

        public virtual void Bind(UIViewBase view)
        {
            _view = view as PlayingStateUIView;
            // 绑定 BagButton 点击事件
            _view?.BindBagButton(OnBagButtonClicked);
            // 订阅层过渡事件以更新层数显示
            EventBus.Subscribe<LayerTransitionEvent>(OnLayerTransition);
            // 订阅金币文本更新事件以更新金币显示
            EventBus.Subscribe<CoinTextUpdateEvent>(OnCoinTextUpdate);
        }
        private void OnCoinTextUpdate(CoinTextUpdateEvent evt)
        {
            CDTU.Utils.CDLogger.Log($"金币文本更新事件：{evt.NewText}");
            _view?.SetCoinText($"第{evt.NewText}层");
        }
        private void OnLayerTransition(LayerTransitionEvent evt)
        {
            CDTU.Utils.CDLogger.Log($"层过渡事件：从层 {evt.FromLayer} 到层 {evt.ToLayer}");
            _view?.SetLevelText($"第{evt.ToLayer}层");
        }

        //TODO-后面联机可能改
        public virtual void OnOpen(UIArgs args)
        {
            // 打开时初始化
            // 监听玩家注册事件以更新 UI
            PlayerManager.Instance.OnPlayerRegistered += PlayerRegistered;
            // 监听玩家注销以便及时清理订阅
            PlayerManager.Instance.OnPlayerUnregistered += PlayerUnregistered;
            // 若玩家已存在（可能在 UI 打开前注册），则直接调用注册回调以完成订阅
            PlayerRuntimeState existingState = null;
            foreach (var p in PlayerManager.Instance.GetAllPlayersData())
            {
                if (p.IsLocal) { existingState = p; break; }
            }
            if (existingState != null) PlayerRegistered(existingState);

        }

        public virtual void OnClose()
        {
            // 关闭时清理
            // 退订所有事件，避免内存泄漏或悬挂引用
            var pm = PlayerManager.Instance;
            if (pm != null)
            {
                pm.OnPlayerRegistered -= PlayerRegistered;
                pm.OnPlayerUnregistered -= PlayerUnregistered;
            }
            UnsubscribeFromPlayerHealthEvents();
            UnsubscribeFromSkillEvents();
            EventBus.Unsubscribe<LayerTransitionEvent>(OnLayerTransition);
            _myPlayerState = null;
            _view = null;
        }

        public virtual void OnCovered()
        {
            GameInput.Instance.PausePlayerInput();
        }

        public virtual void OnResume()
        {
            GameInput.Instance.ResumePlayerInput();
        }
        /// <summary>
        /// 订阅技能相关的事件
        /// </summary>
        private void SubscribeToSkillEvents()
        {
            // 检查玩家状态是否有效，无效则直接返回
            if (_myPlayerState == null) return;
            // 检查是否已经订阅过技能事件，避免重复订阅
            if (_skillEventsSubscribed) return;

            // 订阅能量变化事件，用于处理卡牌能量改变的情况
            EventBus.Subscribe<ActiveCardEnergyChangedEvent>(OnActiveCardEnergyChanged);

            // 订阅装备事件（建立映射）
            EventBus.Subscribe<SkillSlotEquippedEvent>(OnSkillSlotEquipped);

            // 订阅技能使用事件
            EventBus.Subscribe<PlayerSkillCastEvent>(OnPlayerSkillCast);

            _skillEventsSubscribed = true;
        }

        private void UnsubscribeFromSkillEvents()
        {
            if (!_skillEventsSubscribed) return;

            EventBus.Unsubscribe<ActiveCardEnergyChangedEvent>(OnActiveCardEnergyChanged);
            EventBus.Unsubscribe<SkillSlotEquippedEvent>(OnSkillSlotEquipped);
            EventBus.Unsubscribe<PlayerSkillCastEvent>(OnPlayerSkillCast);

            _skillEventsSubscribed = false;
            _slotMapping.Clear();
            _instanceToSlot.Clear();
        }


        private void PlayerRegistered(PlayerRuntimeState state)
        {
            // 只响应本地玩家的注册（UI 只关注本地玩家）
            if (state == null || !state.IsLocal) return;

            // 保存引用并订阅生命值与技能事件
            _myPlayerState = state;
            CDTU.Utils.CDLogger.Log("玩家注册：" + state.PlayerId);
            SubscribeToPlayerHealthEvents();
            SubscribeToSkillEvents();

            // 初始化时清空映射并刷新所有技能槽
            _slotMapping.Clear();
            _instanceToSlot.Clear();
            RefreshAllSkillSlots();
        }

        private void SubscribeToPlayerHealthEvents()
        {
            if (_myPlayerState == null) return;
            var stats = _myPlayerState.Controller?.Stats;
            if (stats == null) return;

            // 订阅 CharacterStats 的 OnHealthChanged（签名 Action<float current, float max>）
            stats.OnHealthChanged += OnPlayerHealthChanged;

            // 立即刷新一次 UI，确保打开时数据同步
            OnPlayerHealthChanged(stats.CurrentHP, stats.MaxHP.Value);
        }

        private void UnsubscribeFromPlayerHealthEvents()
        {
            if (_myPlayerState == null) return;
            var stats = _myPlayerState.Controller?.Stats;
            if (stats == null) return;

            stats.OnHealthChanged -= OnPlayerHealthChanged;
        }

        private void OnPlayerHealthChanged(float currentHealth, float maxHealth)
        {
            CDTU.Utils.CDLogger.Log($"玩家血量变化，当前血量：{currentHealth}");
            _view?.SetHealthNormalized(currentHealth / Math.Max(1f, maxHealth));
        }


        // 以下方法用于界面根据事件刷新技能槽显示

        /// <summary>
        /// 处理技能槽装备事件：建立映射并更新UI
        /// </summary>
        private void OnSkillSlotEquipped(SkillSlotEquippedEvent evt)
        {
            if (evt.PlayerId != _myPlayerState?.PlayerId) return;

            // 如果该 instance 已存在旧映射，先清理旧槽位映射
            if (_instanceToSlot.TryGetValue(evt.InstanceId, out var oldSlot) && oldSlot != evt.SlotIndex)
            {
                _slotMapping.Remove(oldSlot);
            }

            // 建立双向映射
            _slotMapping[evt.SlotIndex] = (evt.InstanceId, evt.MaxEnergy);
            _instanceToSlot[evt.InstanceId] = evt.SlotIndex;

            // 设置图标（通过实例查到定义）
            var cardState = GameRoot.Instance?.InventoryManager?.ActiveCardService?.GetCardByInstanceId(evt.InstanceId);
            var cardDef = cardState != null ? GameRoot.Instance?.CardDatabase?.Resolve(cardState.CardId) : null;
            _view?.SetSkillSlotIcon(evt.SlotIndex, cardDef?.GetSprite());

            // 设置初始能量
            if (cardState != null)
            {
                float normalized = Mathf.Clamp01((float)cardState.CurrentEnergy / Mathf.Max(1, evt.MaxEnergy));
                _view?.SetSkillSlotEnergy(evt.SlotIndex, normalized);
            }
        }

        /// <summary>
        /// 处理能量变化事件：O(1) 字典查找对应槽位
        /// </summary>
        private void OnActiveCardEnergyChanged(ActiveCardEnergyChangedEvent evt)
        {
            if (evt.PlayerId != _myPlayerState?.PlayerId) return;

            // 使用反向映射实现 O(1) 查找对应槽位
            if (!_instanceToSlot.TryGetValue(evt.InstanceId, out var slotIndex)) return;

            // 优先使用事件中的 MaxEnergy（发布时从配置获取，更准确）
            // 回退到映射中缓存的值作为防御
            int maxEnergy = evt.MaxEnergy > 0 ? evt.MaxEnergy
                : (_slotMapping.TryGetValue(slotIndex, out var info) ? info.MaxEnergy : 100);

            float normalized = Mathf.Clamp01((float)evt.NewEnergy / Mathf.Max(1, maxEnergy));
            _view?.SetSkillSlotEnergy(slotIndex, normalized);
        }

        /// <summary>
        /// 处理技能使用事件：触发技能视觉效果
        /// </summary>
        private void OnPlayerSkillCast(PlayerSkillCastEvent evt)
        {
            if (_myPlayerState == null || evt.PlayerId != _myPlayerState.PlayerId) return;
            _view?.SetSkillSlotUsed(evt.SlotIndex);
        }

        /// <summary>
        /// 刷新所有技能槽的显示（用于玩家注册时初始化）
        /// </summary>
        private void RefreshAllSkillSlots()
        {
            if (_myPlayerState == null || _view == null) return;

            var skillComponent = _myPlayerState.Controller?.GetComponent<PlayerSkillComponent>();
            if (skillComponent == null) return;

            for (int i = 0; i < skillComponent.SlotCount; i++)
            {
                var runtime = skillComponent.GetRuntime(i);
                if (runtime != null)
                {
                    // 获取当前状态
                    var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(runtime.CardId);
                    var cardState = GameRoot.Instance?.InventoryManager?.ActiveCardService?.GetCardByInstanceId(runtime.InstanceId);
                    // 建立双向映射
                    int maxEnergy = cardDef?.activeCardConfig?.maxEnergy ?? 100;
                    _slotMapping[i] = (runtime.InstanceId, maxEnergy);
                    _instanceToSlot[runtime.InstanceId] = i;

                    // 设置图标
                    _view?.SetSkillSlotIcon(i, cardDef?.GetSprite());

                    // 设置能量
                    if (cardState != null)
                    {
                        float normalized = Mathf.Clamp01((float)cardState.CurrentEnergy / Mathf.Max(1, maxEnergy));
                        _view?.SetSkillSlotEnergy(i, normalized);
                    }
                }
                else
                {
                    // 空槽位：清理双向映射与 UI
                    if (_slotMapping.TryGetValue(i, out var existing))
                    {
                        _instanceToSlot.Remove(existing.InstanceId);
                        _slotMapping.Remove(i);
                    }
                    _view?.SetSkillSlotIcon(i, null);
                    _view?.SetSkillSlotEnergy(i, 0f);
                }
            }
        }

        private void PlayerUnregistered(PlayerRuntimeState state)
        {
            if (_myPlayerState == null) return;
            if (state == null) return;
            if (state.PlayerId == _myPlayerState.PlayerId)
            {
                // 对应玩家被注销，清理订阅
                UnsubscribeFromPlayerHealthEvents();
                UnsubscribeFromSkillEvents();
                _myPlayerState = null;
            }
        }

        /// <summary>
        /// BagButton 点击事件处理
        /// </summary>
        private void OnBagButtonClicked()
        {
            //时间停止
            // await UIManager.Instance.Open<BagViewView>(layer: UILayer.Normal);
            //停止游戏输入写在UILogic的 OnCovered 里
            //开启游戏输入写在UILogic的 OnResume 里
            _ = OpenBagUIAsync();
        }

        private async Task<object> OpenBagUIAsync()
        {
            try
            {
                await UIManager.Instance.Open<BagViewView>(layer: UILayer.Normal);
            }
            catch (System.Exception ex)
            {
                CDTU.Utils.CDLogger.LogError($"打开背包失败: {ex.Message}");
            }
            return null;
        }
    }


    /// <summary>
    /// MonoBehaviour Wrapper：创建并持有 LogicCore，在运行时作为 IUILogic 注入到 View
    /// </summary>
    public class PlayingStateUILogic : MonoBehaviour, IUILogic
    {
        private PlayingStateUILogicCore _core = new PlayingStateUILogicCore();

        public void Bind(UIViewBase view)
        {
            _core.Bind(view);
        }

        public void OnOpen(UIArgs args)
        {
            _core.OnOpen(args);
        }

        public void OnClose()
        {
            _core.OnClose();
        }
        public void OnCovered()
        {
            _core.OnCovered();
        }

        public void OnResume()
        {
            _core.OnResume();
        }

    }
}
