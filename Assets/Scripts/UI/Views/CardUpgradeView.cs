using System;
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
        [SerializeField] private TMP_Text description1;
        [SerializeField] private TMP_Text option1Name;
        [SerializeField] private Image option2ImageButton;
        [SerializeField] private Button option2Image;
        [SerializeField] private TMP_Text description2;
        [SerializeField] private TMP_Text option2Name;
        [SerializeField] private TMP_Text skillNameText;
        [SerializeField] private TMP_Text skillLevelText;
        public override bool Exclusive => false;
        public override bool CanBack => true;

        public override void OnCreate()
        {
            // 组件已在编辑器中手动绑定，无需运行时自动绑定
        }

        /// <summary>更新文本内容</summary>
        public void SetDescription1(string content)
        {
            if (description1 != null) description1.text = content;
        }
        public void SetOption1Name(string content)
        {
            if (option1Name != null) option1Name.text = content;
        }
        public void SetDescription2(string content)
        {
            if (description2 != null) description2.text = content;
        }
        public void SetOption2Name(string content)
        {
            if (option2Name != null) option2Name.text = content;
        }
        public void SetSkillNameText(string content)
        {
            if (skillNameText != null) skillNameText.text = content;
        }
        public void SetSkillLevelText(string content)
        {
            if (skillLevelText != null) skillLevelText.text = content;
        }

        /// <summary>绑定 Button 事件（使用基类 BindButton，自动在 OnClose 时清理）</summary>
        public void BindOption1ImageButton(System.Action onClickAction)
        {
            if (option1Image != null && onClickAction != null) BindButton(option1Image, onClickAction);
        }
        public void BindOption2ImageButton(System.Action onClickAction)
        {
            if (option2Image != null && onClickAction != null) BindButton(option2Image, onClickAction);
        }

        /// <summary>设置分支 A 的显示信息</summary>
        public void SetOption1(string name, string description, Sprite icon)
        {
            if (option1ImageButton != null && icon != null) option1ImageButton.sprite = icon;
            if (option1Name != null) option1Name.text = name ?? "分支 A";
            if (description1 != null) description1.text = description ?? string.Empty;
        }

        /// <summary>设置分支 A 的显示信息（带稀有度颜色）</summary>
        public void SetOption1(string name, string description, Sprite icon, Color rarityColor)
        {
            SetOption1(name, description, icon);
            if (option1Name != null) option1Name.color = rarityColor;
        }

        /// <summary>设置分支 B 的显示信息</summary>
        public void SetOption2(string name, string description, Sprite icon)
        {
            if (option2ImageButton != null && icon != null) option2ImageButton.sprite = icon;
            if (option2Name != null) option2Name.text = name ?? "分支 B";
            if (description2 != null) description2.text = description ?? string.Empty;
        }

        /// <summary>设置分支 B 的显示信息（带稀有度颜色）</summary>
        public void SetOption2(string name, string description, Sprite icon, Color rarityColor)
        {
            SetOption2(name, description, icon);
            if (option2Name != null) option2Name.color = rarityColor;
        }

        /// <summary>隐藏选项1</summary>
        public void HideOption1()
        {
            if (option1ImageButton != null) option1ImageButton.gameObject.SetActive(false);
            if (option1Name != null) option1Name.gameObject.SetActive(false);
            if (description1 != null) description1.gameObject.SetActive(false);
        }

        /// <summary>隐藏选项2</summary>
        public void HideOption2()
        {
            if (option2ImageButton != null) option2ImageButton.gameObject.SetActive(false);
            if (option2Name != null) option2Name.gameObject.SetActive(false);
            if (description2 != null) description2.gameObject.SetActive(false);
        }

        /// <summary>显示选项1</summary>
        public void ShowOption1()
        {
            if (option1ImageButton != null) option1ImageButton.gameObject.SetActive(true);
            if (option1Name != null) option1Name.gameObject.SetActive(true);
            if (description1 != null) description1.gameObject.SetActive(true);
        }

        /// <summary>显示选项2</summary>
        public void ShowOption2()
        {
            if (option2ImageButton != null) option2ImageButton.gameObject.SetActive(true);
            if (option2Name != null) option2Name.gameObject.SetActive(true);
            if (description2 != null) description2.gameObject.SetActive(true);
        }

        /// <summary>清空显示</summary>
        public void ClearDisplay()
        {
            SetSkillNameText(string.Empty);
            SetSkillLevelText(string.Empty);
            SetOption1(string.Empty, string.Empty, null);
            SetOption2(string.Empty, string.Empty, null);
            ShowOption1();
            ShowOption2();
        }
    }
}
