using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// DeadUIView View 层 - UI 组件绑定
    /// </summary>
    public partial class DeadUIView : UIViewBase
    {
        // UI Components
        [SerializeField] private Image panel;
        [SerializeField] private TMP_Text deadMessage;
        [SerializeField] private Image retryButton;
        [SerializeField] private Button retry;
        [SerializeField] private TMP_Text textTMPTMP_Text;
        public override bool Exclusive => false;
        public override bool CanBack => true;

        public override void OnCreate()
        {
            // 组件已在编辑器中手动绑定，无需运行时自动绑定
        }

        /// <summary>更新文本内容</summary>
        public void SetDeadMessage(string content)
        {
            if (deadMessage != null) deadMessage.text = content;
        }
        public void SetTextTMPTMP_Text(string content)
        {
            if (textTMPTMP_Text != null) textTMPTMP_Text.text = content;
        }

        /// <summary>绑定 Retry 按钮点击事件（使用基类 BindButton，自动在 OnClose 时清理）</summary>
        public void BindRetryButton(System.Action onClickAction)
        {
            if (retry != null && onClickAction != null) BindButton(retry, onClickAction);
        }




        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
