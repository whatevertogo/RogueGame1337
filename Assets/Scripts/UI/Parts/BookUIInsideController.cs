using System;
using UnityEngine;

/// <summary>
/// 书本 UI 状态枚举
/// </summary>
public enum BookUIState
{
    /// <summary>默认视图（主菜单）</summary>
    DefaultView,

    /// <summary>设置视图</summary>
    SettingView,

    /// <summary>存档视图</summary>
    SaveView,

    /// <summary>退出确认视图</summary>
    QuitView,
}

/// <summary>
/// BookUI 状态控制器
/// 职责：管理 PauseUI 中不同子视图的切换和显示
/// </summary>
public class BookUIInsideController : MonoBehaviour
{
    [Header("子视图引用")]
    [SerializeField]
    [Tooltip("设置视图 GameObject")]
    private GameObject settingView;

    [SerializeField]
    [Tooltip("存档视图 GameObject")]
    private GameObject saveView;

    [SerializeField]
    [Tooltip("退出确认视图 GameObject")]
    private GameObject quitView;

    [Header("过渡配置")]
    [SerializeField]
    [Tooltip("是否启用视图切换动画")]
    private bool enableTransitionAnimation = true;

    [SerializeField]
    [Tooltip("视图切换动画时长（秒）")]
    private float transitionDuration = 0.3f;

    // 当前状态
    private BookUIState _currentState = BookUIState.DefaultView;

    /// <summary>
    /// 获取当前状态
    /// </summary>
    public BookUIState GetCurrentState() => _currentState;

    /// <summary>
    /// 切换到指定状态
    /// </summary>
    /// <param name="newState">新状态</param>
    public void SwitchState(BookUIState newState)
    {
        if (_currentState == newState)
            return;

        BookUIState previousState = _currentState;
        _currentState = newState;

        // 更新视图显示
        UpdateView();
        Debug.Log($"[BookUI] 状态切换: {previousState} → {newState}");
    }

    /// <summary>
    /// 更新视图显示（根据当前状态显示对应子视图）
    /// </summary>
    public void UpdateView()
    {
        // 隐藏所有子视图
        HideAllViews();

        // 根据当前状态显示对应视图
        switch (_currentState)
        {
            case BookUIState.DefaultView:
                // 默认视图：显示主菜单按钮（隐藏所有子视图）
                ShowDefaultView();
                break;

            case BookUIState.SettingView:
                // 设置视图：显示设置面板
                ShowSettingView();
                break;

            case BookUIState.SaveView:
                // 存档视图：显示存档槽位
                ShowSaveView();
                break;

            case BookUIState.QuitView:
                // 退出确认视图：显示退出确认面板
                ShowQuitView();
                break;
        }
    }

    /// <summary>
    /// 隐藏所有子视图
    /// </summary>
    private void HideAllViews()
    {
        if (settingView != null)
            settingView.SetActive(false);

        if (saveView != null)
            saveView.SetActive(false);

        if (quitView != null)
            quitView.SetActive(false);
    }

    /// <summary>
    /// 显示默认视图（主菜单）
    /// </summary>
    private void ShowDefaultView()
    {
        // 默认视图不需要显示任何子视图
        // 主按钮（Setting/Save/Quit）始终可见
        Debug.Log("[BookUI] 显示默认视图（主菜单）");
    }

    /// <summary>
    /// 显示设置视图
    /// </summary>
    private void ShowSettingView()
    {
        if (settingView != null)
        {
            settingView.SetActive(true);
            Debug.Log("[BookUI] 显示设置视图");
        }
        else
        {
            Debug.LogWarning("[BookUI] SettingView 引用未设置，请检查 Inspector 配置");
        }
    }

    /// <summary>
    /// 显示存档视图
    /// </summary>
    private void ShowSaveView()
    {
        if (saveView != null)
        {
            saveView.SetActive(true);
            Debug.Log("[BookUI] 显示存档视图");
        }
        else
        {
            Debug.LogWarning("[BookUI] SaveView 引用未设置，请检查 Inspector 配置");
        }
    }

    /// <summary>
    /// 显示退出确认视图
    /// </summary>
    private void ShowQuitView()
    {
        if (quitView != null)
        {
            quitView.SetActive(true);
            Debug.Log("[BookUI] 显示退出确认视图");
        }
        else
        {
            Debug.LogWarning("[BookUI] QuitView 引用未设置，请检查 Inspector 配置");
        }
    }

    /// <summary>
    /// 返回到默认视图（主菜单）
    /// </summary>
    public void ReturnToDefaultView()
    {
        SwitchState(BookUIState.DefaultView);
    }

    /// <summary>
    /// 初始化（在 Start 或 Awake 中调用）
    /// </summary>
    private void Start()
    {
        // 初始化时显示默认视图
        UpdateView();
    }

    /// <summary>
    /// 重置到默认状态
    /// </summary>
    public void Reset()
    {
        _currentState = BookUIState.DefaultView;
        UpdateView();
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器辅助：验证引用是否正确配置
    /// </summary>
    private void OnValidate()
    {
        if (settingView == null)
            Debug.LogWarning($"[BookUI] {nameof(settingView)} 未设置");

        if (saveView == null)
            Debug.LogWarning($"[BookUI] {nameof(saveView)} 未设置");

        if (quitView == null)
            Debug.LogWarning($"[BookUI] {nameof(quitView)} 未设置");
    }
#endif
}
