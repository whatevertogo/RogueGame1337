using System;
using UnityEngine;
using UI;
using System.Threading.Tasks;
using Core.Events;
using RogueGame.Events;

namespace Game.UI
{
    /// <summary>
    /// PlayingStateUI 纯逻辑核心（可单元测试）
    /// </summary>
    public class PlayingStateUILogicCore
    {
        protected PlayingStateUIView _view;

        private PlayerRuntimeState _myPlayerState;
        private bool _skillEventsSubscribed = false;

        public virtual void Bind(UIViewBase view)
        {
            _view = view as PlayingStateUIView;
            // 绑定 BagButton 点击事件
            _view?.BindBagButton(OnBagButtonClicked);
            EventBus.Subscribe<LayerTransitionEvent>(OnLayerTransition);
        }

        private void OnLayerTransition(LayerTransitionEvent evt)
        {
            CDTU.Utils.CDLogger.Log($"层过渡事件：从层 {evt.FromLayer} 到层 {evt.ToLayer}");
            _view.SetLevelText($"第{evt.ToLayer}层");
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
        private void SubscribeToSkillEvents()
        {
            var pm = PlayerManager.Instance;
            if (pm == null) return;
        }

        private void UnsubscribeFromSkillEvents()
        {
            var pm = PlayerManager.Instance;
            if (pm == null) return;
            if (!_skillEventsSubscribed) return;
            _skillEventsSubscribed = false;
        }

        private void OnPlayerSkillEquipped(string playerId, int slotIndex, string cardId)
        {
            if (_myPlayerState == null || playerId != _myPlayerState.PlayerId) return;
            var def = GameRoot.Instance?.CardDatabase?.Resolve(cardId);
            if (def != null)
            {
                _view?.SetSkillSlotIcon(slotIndex, def.GetSprite());
            }
        }

        private void OnPlayerSkillUnequipped(string playerId, int slotIndex)
        {
            if (_myPlayerState == null || playerId != _myPlayerState.PlayerId) return;
            _view?.SetSkillSlotIcon(slotIndex, null);
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

        private void OnPlayerHealthChanged(float currentHealth, float maxHealth)
        {
            CDTU.Utils.CDLogger.Log($"玩家血量变化，当前血量：{currentHealth}");
            _view?.SetHealthNormalized(currentHealth / Math.Max(1f, maxHealth));
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

        private void TryUseSkillSlot(int slotIndex)
        {

        }
    }
}
