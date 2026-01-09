using System;
using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// PlayingStateUI View 层 - UI 组件绑定
    /// </summary>
    public partial class PlayingStateUIView : UIViewBase
    {
        // UI Components
        [SerializeField]
        private Image bloorBar;

        [SerializeField]
        private TMP_Text nowLevel;

        [SerializeField]
        private Image skillSlote1Image;

        [SerializeField]
        private Image skillSlote2Image;

        [SerializeField]
        private Image skillSlote3Image;

        [SerializeField]
        private Image skillSlot1Energy;

        [SerializeField]
        private Image skillSlot2Energy;

        [SerializeField]
        private Image skillSlot3Energy;

        [SerializeField]
        private Button BagButton;

        [SerializeField]
        private Button pauseUIViewButton;

        [SerializeField]
        private TMP_Text coinText;

        public override bool Exclusive => false;
        public override bool CanBack => false;

        // 在运行时创建阶段进行自动绑定（UIManager 会调用 OnCreate）
        public override void OnCreate() { }

        private void BindComponents() { } // 预留给自动绑定工具使用

        /// <summary>
        /// 绑定 BagButton 点击事件（使用基类 BindButton，自动在 OnClose 时清理）
        /// </summary>
        /// <param name="onClickAction">点击时触发的回调函数</param>
        public void BindBagButton(Action onClickAction)
        {
            if (BagButton != null && onClickAction != null)
                BindButton(BagButton, onClickAction);
        }

        public void BindPauseButton(Action onClickAction)
        {
            if (pauseUIViewButton != null && onClickAction != null)
                BindButton(pauseUIViewButton, onClickAction);
        }

        /// <summary>
        /// 以归一化值（0-1）更新生命值显示（血条）。
        /// View 层负责具体的 UI 控制，Logic 层只传递数据。
        /// </summary>
        public void SetHealthNormalized(float normalized)
        {
            if (bloorBar != null)
            {
                //TODO-或许有更好duration的值,现在默认0.1f
                bloorBar
                    .DOFillAmount(Mathf.Clamp01(normalized), 0.08f)
                    .SetEase(Ease.OutCirc)
                    .SetUpdate(true);
            }
        }

        /// <summary>
        /// 设置当前关卡/层级显示文本
        /// </summary>
        public void SetLevelText(string content)
        {
            if (nowLevel != null)
                nowLevel.text = content;
        }

        /// <summary>
        /// 更新技能槽图标（slotIndex 从 0 开始）
        /// </summary>
        public void SetSkillSlotIcon(int slotIndex, Sprite icon)
        {
            if (slotIndex == 0 && skillSlote1Image != null)
            {
                skillSlote1Image.sprite = icon;
                skillSlote1Image.enabled = icon != null;
            }
            else if (slotIndex == 1 && skillSlote2Image != null)
            {
                skillSlote2Image.sprite = icon;
                skillSlote2Image.enabled = icon != null;
            }
            else if (slotIndex == 2 && skillSlote3Image != null)
            {
                skillSlote3Image.sprite = icon;
                skillSlote3Image.enabled = icon != null;
            }
        }

        public void SetSkillSlotEnergy(int slotIndex, float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            if (slotIndex == 0 && skillSlot1Energy != null)
            {
                //TODO-或许有更好duration的值,现在默认0.05f
                skillSlot1Energy
                    .DOFillAmount(Mathf.Clamp01(normalized), 0.05f)
                    .SetEase(Ease.OutCirc)
                    .SetUpdate(true);
            }
            else if (slotIndex == 1 && skillSlot2Energy != null)
            {
                //TODO-或许有更好duration的值,现在默认0.05f
                skillSlot2Energy
                    .DOFillAmount(Mathf.Clamp01(normalized), 0.05f)
                    .SetEase(Ease.OutCirc)
                    .SetUpdate(true);
            }
            else if (slotIndex == 2 && skillSlot3Energy != null)
            {
                //TODO-或许有更好duration的值,现在默认0.05f
                skillSlot3Energy
                    .DOFillAmount(Mathf.Clamp01(normalized), 0.05f)
                    .SetEase(Ease.OutCirc)
                    .SetUpdate(true);
            }
        }

        public void SetSkillSlotUsed(int slotIndex)
        {
            if (slotIndex == 0 && skillSlot1Energy != null)
            {
                //TODO-或许有更好duration的值,现在默认0.04f
                skillSlot1Energy.DOFillAmount(0f, 0.04f).SetEase(Ease.OutCirc).SetUpdate(true);
            }
            else if (slotIndex == 1 && skillSlot2Energy != null)
            {
                //TODO-或许有更好duration的值,现在默认0.04f
                skillSlot2Energy.DOFillAmount(0f, 0.04f).SetEase(Ease.OutCirc).SetUpdate(true);
            }
            else if (slotIndex == 2 && skillSlot3Energy != null)
            {
                //TODO-或许有更好duration的值,现在默认0.04f
                skillSlot3Energy.DOFillAmount(0f, 0.04f).SetEase(Ease.OutCirc).SetUpdate(true);
            }
        }

        public void SetCoinText(string content)
        {
            if (coinText != null)
                coinText.text = content;
        }
    }
}
