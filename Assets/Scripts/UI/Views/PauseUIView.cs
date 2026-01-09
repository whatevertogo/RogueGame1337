using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// PauseUIView View 层 - UI 组件绑定
    /// </summary>
    public partial class PauseUIView : UIViewBase
    {
        // UI Components
        [SerializeField] private Image settingButton;
        [SerializeField] private Button setting;
        [SerializeField] private Image saveButton;
        [SerializeField] private Button save;
        [SerializeField] private Image quitButton;
        [SerializeField] private Button quit;
        [SerializeField] private Image saveSlotButton1;
        [SerializeField] private Button saveSlotButton1Button;
        [SerializeField] private TMP_Text saveTime1;
        [SerializeField] private Image saveSlotButton2;
        [SerializeField] private Button saveSlotButton2Button;
        [SerializeField] private TMP_Text saveTime2;
        [SerializeField] private Image saveSlotButton3;
        [SerializeField] private Button saveSlotButton3Button;
        [SerializeField] private TMP_Text saveTime3;
        [SerializeField] private Image saveSlotButton;
        [SerializeField] private Button saveSlot;
        [SerializeField] private TMP_Text saveTime4;
        [SerializeField] private Image quitButtonImage;
        [SerializeField] private Button quitButton1;

        // Inside Controller
        public BookUIInsideController bookInsideUIController;
        public override bool Exclusive => false;
        public override bool CanBack => true;

        public override void OnCreate()
        {
            // 组件已在编辑器中手动绑定，无需运行时自动绑定
        }

        public void SetSaveTime1(string content)
        {
            if (saveTime1 != null) saveTime1.text = content;
        }
        public void SetSaveTime2(string content)
        {
            if (saveTime2 != null) saveTime2.text = content;
        }
        public void SetSaveTime3(string content)
        {
            if (saveTime3 != null) saveTime3.text = content;
        }
        public void SetSaveTime4(string content)
        {
            if (saveTime4 != null) saveTime4.text = content;
        }

        /// <summary>绑定 Button 事件（使用基类 BindButton，自动在 OnClose 时清理）</summary>
        public void BindSettingButton(System.Action onClickAction)
        {
            if (setting != null && onClickAction != null) BindButton(setting, onClickAction);
        }
        public void BindSaveButton(System.Action onClickAction)
        {
            if (save != null && onClickAction != null) BindButton(save, onClickAction);
        }
        public void BindQuitButton(System.Action onClickAction)
        {
            if (quit != null && onClickAction != null) BindButton(quit, onClickAction);
        }
        public void BindSaveSlotButton1Button(System.Action onClickAction)
        {
            if (saveSlotButton1Button != null && onClickAction != null) BindButton(saveSlotButton1Button, onClickAction);
        }
        public void BindSaveSlotButton2Button(System.Action onClickAction)
        {
            if (saveSlotButton2Button != null && onClickAction != null) BindButton(saveSlotButton2Button, onClickAction);
        }
        public void BindSaveSlotButton3Button(System.Action onClickAction)
        {
            if (saveSlotButton3Button != null && onClickAction != null) BindButton(saveSlotButton3Button, onClickAction);
        }
        public void BindSaveSlotButton(System.Action onClickAction)
        {
            if (saveSlot != null && onClickAction != null) BindButton(saveSlot, onClickAction);
        }
        public void BindQuitButton1Button(System.Action onClickAction)
        {
            if (quitButton1 != null && onClickAction != null) BindButton(quitButton1, onClickAction);
        }





        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
