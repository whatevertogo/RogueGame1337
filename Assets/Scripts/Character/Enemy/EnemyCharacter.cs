
using System;
using Character;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyAnimator))]
public class EnemyCharacter : CharacterBase
{

    [Header("Enemy特殊配置")]
    [SerializeField, ]
    private EnemyConfigSO enemyConfig;

    [SerializeField]
    private EnemyConfigStats enemyConfigStats;

    private EnemyAnimator EnemyAnim => GetComponent<EnemyAnimator>();
    private bool _deathHandled = false;


    protected override void Awake()
    {
        base.Awake();

        InitializeEnemyConfig();

        // 订阅攻击事件（播放动画等）
        if (Combat != null)
        {
            Combat.OnAttack += OnAttackPerformed;
        }

        // 如果没有配置，警告并退出初始化
        if (enemyConfig == null)
        {
            CDTU.Utils.CDLogger.LogWarning($"[EnemyCharacter] {gameObject.name} 没有配置 EnemyConfigSO!");
        }

        // 设置阵营为敌人（如果尚未设置）
        SetTeam(Character.TeamType.Enemy);

        // 自动设置 Layer 为 Enemy 层
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1 && gameObject.layer != enemyLayer)
        {
            gameObject.layer = enemyLayer;
            // 同时递归设置所有子对象的 Layer
            SetLayerRecursive(transform, enemyLayer);
        }

        // 订阅死亡事件，处理掉落与击杀归属
        var health = GetComponent<HealthComponent>();
        if (health != null)
        {
            health.OnDeathWithAttacker += HandleDeathWithAttacker;
        }


