using CDTU.Utils;
using Core.Events;
using RogueGame.Events;

public class ComboManager : Singleton<ComboManager>
{
    private int _currentCombo = 0;
    private float _remainingTime = 0f;
    private ComboTier _currentTier;

    private void OnEntityKilled(EntityKilledEvent evt)
    {
        // 发布连击变化事件
        EventBus.Publish(new ComboChangedEvent(_currentCombo, _currentTier));
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
        EventBus.Publish(new ComboChangedEvent(0));
    }
}
