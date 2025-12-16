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
            // 若玩家已存在（可能在 UI 打开前注册），则直接调用注册回调以完成订阅
            PlayerRuntimeState existingState = null;
            foreach (var p in PlayerManager.Instance.GetAllPlayersData())
            {
                if (p.IsLocal) { existingState = p; break; }
            }
            if (existingState != null) PlayerRegistered(existingState);

        }

        private void PlayerRegistered(PlayerRuntimeState state)
        {
            // 保存引用并订阅生命值相关事件
            _myPlayerState = state;
            Debug.Log("玩家注册：" + state.PlayerId);
            SubscribeToPlayerHealthEvents();
        }

        private void SubscribeToPlayerHealthEvents()
        {
            if (_myPlayerState == null) return;
            var health = _myPlayerState.Controller?.Health;
            if (health == null) return;

            // 规范命名的事件处理函数，签名需匹配 HealthComponent 定义
            health.OnDamaged += OnPlayerHealthDamaged;
            health.OnHealed += OnPlayerHealthHealed;
        }

        private void UnsubscribeFromPlayerHealthEvents()
        {
            if (_myPlayerState == null) return;
            var health = _myPlayerState.Controller?.Health;
            if (health == null) return;

            health.OnDamaged -= OnPlayerHealthDamaged;
            health.OnHealed -= OnPlayerHealthHealed;
        }

        private void OnPlayerHealthDamaged(DamageResult result)
        {
            float current = _myPlayerState.Controller.Health.CurrentHP;
            Debug.Log($"玩家受伤，实际伤害：{result.FinalDamage}，当前血量：{current}");
            // 使用 View 提供的封装方法更新血条（避免直接访问私有字段）
            _view?.SetHealthNormalized(current / Math.Max(1f, _myPlayerState.Controller.Health.MaxHP));
        }

        private void OnPlayerHealthHealed(float amount)
        {
            float current = _myPlayerState.Controller.Health.CurrentHP;
            Debug.Log($"玩家治疗，恢复量：{amount}，当前血量：{current}");
            _view?.SetHealthNormalized(current / Math.Max(1f, _myPlayerState.Controller.Health.MaxHP));
        }

        public virtual void OnClose()
        {
            // 关闭时清理
            // 退订所有事件，避免内存泄漏或悬挂引用
            PlayerManager.Instance.OnPlayerRegistered -= PlayerRegistered;
            UnsubscribeFromPlayerHealthEvents();
            _myPlayerState = null;
            _view = null;
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
    }
}