        var rb = GetComponent<Rigidbody2D>();
        var col = GetComponent<Collider2D>();
        CDTU.Utils.CDLogger.Log($"[EnemyCharacter] Awake: {gameObject.name}, tag={gameObject.tag}, layer={LayerMask.LayerToName(gameObject.layer)}, Rigidbody2D={(rb != null ? "Yes" : "No")}, Collider2D={(col != null ? "Yes" : "No")}");


    }

    private void InitializeEnemyConfig()
    {
        if (enemyConfig != null)
        {
            enemyConfigStats = new EnemyConfigStats(enemyConfig);
        }
        else
        {
            enemyConfigStats = new EnemyConfigStats();
        }

        // 如果配置标记为精英，则应用基础 stat 修饰符
        if (enemyConfigStats.isEliteDefault)
        {
            ApplyEliteModifiers();
        }
    }

    /// <summary>
    /// 在实例化后覆盖或注入配置（供 EnemyFactory 调用）
    /// </summary>
    public void OverrideConfig(EnemyConfigSO configSO, int floor = 1)
    {
        if (configSO == null) return;
        try
        {
            enemyConfig = configSO;
            enemyConfigStats = new EnemyConfigStats(configSO);
            if (enemyConfigStats.isEliteDefault)
            {
                ApplyEliteModifiers();
            }
        }
        catch (System.Exception ex)
        {
            CDTU.Utils.CDLogger.LogWarning($"[EnemyCharacter] OverrideConfig failed: {ex.Message}");
        }
    }

    private void ApplyEliteModifiers()
    {
        // 精英示例: 生命 x3, 攻击力 x1.5
        try
        {
            // 使用 StatModifier: PercentMult uses (1 + mod.Value)
            var hpMult = new StatModifier(2.0f, StatModType.PercentMult, this);
            Stats.MaxHP.AddModifier(hpMult);

            var atkMult = new StatModifier(0.5f, StatModType.PercentMult, this);
            Stats.AttackPower.AddModifier(atkMult);
        }
        catch (Exception ex)
        {
            CDTU.Utils.CDLogger.LogWarning($"[EnemyCharacter] ApplyEliteModifiers failed: {ex.Message}");
        }
    }

    private void OnAttackPerformed()
    {
        // 播放攻击动画
        try
        {
            EnemyAnim?.PlayAttackAnim();
            CDTU.Utils.CDLogger.Log($"[EnemyCharacter] {gameObject.name} 播放攻击动画");
        }
        catch (Exception ex)
        {
            CDTU.Utils.CDLogger.LogWarning($"[EnemyCharacter] PlayAttack failed: {ex.Message}");
        }
    }

    private void HandleDeathWithAttacker(GameObject attacker)
    {
        // 掉落金币/卡牌（使用 LootDropper 生成拾取物）
        int amount = UnityEngine.Random.Range(enemyConfigStats.CoinMin, enemyConfigStats.CoinMax + 1);
        if (amount > 0)
        {
            LootDropper.Instance?.DropCoins(transform.position, amount);
        }

        //TODO-后面设置卡牌权重来选择掉落
        // 扔卡牌：被动 & 主动分开依据概率
        if (enemyConfigStats.PassiveCardIds != null && enemyConfigStats.PassiveCardIds.Length > 0 && UnityEngine.Random.value <= enemyConfigStats.PassiveDropChance)
        {
            var index = UnityEngine.Random.Range(0, enemyConfigStats.PassiveCardIds.Length);
            var passiveCardId = enemyConfigStats.PassiveCardIds[index];
            LootDropper.Instance?.DropCard(transform.position, passiveCardId);
        }
        if (enemyConfigStats.ActiveCardIds != null && enemyConfigStats.ActiveCardIds.Length > 0 && UnityEngine.Random.value <= enemyConfigStats.ActiveDropChance)
        {
            var index = UnityEngine.Random.Range(0, enemyConfigStats.ActiveCardIds.Length);
            var activeCardId = enemyConfigStats.ActiveCardIds[index];
            LootDropper.Instance?.DropCard(transform.position, activeCardId);
        }

        // 分配能量给击杀者（如果击杀者为玩家）
        PlayerController killer = null;
        if (attacker != null)
        {
            killer = attacker.GetComponentInParent<PlayerController>();
            if (killer == null)
            {
                // if attacker is projectile, try to read owner chain
                var proj = attacker.GetComponent<ProjectileBase>();
                if (proj != null && proj.Owner != null)
                {
                    killer = proj.Owner.GetComponentInParent<PlayerController>();
                }
            }
        }

        if (killer != null)
        {
            var pm = PlayerManager.Instance;
        }
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        if (_deathHandled) return;
        _deathHandled = true;

        // 播放死亡动画
        try
        {
            EnemyAnim?.PlayDeathAnim();
        }
        catch (Exception ex)
        {
            CDTU.Utils.CDLogger.LogWarning($"[EnemyCharacter] PlayDeath failed: {ex.Message}");
        }

        Movement?.SetCanMove(false);

        if (Combat != null)
        {
            Combat.enabled = false;
        }

        // 禁用碰撞器以免阻挡其他单位
        foreach (var col in GetComponents<Collider2D>())
        {
            try { col.enabled = false; } catch (Exception) { }
        }

        // 停用刚体物理模拟
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            try { rb.simulated = false; } catch (Exception) { }
        }

        // 自动销毁或归还到对象池（延迟）
        StartCoroutine(DestroyDelayed(3f));
    }

    private IEnumerator DestroyDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        // 触发移除流程：如果使用对象池，这里改为归还
        Destroy(gameObject);
    }

    /// <summary>
    /// 递归设置游戏对象及其所有子对象的 Layer
    /// </summary>
    private void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
        {
            SetLayerRecursive(t.GetChild(i), layer);
        }
    }

    protected override void OnDestroy()
    {
        // 退订事件
        if (Combat != null)
        {
            Combat.OnAttack -= OnAttackPerformed;
        }
        var health = GetComponent<HealthComponent>();
        if (health != null)
        {
            health.OnDeathWithAttacker -= HandleDeathWithAttacker;
        }
        base.OnDestroy();
    }
}
