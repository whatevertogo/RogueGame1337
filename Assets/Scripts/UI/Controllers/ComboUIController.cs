using System;
using Core.Events;
using RogueGame.Events;
using UI;

public static class ComboUIController
{
    private static bool _initialized;
    private static UIManager _uiManager;

    public static void Initialize(UIManager uiManager)
    {
        if (_initialized)
            return;
        _initialized = true;
        _uiManager = uiManager;

        // 在此处添加任何 Combo UI 相关的初始化代码
        EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);
    }

    private static void OnComboChanged(ComboChangedEvent @event) { }

    // Combo UI 相关代码
}
