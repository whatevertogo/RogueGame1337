using CDTU.Utils;
using Core.Events;
using RogueGame.Events;

public class ComboManager : Singleton<ComboManager>
{
    private int _currentCombo = 0;
    private float _remainingTime = 0f;

    private void OnEntityKilled(EntityKilledEvent evt)
    {
        // 发布连击变化事件
        // EventBus.Publish(
        //     new ComboChangedEvent(
        //         _currentCombo,
        //         (int)_currentTier,
        //         GetTierName(_currentTier),
        //         GetEnergyMultiplier(),
        //         false
        //     )
        // );
    }

    private void Update()
    {
        // 超时重置
        if (_remainingTime <= 0)
        {
            ResetCombo();
        }
    }

    private void ResetCombo()
    {
        // EventBus.Publish(new ComboExpiredEvent(finalCombo, (int)finalTier));
        EventBus.Publish(new ComboChangedEvent(0, 0, "无", 1.0f, false));
    }
}
