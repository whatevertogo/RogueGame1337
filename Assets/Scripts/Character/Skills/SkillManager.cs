using System;
using UnityEngine;
using Character.Components;
using Character.Skills;

namespace Character.Skills
{
    /// <summary>
    /// 技能槽状态管理
    /// </summary>
    [Serializable]
    public class SkillSlot
    {
        public SkillData equippedSkill;
        public int currentEnergy;
        public bool hasBeenUsedInRoom;
        
        public bool CanUseSkill()
        {
            return equippedSkill != null && 
                   currentEnergy >= equippedSkill.maxEnergy && 
                   !hasBeenUsedInRoom && 
                   equippedSkill.canBeUsedInRoom;
        }
        
        public void ResetRoomUsage()
        {
            hasBeenUsedInRoom = false;
        }
        
        public void UseSkill()
        {
            if (CanUseSkill())
            {
                currentEnergy = 0;
                hasBeenUsedInRoom = true;
            }
        }
        
        public void AddEnergy(int amount)
        {
            if (equippedSkill != null)
            {
                currentEnergy = Mathf.Min(currentEnergy + amount, equippedSkill.maxEnergy);
            }
        }
    }
    
    /// <summary>
    /// 技能管理器 - 处理Q/E槽逻辑
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        [Header("技能槽配置")]
        public SkillSlot qSlot = new SkillSlot();
        public SkillSlot eSlot = new SkillSlot();
        
        [Header("充能配置")]
        public int normalEnemyEnergy = 10;
        public int eliteEnemyEnergy = 30;
        public int bossEnemyEnergy = 50;
        
        // 事件
        public event Action<int, int> OnEnergyChanged; // 槽位索引, 新能量值
        public event Action<int> OnSkillUsed; // 槽位索引
        public event Action<int> OnSkillEquipped; // 槽位索引
        
        private CharacterBase characterBase;
        
        private void Awake()
        {
            characterBase = GetComponent<CharacterBase>();
            if (characterBase != null)
            {
                characterBase.Health.OnDeath += OnCharacterDeath;
            }
        }
        
        private void OnDestroy()
        {
            if (characterBase != null && characterBase.Health != null)
            {
                characterBase.Health.OnDeath -= OnCharacterDeath;
            }
        }
        
        /// <summary>
        /// 装备技能到指定槽位
        /// </summary>
        public bool EquipSkill(SkillData skill, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 1) return false;
            
            var slot = slotIndex == 0 ? qSlot : eSlot;
            slot.equippedSkill = skill;
            slot.currentEnergy = 0;
            slot.hasBeenUsedInRoom = false;
            
            OnSkillEquipped?.Invoke(slotIndex);
            return true;
        }
        
        /// <summary>
        /// 使用技能
        /// </summary>
        public bool UseSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 1) return false;
            
            var slot = slotIndex == 0 ? qSlot : eSlot;
            if (slot.CanUseSkill())
            {
                slot.UseSkill();
                slot.equippedSkill.ApplyEffect(gameObject);
                
                OnSkillUsed?.Invoke(slotIndex);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 为击杀敌人充能
        /// </summary>
        public void AddEnergyForEnemyKill(EnemyType enemyType)
        {
            int energyAmount = enemyType switch
            {
                EnemyType.Normal => normalEnemyEnergy,
                EnemyType.Elite => eliteEnemyEnergy,
                EnemyType.Boss => bossEnemyEnergy,
                _ => 0
            };
            
            if (energyAmount > 0)
            {
                qSlot.AddEnergy(energyAmount);
                eSlot.AddEnergy(energyAmount);
                
                OnEnergyChanged?.Invoke(0, qSlot.currentEnergy);
                OnEnergyChanged?.Invoke(1, eSlot.currentEnergy);
            }
        }
        
        /// <summary>
        /// 进入新房间时重置技能使用状态
        /// </summary>
        public void OnEnterNewRoom()
        {
            qSlot.ResetRoomUsage();
            eSlot.ResetRoomUsage();
        }
        
        /// <summary>
        /// 获取技能槽状态
        /// </summary>
        public SkillSlot GetSlotState(int slotIndex)
        {
            return slotIndex == 0 ? qSlot : eSlot;
        }
        
        private void OnCharacterDeath()
        {
            // 死亡时重置技能状态
            qSlot.currentEnergy = 0;
            eSlot.currentEnergy = 0;
        }
    }
    
    public enum EnemyType
    {
        Normal,
        Elite,
        Boss
    }
}