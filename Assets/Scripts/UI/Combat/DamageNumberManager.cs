using System.Collections.Generic;
using UnityEngine;
using CDTU.Utils;
using Core.Events;
using RogueGame.Events;

namespace UI.Combat
{
    /// <summary>
    /// 伤害数字管理器 - 负责生成和管理伤害数字显示
    /// </summary>
    public class DamageNumberManager : Singleton<DamageNumberManager>
    {
        [Header("预制体设置")]
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private Transform canvasTransform;
        [SerializeField] private int poolSize = 20;
        
        [Header("显示设置")]
        [SerializeField] private bool enableDamageNumbers = true;
        [SerializeField] private bool worldSpaceMode = true;
        [SerializeField] private float worldSpaceOffset = 1f;
        
        private Queue<DamageNumber> _damageNumberPool = new Queue<DamageNumber>();
        private List<DamageNumber> _activeDamageNumbers = new List<DamageNumber>();
        
        protected override void Awake()
        {
            base.Awake();
            InitializePool();
            
            // 订阅伤害事件
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<HealEvent>(OnHeal);
        }
        
        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<HealEvent>(OnHeal);
            base.OnDestroy();
        }
        
        /// <summary>
        /// 初始化对象池
        /// </summary>
        private void InitializePool()
        {
            if (damageNumberPrefab == null || canvasTransform == null)
            {
                CDTU.Utils.CDLogger.LogError("[DamageNumberManager] 预制体或Canvas未设置！");
                return;
            }
            
            for (int i = 0; i < poolSize; i++)
            {
                var obj = Instantiate(damageNumberPrefab, canvasTransform);
                var damageNumber = obj.GetComponent<DamageNumber>();
                
                if (damageNumber != null)
                {
                    damageNumber.Reset();
                    obj.SetActive(false);
                    _damageNumberPool.Enqueue(damageNumber);
                }
                else
                {
                    CDTU.Utils.CDLogger.LogError("[DamageNumberManager] 预制体缺少DamageNumber组件！");
                    Destroy(obj);
                }
            }
            
            CDTU.Utils.CDLogger.Log($"[DamageNumberManager] 对象池初始化完成: {poolSize}个对象");
        }
        
        /// <summary>
        /// 从池中获取伤害数字对象
        /// </summary>
        private DamageNumber GetFromPool()
        {
            if (_damageNumberPool.Count > 0)
            {
                var damageNumber = _damageNumberPool.Dequeue();
                damageNumber.gameObject.SetActive(true);
                _activeDamageNumbers.Add(damageNumber);
                return damageNumber;
            }
            
            // 池为空时创建新对象
            if (damageNumberPrefab != null && canvasTransform != null)
            {
                var obj = Instantiate(damageNumberPrefab, canvasTransform);
                var damageNumber = obj.GetComponent<DamageNumber>();
                
                if (damageNumber != null)
                {
                    _activeDamageNumbers.Add(damageNumber);
                    return damageNumber;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 显示伤害数字
        /// </summary>
        /// <param name="position">世界空间位置</param>
        /// <param name="damage">伤害值</param>
        /// <param name="isCrit">是否暴击</param>
        /// <param name="isHeal">是否治疗</param>
        /// <param name="isMiss">是否未命中</param>
        public void ShowDamageNumber(Vector3 position, int damage, bool isCrit = false, bool isHeal = false, bool isMiss = false)
        {
            if (!enableDamageNumbers) return;
            
            var damageNumber = GetFromPool();
            if (damageNumber == null) return;
            
            // 设置位置
            if (worldSpaceMode)
            {
                // 世界空间模式 - 转换为屏幕空间
                var screenPos = Camera.main?.WorldToScreenPoint(position + Vector3.up * worldSpaceOffset);
                if (screenPos.HasValue)
                {
                    damageNumber.transform.position = screenPos.Value;
                }
            }
            else
            {
                // 直接使用世界坐标
                damageNumber.transform.position = position;
            }
            
            // 显示伤害
            damageNumber.ShowDamage(damage, isCrit, isHeal, isMiss);
        }
        
        /// <summary>
        /// 归还对象到池中
        /// </summary>
        public void ReturnToPool(DamageNumber damageNumber)
        {
            if (damageNumber == null || _activeDamageNumbers.Contains(damageNumber) == false) return;
            
            _activeDamageNumbers.Remove(damageNumber);
            damageNumber.Reset();
            damageNumber.gameObject.SetActive(false);
            _damageNumberPool.Enqueue(damageNumber);
        }
        
        /// <summary>
        /// 清理所有活动的伤害数字
        /// </summary>
        public void ClearAllDamageNumbers()
        {
            foreach (var damageNumber in _activeDamageNumbers.ToArray())
            {
                ReturnToPool(damageNumber);
            }
        }
        
        // 事件处理
        private void OnDamageDealt(DamageDealtEvent evt)
        {
            if (evt == null) return;
            
            ShowDamageNumber(
                evt.Position, 
                evt.Damage, 
                evt.IsCrit, 
                false, // 不是治疗
                evt.IsMiss
            );
        }
        
        private void OnHeal(HealEvent evt)
        {
            if (evt == null) return;
            
            ShowDamageNumber(
                evt.Position,
                evt.Amount,
                false, // 不是暴击
                true, // 是治疗
                false // 不是未命中
            );
        }
        
        /// <summary>
        /// 调试功能：显示测试伤害
        /// </summary>
        [ContextMenu("显示测试伤害")]
        private void ShowTestDamage()
        {
            Vector3 testPos = transform.position + Vector3.forward * 5f;
            ShowDamageNumber(testPos, 100, true, false, false);
        }
        
        /// <summary>
        /// 调试功能：显示测试治疗
        /// </summary>
        [ContextMenu("显示测试治疗")]
        private void ShowTestHeal()
        {
            Vector3 testPos = transform.position + Vector3.forward * 5f;
            ShowDamageNumber(testPos, 50, false, true, false);
        }
    }
    
    /// <summary>
    /// 伤害事件数据
    /// </summary>
    [System.Serializable]
    public class DamageDealtEvent
    {
        public Vector3 Position;
        public int Damage;
        public bool IsCrit;
        public bool IsMiss;
        public GameObject Target;
        public GameObject Source;
    }
    
    /// <summary>
    /// 治疗事件数据
    /// </summary>
    [System.Serializable]
    public class HealEvent
    {
        public Vector3 Position;
        public int Amount;
        public GameObject Target;
    }
}