using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UI;
using DG.Tweening;

namespace Game.UI
{
    /// <summary>
    /// PlayingStateUI View 层 - UI 组件绑定
    /// </summary>
    public partial class PlayingStateUIView : UIViewBase
    {
        // UI Components
        [SerializeField] private Image bloorBarBackGround;
        [SerializeField] private Image bloorBar;
        [SerializeField] private TMP_Text nowLevel;
        [SerializeField] private Image skillSlote1Image;
        [SerializeField] private Image skillSlote2Image;
        [SerializeField] private Image skillSlot1Energy;
        [SerializeField] private Image skillSlot2Energy;
        [SerializeField] private Button BagButton;

        public override bool Exclusive => true;

        // 在运行时创建阶段进行自动绑定（UIManager 会调用 OnCreate）
        public override void OnCreate()
        {
            BindComponentsAtRuntime();
        }

        private void BindComponents() { } // 预留给自动绑定工具使用
        private void BindComponentsAtRuntime()
        {
            if (bloorBarBackGround == null)
            {
                var t = transform.Find("BloorBarPartical/BloorBarBackGround");
                if (t != null) bloorBarBackGround = t.GetComponent<Image>();
                if (bloorBarBackGround == null) Debug.LogWarning("[PlayingStateUIView] bloorBarBackGround (Image) 未绑定");
            }
            if (bloorBar == null)
            {
                var t = transform.Find("BloorBarPartical/BloorBar");
                if (t != null) bloorBar = t.GetComponent<Image>();
                if (bloorBar == null) Debug.LogWarning("[PlayingStateUIView] bloorBar (Image) 未绑定");
            }
            if (nowLevel == null)
            {
                var t = transform.Find("NowLevel/NowLevel");
                if (t != null) nowLevel = t.GetComponent<TMP_Text>();
                if (nowLevel == null) Debug.LogWarning("[PlayingStateUIView] nowLevel (TMP_Text) 未绑定");
            }
            if (skillSlote1Image == null)
            {
                var t = transform.Find("SkillSlotes/SkillSlote1/SkillSlote1Image");
                if (t != null) skillSlote1Image = t.GetComponent<Image>();
                if (skillSlote1Image == null) Debug.LogWarning("[PlayingStateUIView] skillSlote1Image (Image) 未绑定");
            }
            if (skillSlote2Image == null)
            {
                var t = transform.Find("SkillSlotes/SkillSlote2/SkillSlote2Image");
                if (t != null) skillSlote2Image = t.GetComponent<Image>();
                if (skillSlote2Image == null) Debug.LogWarning("[PlayingStateUIView] skillSlote2Image (Image) 未绑定");
            }
            if (skillSlot1Energy == null)
            {
                var t = transform.Find("SkillSlotes/SkillSlote1/SkillSlot1Energy");
                if (t != null) skillSlot1Energy = t.GetComponent<Image>();
            }
            if (skillSlot2Energy == null)
            {
                var t = transform.Find("SkillSlotes/SkillSlote2/SkillSlot2Energy");
                if (t != null) skillSlot2Energy = t.GetComponent<Image>();
            }
            
            // 绑定 BagButton 点击事件
            if (BagButton != null)
            {
                BagButton.onClick.RemoveAllListeners();
            }
        }
        
        /// <summary>
        /// 绑定 BagButton 点击事件
        /// </summary>
        /// <param name="onClickAction">点击时触发的回调函数</param>
        public void BindBagButton(Action onClickAction)
        {
            if (BagButton != null)
            {
                BagButton.onClick.RemoveAllListeners();
                BagButton.onClick.AddListener(() => onClickAction?.Invoke());
            }
        }

        /// <summary>更新文本内容</summary>
        public void SetNowLevel(string content)
        {
            if (nowLevel != null) nowLevel.text = content;
        }

        /// <summary>
        /// 以归一化值（0-1）更新生命值显示（血条）。
        /// View 层负责具体的 UI 控制，Logic 层只传递数据。
        /// </summary>
        public void SetHealthNormalized(float normalized)
        {
            if (bloorBar != null)
            {
                var f = bloorBar.fillAmount;
                // DOTween.To(() => f, x => f = x, Mathf.Clamp01(normalized), 0.1f)
                //     .OnUpdate(() => bloorBar.fillAmount = f)
                //     .SetEase(Ease.OutCubic);

                //TODO-或许有更好duration的值,现在默认0.1f
                bloorBar.DOFillAmount(Mathf.Clamp01(normalized), 0.08f).SetEase(Ease.OutCirc).SetUpdate(true);
            }
        }

        /// <summary>
        /// 设置当前关卡/层级显示文本
        /// </summary>
        public void SetLevelText(string content)
        {
            if (nowLevel != null) nowLevel.text = content;
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
        }

        public void SetSkillSlotEnergy(int slotIndex, float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            if (slotIndex == 0 && skillSlot1Energy != null)
            {
                //TODO-或许有更好duration的值,现在默认0.05f
                skillSlot1Energy.DOFillAmount(Mathf.Clamp01(normalized), 0.05f).SetEase(Ease.OutCirc).SetUpdate(true);
            }
            else if (slotIndex == 1 && skillSlot2Energy != null)
            {
                //TODO-或许有更好duration的值,现在默认0.05f
                skillSlot2Energy.DOFillAmount(Mathf.Clamp01(normalized), 0.05f).SetEase(Ease.OutCirc).SetUpdate(true);
            }
        }

        public void SetSkillSlotUsed(int slotIndex)
        {
            if(slotIndex == 0 && skillSlot1Energy != null)
            {
                //TODO-或许有更好duration的值,现在默认0.04f
                skillSlot1Energy.DOFillAmount(0f, 0.04f).SetEase(Ease.OutCirc).SetUpdate(true);
            }
            else if (slotIndex == 1 && skillSlot2Energy != null)
            {
                //TODO-或许有更好duration的值,现在默认0.04f
                skillSlot2Energy.DOFillAmount(0f, 0.04f).SetEase(Ease.OutCirc).SetUpdate(true);
            }

        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
