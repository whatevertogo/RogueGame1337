using System;
using UnityEngine;
using UI;
using Character.Core;

namespace Game.UI
{
    /// <summary>
    /// PlayingStateUI 纯逻辑核心（可单元测试）
    /// </summary>
    public class PlayingStateUILogicCore
    {
        protected PlayingStateUIView _view;

        private PlayerRuntimeState _myPlayerState;
        public virtual void Bind(UIViewBase view)
        {
            _view = view as PlayingStateUIView;
        }

        //todo-后面联机可能改
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

        private void SubscribeToSkillEvents()
        {
            var pm = PlayerManager.GetExistingInstance();
            if (pm == null) return;
            pm.OnPlayerSkillEnergyChanged += this.OnPlayerSkillEnergyChanged;
            pm.OnPlayerSkillUsed += this.OnPlayerSkillUsed;
        }

        private void UnsubscribeFromSkillEvents()
        {
            var pm = PlayerManager.GetExistingInstance();
            if (pm == null) return;
            pm.OnPlayerSkillEnergyChanged -= this.OnPlayerSkillEnergyChanged;
            pm.OnPlayerSkillUsed -= this.OnPlayerSkillUsed;
        }

        private void PlayerRegistered(PlayerRuntimeState state)
        {
            // 保存引用并订阅生命值相关事件
            _myPlayerState = state;
            Debug.Log("玩家注册：" + state.PlayerId);
            SubscribeToPlayerHealthEvents();
            SubscribeToSkillEvents();
            // TODO-刷新技能槽初始显示
            if (_myPlayerState != null && _view != null)
            {
                
            }
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

        private void OnPlayerHealthChanged(float currentHealth,float maxHealth)
        {
            Debug.Log($"玩家血量变化，当前血量：{currentHealth}");
            _view?.SetHealthNormalized(currentHealth / Math.Max(1f, maxHealth));
        }

        public virtual void OnClose()
        {
            // 关闭时清理
            // 退订所有事件，避免内存泄漏或悬挂引用
            var pm = PlayerManager.GetExistingInstance();
            if (pm != null)
            {
                pm.OnPlayerRegistered -= PlayerRegistered;
                pm.OnPlayerUnregistered -= PlayerUnregistered;
            }
            UnsubscribeFromPlayerHealthEvents();
            UnsubscribeFromSkillEvents();
            _myPlayerState = null;
            _view = null;
        }

        private void OnPlayerSkillEnergyChanged(string playerId, int slotIndex, float energy)
        {
            if (_myPlayerState == null) return;
            if (playerId != _myPlayerState.PlayerId) return;
            // energy is normalized 0..1 from PlayerSkillComponent
            _view?.SetSkillSlotEnergy(slotIndex, energy);
        }

        private void OnPlayerSkillUsed(string playerId, int slotIndex)
        {
            if (_myPlayerState == null) return;
            if (playerId != _myPlayerState.PlayerId) return;
            _view?.SetSkillSlotUsed(slotIndex);
        }

        private void PlayerUnregistered(PlayerRuntimeState state)
        {
            if (_myPlayerState == null) return;
            if (state == null) return;
            if (state.PlayerId == _myPlayerState.PlayerId)
            {
                // 对应玩家被注销，清理订阅
                UnsubscribeFromPlayerHealthEvents();
                _myPlayerState = null;
            }
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

        private void TryUseSkillSlot(int slotIndex)
        {
            
        }
    }
}
