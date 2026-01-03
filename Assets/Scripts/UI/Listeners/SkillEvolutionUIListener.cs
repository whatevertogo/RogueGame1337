using Core.Events;
using RogueGame.Events;
using UnityEngine;
using Game.UI;

namespace UI.Listeners
{
    /// <summary>
    /// 技能进化 UI 全局监听器
    /// 在游戏启动时订阅进化事件，确保事件不会因 UI 未加载而丢失
    /// </summary>
    public class SkillEvolutionUIListener : MonoBehaviour
    {
        private static SkillEvolutionUIListener _instance;
        public static SkillEvolutionUIListener Instance => _instance;

        /// <summary>
        /// 待处理的进化请求（UI 打开后会读取并清空）
        /// </summary>
        public static SkillEvolutionRequestedEvent PendingEvent { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<SkillEvolutionRequestedEvent>(OnEvolutionRequested);
            EventBus.Subscribe<SkillEvolvedEvent>(OnEvolutionCompleted);
            Debug.Log("[SkillEvolutionUIListener] 已启动全局进化事件监听");
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SkillEvolutionRequestedEvent>(OnEvolutionRequested);
            EventBus.Unsubscribe<SkillEvolvedEvent>(OnEvolutionCompleted);
            Debug.Log("[SkillEvolutionUIListener] 已停止全局进化事件监听");
        }

        /// <summary>
        /// 处理技能进化请求事件
        /// </summary>
        private void OnEvolutionRequested(SkillEvolutionRequestedEvent evt)
        {
            Debug.Log($"[SkillEvolutionUIListener] 收到进化请求: {evt.CardId} Lv{evt.CurrentLevel}→Lv{evt.NextLevel}");
            
            // 保存事件供 UI 使用
            PendingEvent = evt;

            // 打开进化选择 UI
            if (UIManager.Instance != null)
            {
                _ = UIManager.Instance.Open<CardUpgradeView>();
            }
            else
            {
                Debug.LogError("[SkillEvolutionUIListener] UIManager.Instance 为 null，无法打开进化 UI");
            }
        }

        /// <summary>
        /// 处理技能进化完成事件
        /// </summary>
        private void OnEvolutionCompleted(SkillEvolvedEvent evt)
        {
            Debug.Log($"[SkillEvolutionUIListener] 进化完成: {evt.CardId} → Lv{evt.NewLevel}");
            
            // 清空待处理事件
            if (PendingEvent != null && evt.InstanceId == PendingEvent.InstanceId)
            {
                PendingEvent = null;
            }

            // 关闭进化 UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.Close<CardUpgradeView>();
            }
        }

        /// <summary>
        /// 消费待处理事件（UI 读取后调用）
        /// </summary>
        public static SkillEvolutionRequestedEvent ConsumePendingEvent()
        {
            var evt = PendingEvent;
            PendingEvent = null;
            return evt;
        }
    }
}
