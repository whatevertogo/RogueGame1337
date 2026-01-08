using Character.Components;
using UnityEngine;

public class EnemyView : MonoBehaviour
{
    [SerializeField] private CharacterStats characterStats;
    [SerializeField] private Transform headAnchor;

    private WorldHpBarBinder binder;

    void OnEnable()
    {
        if (characterStats == null) characterStats = GetComponent<CharacterStats>();
        if (WorldHpBarSystem.Instance == null) return;
        binder = WorldHpBarSystem.Instance.Bind(
            characterStats,
            headAnchor
        );
    }

    void OnDisable()
    {
        if (WorldHpBarSystem.Instance == null) return;
        if (binder != null) WorldHpBarSystem.Instance.Unbind(binder);
    }
}