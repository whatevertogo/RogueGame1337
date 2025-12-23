using UnityEngine;
using UI;
using System.Threading.Tasks;
using System.Collections;

namespace Game.UI
{
    /// <summary>
    /// UI启动器 - 负责UI系统的初始化和启动
    /// </summary>
    public class UIBootstrap : MonoBehaviour
    {
        [Header("UI启动配置")]
        [SerializeField] private bool autoStartUI = true;
        [SerializeField] private bool showPlayingStateUI = true;

        private void Start()
        {
            InitializeUISystem();
        }

        /// <summary>
        /// 初始化UI系统
        /// </summary>
        private void InitializeUISystem()
        {
            Debug.Log("[UIBootstrap] 开始初始化UI系统");

            // 确保UIManager存在
            if (UIManager.Instance == null)
            {
                Debug.LogError("[UIBootstrap] UIManager单例未找到，请确保场景中有UIManager对象");
                return;
            }

            // 等待一帧确保所有组件初始化完成
            StartCoroutine(DelayedStartUI());
        }

        /// <summary>
        /// 延迟启动UI界面
        /// </summary>
        private IEnumerator DelayedStartUI()
        {
            yield return null; // 等待一帧

            if (autoStartUI && showPlayingStateUI)
            {
                _ = ShowPlayingStateUI();
            }

            Debug.Log("[UIBootstrap] UI系统初始化完成");
        }

        /// <summary>
        /// 显示游戏中的UI界面
        /// </summary>
        public async Task ShowPlayingStateUI()
        {
            if (UIManager.Instance == null)
            {
                Debug.LogError("[UIBootstrap] UIManager实例不存在");
                return;
            }

            try
            {
                // 检查是否已经打开
                if (UIManager.Instance.IsOpen<PlayingStateUIView>())
                {
                    Debug.Log("[UIBootstrap] PlayingStateUI已经打开");
                    return;
                }

                // 异步打开
                var uiView = await UIManager.Instance.Open<PlayingStateUIView>(layer: UILayer.Normal);
                if (uiView != null)
                {
                    Debug.Log("[UIBootstrap] PlayingStateUI打开成功");

                    // 设置初始层级显示
                    uiView.SetLevelText("第1层");
                }
                else
                {
                    Debug.LogError("[UIBootstrap] PlayingStateUI打开失败");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UIBootstrap] 打开PlayingStateUI时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 隐藏游戏中的UI界面
        /// </summary>
        public void HidePlayingStateUI()
        {
            if (UIManager.Instance == null) return;

            if (UIManager.Instance.IsOpen<PlayingStateUIView>())
            {
                UIManager.Instance.Close<PlayingStateUIView>();
                Debug.Log("[UIBootstrap] PlayingStateUI已隐藏");
            }
        }
    }
}