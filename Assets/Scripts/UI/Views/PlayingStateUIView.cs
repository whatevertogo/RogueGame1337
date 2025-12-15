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
        [SerializeField] private TMP_Text textTMP;
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
            if (textTMP == null)
            {
                var t = transform.Find("NowLevel/Text (TMP)");
                if (t != null) textTMP = t.GetComponent<TMP_Text>();
                if (textTMP == null) Debug.LogWarning("[PlayingStateUIView] textTMP (TMP_Text) 未绑定");
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
        public void SetTextTMP(string content)
        {
            if (textTMP != null) textTMP.text = content;
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
