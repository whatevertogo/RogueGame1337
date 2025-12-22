using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// ShopUIView View 层 - UI 组件绑定
    /// </summary>
    public partial class ShopUIView : UIViewBase
    {
        // UI Components
        [SerializeField] private Image image;
        [SerializeField] private Image button;
        [SerializeField] private Button button1;
        [SerializeField] private TMP_Text textTMP;
        [SerializeField] private TMP_Text ShopType;
        [SerializeField] private TMP_Text coinText;
        public override bool Exclusive => false;
        public override bool CanBack => true;

        public override void OnCreate()
        {
            // 组件已在编辑器中手动绑定，无需运行时自动绑定
        }

        /// <summary>更新文本内容</summary>
        public void SetTextTMP(string content)
        {
            if (textTMP != null) textTMP.text = content;
        }
        public void SetShopType(string content)
        {
            if (ShopType != null) ShopType.text = content;
        }
        public void SetCoinText(string content)
        {
            if (coinText != null) coinText.text = content;
        }

        /// <summary>绑定 Button 事件</summary>
        public void BindButton1Button(System.Action onClickAction)
        {
            if (button1 != null) { button1.onClick.RemoveAllListeners(); if (onClickAction != null) { button1.onClick.AddListener(() => onClickAction()); } }
        }



        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
