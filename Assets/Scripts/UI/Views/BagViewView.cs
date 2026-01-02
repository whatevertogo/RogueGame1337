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
        [SerializeField] private GameObject BagViewALL;
        [SerializeField] private GameObject PlayerStatViewALL;
        [SerializeField] private GameObject cardUIPrefab;
        [SerializeField] private Button ClearCardButton1;


        public override bool Exclusive => false;

        public override bool CanBack => true;

        public override void OnCreate()
        {
            // 组件已在编辑器中手动绑定，无需运行时自动绑定
        }

        /// <summary>更新文本内容</summary>
        public void SetMaxHP(string content)
        {
            if (maxHP) maxHP.text = content;
        }
        public void SetHPRegen(string content)
        {
            if (hPRegen) hPRegen.text = content;
        }
        public void SetMoveSpeed(string content)
        {
            if (moveSpeed) moveSpeed.text = content;
        }
        public void SetAcceleration(string content)
        {
            if (acceleration) acceleration.text = content;
        }
        public void SetAttackPower(string content)
        {
            if (attackPower) attackPower.text = content;
        }
        public void SetAttackSpeed(string content)
        {
            if (attackSpeed) attackSpeed.text = content;
        }
        public void SetAttackRange(string content)
        {
            if (attackRange) attackRange.text = content;
        }
        public void SetArmor(string content)
        {
            if (armor) armor.text = content;
        }
        public void SetDodge(string content)
        {
            if (dodge) dodge.text = content;
        }

        /// <summary>设置玩家头像图片</summary>
        public void SetPlayerImage(Sprite sprite)
        {
            if (playerImage != null) playerImage.sprite = sprite;
        }

        /// <summary>绑定 Button 事件</summary>
        public void BindPlayerStats1Button(System.Action onClickAction)
        {
            if (playerStats1) { playerStats1.onClick.RemoveAllListeners(); if (onClickAction != null) playerStats1.onClick.AddListener(() => onClickAction()); }
        }
        public void BindPlayerStats12Button(System.Action onClickAction)
        {
            if (playerStats12) { playerStats12.onClick.RemoveAllListeners(); if (onClickAction != null) playerStats12.onClick.AddListener(() => onClickAction()); }
        }

        public void BindClearCardButton1(System.Action onClickAction)
        {
            if (ClearCardButton1) { ClearCardButton1.onClick.RemoveAllListeners(); if (onClickAction != null) ClearCardButton1.onClick.AddListener(() => onClickAction()); }
        }

        public void SetBagViewALLActive(bool isActive)
        {
            if (BagViewALL)
            {
                BagViewALL.SetActive(isActive);
            }
        }

        public void SetPlayerStatViewActive(bool isActive)
        {
            if (PlayerStatViewALL)
            {
                PlayerStatViewALL.SetActive(isActive);
            }
        }

        public void AddCardView(string cardId, int Amount = 1)
        {
            if (!cardUIPrefab || !scrollView1 || scrollView1.content == null) return;

            // 以 prefab 的 GameObject 形式在 content 下实例化，并保持本地变换
            var go = Instantiate(cardUIPrefab, scrollView1.content, false);
            var newCard = go.GetComponent<CardUIPrefab>();
            if (!newCard) return;

            // 重置 RectTransform 以避免继承不期望的缩放/位置
            if (newCard.transform is RectTransform rt)
            {
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
            }
            newCard.transform.SetAsLastSibling();

            newCard.Init(cardId, Amount); // 初始化
        }

        /// <summary>
        /// 清空当前 content 下的卡牌视图（用于刷新前清理）
        /// </summary>
        public void ClearCardViews()
        {
            if (scrollView1 == null || scrollView1.content == null) return;
            var parent = scrollView1.content;
            // 注意：在运行时使用 Destroy
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (Application.isPlaying)
                    GameObject.Destroy(child.gameObject);
                else
                    GameObject.DestroyImmediate(child.gameObject);
            }
        }



        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
