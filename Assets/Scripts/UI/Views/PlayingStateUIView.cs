using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI;
using System;

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
        [SerializeField] private Image skillSlote1BackGround;
        [SerializeField] private Image skillSlote1Image;
        [SerializeField] private Image skillSlote2BackGround;
        [SerializeField] private Image skillSlote2Image;

        // 在运行时创建阶段进行自动绑定（UIManager 会调用 OnCreate）
        public override void OnCreate()
        {
            BindComponentsAtRuntime();
        }

        private void BindComponents() { } // Editor-binding 占位（实际优先运行时绑定）
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
            if (skillSlote1BackGround == null)
            {
                var t = transform.Find("SkillSlotes/SkillSlote1/SkillSlote1BackGround");
                if (t != null) skillSlote1BackGround = t.GetComponent<Image>();
                if (skillSlote1BackGround == null) Debug.LogWarning("[PlayingStateUIView] skillSlote1BackGround (Image) 未绑定");
            }
            if (skillSlote1Image == null)
            {
                var t = transform.Find("SkillSlotes/SkillSlote1/SkillSlote1Image");
                if (t != null) skillSlote1Image = t.GetComponent<Image>();
                if (skillSlote1Image == null) Debug.LogWarning("[PlayingStateUIView] skillSlote1Image (Image) 未绑定");
            }
            if (skillSlote2BackGround == null)
            {
                var t = transform.Find("SkillSlotes/SkillSlote2/SkillSlote2BackGround");
                if (t != null) skillSlote2BackGround = t.GetComponent<Image>();
                if (skillSlote2BackGround == null) Debug.LogWarning("[PlayingStateUIView] skillSlote2BackGround (Image) 未绑定");
            }
            if (skillSlote2Image == null)
            {
                var t = transform.Find("SkillSlotes/SkillSlote2/SkillSlote2Image");
                if (t != null) skillSlote2Image = t.GetComponent<Image>();
                if (skillSlote2Image == null) Debug.LogWarning("[PlayingStateUIView] skillSlote2Image (Image) 未绑定");
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
                bloorBar.fillAmount = Mathf.Clamp01(normalized);
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

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
