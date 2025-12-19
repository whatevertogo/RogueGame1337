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

        public override void OnCreate()
        {
            // 组件已在编辑器中手动绑定，无需运行时自动绑定
        }

        /// <summary>绑定 Button 事件</summary>
        public void BindPlayerStats1Button(System.Action onClickAction)
        {
            if (playerStats1 != null) { playerStats1.onClick.RemoveAllListeners(); playerStats1.onClick.AddListener(() => onClickAction?.Invoke()); }
        }
        public void BindPlayerStats12Button(System.Action onClickAction)
        {
            if (playerStats12 != null) { playerStats12.onClick.RemoveAllListeners(); playerStats12.onClick.AddListener(() => onClickAction?.Invoke()); }
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
