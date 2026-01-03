using System;
using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// CardUpgradeView View 层 - UI 组件绑定
    /// </summary>
    public partial class CardUpgradeView : UIViewBase
    {
        // UI Components
        [SerializeField] private Image option1ImageButton;
        [SerializeField] private Button option1Image;
        [SerializeField] private Image option2ImageButton;
        [SerializeField] private Button option2Image;
        [SerializeField] private TMP_Text skillNameText;
        [SerializeField] private TMP_Text skillLevelText;

        // 分支信息文本组件（如果需要在编辑器中绑定）
        [SerializeField] private TMP_Text option1NameText;
        [SerializeField] private TMP_Text option1DescText;
        [SerializeField] private TMP_Text option2NameText;
        [SerializeField] private TMP_Text option2DescText;

        public override bool Exclusive => false;
        public override bool CanBack => true;

        public override void OnCreate()
        {
            // 组件已在编辑器中手动绑定，无需运行时自动绑定
        }

        /// <summary>更新技能名称</summary>
        public void SetSkillNameText(string content)
        {
            if (skillNameText != null) skillNameText.text = content;
        }

        /// <summary>更新技能等级</summary>
        public void SetSkillLevelText(string content)
        {
            if (skillLevelText != null) skillLevelText.text = content;
        }

        /// <summary>绑定 Button 事件</summary>
        public void BindOption1ImageButton(System.Action onClickAction)
        {
            if (option1Image != null && onClickAction != null) { option1Image.onClick.AddListener(() => onClickAction()); }
        }
        public void BindOption2ImageButton(System.Action onClickAction)
        {
            if (option2Image != null && onClickAction != null) { option2Image.onClick.AddListener(() => onClickAction()); }
        }

        /// <summary>设置分支 A 的显示信息</summary>
        public void SetOption1(string name, string description, Sprite icon)
        {
            if (option1ImageButton != null && icon != null) option1ImageButton.sprite = icon;
            if (option1NameText != null) option1NameText.text = name ?? "分支 A";
            if (option1DescText != null) option1DescText.text = description ?? string.Empty;
        }

        /// <summary>设置分支 B 的显示信息</summary>
        public void SetOption2(string name, string description, Sprite icon)
        {
            if (option2ImageButton != null && icon != null) option2ImageButton.sprite = icon;
            if (option2NameText != null) option2NameText.text = name ?? "分支 B";
            if (option2DescText != null) option2DescText.text = description ?? string.Empty;
        }

        /// <summary>清空显示</summary>
        public void ClearDisplay()
        {
            SetSkillNameText(string.Empty);
            SetSkillLevelText(string.Empty);
            SetOption1(string.Empty, string.Empty, null);
            SetOption2(string.Empty, string.Empty, null);
        }

        /// <summary>关闭 UI</summary>
        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
