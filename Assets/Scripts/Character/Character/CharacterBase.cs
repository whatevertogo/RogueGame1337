using UnityEngine;
using Character.Core;
using Character.Components;
using Character.Interfaces;

namespace Character
{
    /// <summary>
    /// 角色基类 - 统一管理各组件引用
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(MovementComponent))]
    [RequireComponent(typeof(CombatComponent))]
    [RequireComponent(typeof(StatusEffectComponent))]
    public class CharacterBase : MonoBehaviour, ITeamMember
    {
        [Header("阵营")]
        [SerializeField] private TeamType team = TeamType.Neutral;
        public TeamType Team => team;

        // 组件缓存
        public CharacterStats Stats { get; private set; }
        public HealthComponent Health { get; private set; }
        public MovementComponent Movement { get; private set; }
        public CombatComponent Combat { get; private set; }
        public StatusEffectComponent StatusEffects { get; private set; }
        public SpriteRenderer SpriteRenderer { get; private set; }

        protected virtual void Awake()
        {
            CacheComponents();
            SetupEventListeners();
        }

        protected virtual void OnDestroy()
        {
            RemoveEventListeners();
        }

        private void CacheComponents()
        {
            Stats = GetComponent<CharacterStats>();
            Health = GetComponent<HealthComponent>();
            Movement = GetComponent<MovementComponent>();
            Combat = GetComponent<CombatComponent>();
            StatusEffects = GetComponent<StatusEffectComponent>();
            // 尝试缓存精灵渲染器：优先当前对象，其次查找子对象
            SpriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
            if (SpriteRenderer == null)
            {
                CDTU.Utils.Logger.LogWarning($"[CharacterBase] SpriteRenderer 未找到：{gameObject.name}");
            }
        }

        protected virtual void SetupEventListeners()
        {
            if (Health != null)
            {
                Health.OnDeath += OnDeath;
            }
        }

        protected virtual void RemoveEventListeners()
        {
            if (Health != null)
            {
                Health.OnDeath -= OnDeath;
            }
        }

        protected virtual void OnDeath()
        {
            // 子类重写处理死亡逻辑
            Debug.Log($"{gameObject.name} died!");
        }

        /// <summary>
        /// 设置阵营
        /// </summary>
        public void SetTeam(TeamType newTeam)
        {
            team = newTeam;
        }
    }
}