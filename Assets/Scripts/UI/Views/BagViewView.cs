using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// BagView View 层 - UI 组件绑定
    /// </summary>
    public partial class BagViewView : UIViewBase
    {
        // UI Components
        [SerializeField] private Image bg;
        [SerializeField] private Image bg1;
        [SerializeField] private Image playerStats;
        [SerializeField] private Button playerStats1;
        [SerializeField] private Image icon1;
        [SerializeField] private Image select;
        [SerializeField] private Image playerStats11;
        [SerializeField] private Button playerStats12;
        [SerializeField] private Image icon11;
        [SerializeField] private Image select1;
        [SerializeField] private Image scrollView;
        [SerializeField] private ScrollRect scrollView1;
        [SerializeField] private Image viewport;
        [SerializeField] private Image cardUIPrefab;
        [SerializeField] private Image cardBackGround;
        [SerializeField] private Image cardImageRealByID;
        [SerializeField] private Image scrollbarHorizontal;
        [SerializeField] private Image handle;
        [SerializeField] private Image scrollbarVertical;
        [SerializeField] private Image handle1;
        [SerializeField] private Image descriptionviewCardView;
        [SerializeField] private Image cardSlot1;
        [SerializeField] private Image cardSlot2;
        [SerializeField] private Image playerImage;
        [SerializeField] private Image playerStatsBackGround;
        [SerializeField] private TMP_Text maxHP;
        [SerializeField] private TMP_Text hPRegen;
        [SerializeField] private TMP_Text moveSpeed;
        [SerializeField] private TMP_Text acceleration;
        [SerializeField] private TMP_Text attackPower;
        [SerializeField] private TMP_Text attackSpeed;
        [SerializeField] private TMP_Text attackRange;
        [SerializeField] private TMP_Text armor;
        [SerializeField] private TMP_Text dodge;
        [SerializeField] private TMP_Text skillCooldownReductionRate;

        public override bool Exclusive => false;

        public override bool CanBack => true;

        public override void OnCreate()
        {
            // 组件已在编辑器中手动绑定，无需运行时自动绑定
        }

        /// <summary>更新文本内容</summary>
        public void SetMaxHP(string content)
        {
            if (maxHP != null) maxHP.text = content;
        }
        public void SetHPRegen(string content)
        {
            if (hPRegen != null) hPRegen.text = content;
        }
        public void SetMoveSpeed(string content)
        {
            if (moveSpeed != null) moveSpeed.text = content;
        }
        public void SetAcceleration(string content)
        {
            if (acceleration != null) acceleration.text = content;
        }
        public void SetAttackPower(string content)
        {
            if (attackPower != null) attackPower.text = content;
        }
        public void SetAttackSpeed(string content)
        {
            if (attackSpeed != null) attackSpeed.text = content;
        }
        public void SetAttackRange(string content)
        {
            if (attackRange != null) attackRange.text = content;
        }
        public void SetArmor(string content)
        {
            if (armor != null) armor.text = content;
        }
        public void SetDodge(string content)
        {
            if (dodge != null) dodge.text = content;
        }
        public void SetSkillCooldownReductionRate(string content)
        {
            if (skillCooldownReductionRate != null) skillCooldownReductionRate.text = content;
        }

        /// <summary>设置玩家头像图片</summary>
        public void SetPlayerImage(Sprite sprite)
        {
            if (playerImage != null) playerImage.sprite = sprite;
        }

        /// <summary>绑定 Button 事件</summary>
        public void BindPlayerStats1Button(System.Action onClickAction)
        {
            if (playerStats1 != null) { playerStats1.onClick.RemoveAllListeners(); if (onClickAction != null) playerStats1.onClick.AddListener(() => onClickAction()); }
        }
        public void BindPlayerStats12Button(System.Action onClickAction)
        {
            if (playerStats12 != null) { playerStats12.onClick.RemoveAllListeners(); if (onClickAction != null) playerStats12.onClick.AddListener(() => onClickAction()); }
        }

        /// <summary>设置 maxHP 的值</summary>

        /// <summary>设置 hPRegen 的值</summary>

        /// <summary>设置 moveSpeed 的值</summary>

        /// <summary>设置 acceleration 的值</summary>

        /// <summary>设置 attackPower 的值</summary>

        /// <summary>设置 attackSpeed 的值</summary>

        /// <summary>设置 attackRange 的值</summary>

        /// <summary>设置 armor 的值</summary>

        /// <summary>设置 dodge 的值</summary>

        /// <summary>设置 skillCooldownReductionRate 的值</summary>

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
