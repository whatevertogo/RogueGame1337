
using System;
using UnityEngine;

namespace Character
{
    /// <summary>
    /// 角色状态机，管理角色的各种状态转换和逻辑
    /// </summary>
    public class PlayerStateMachine : MonoBehaviour
    {
        /// <summary>
        /// 当前状态
        /// </summary>
        public CharacterState State { get; private set; }

        /// <summary>
        /// 当前状态是否可以被中断
        /// </summary>
        public bool CanInterrupt { get; private set; }

        /// <summary>
        /// 当前状态的持续时间
        /// </summary>
        public float StateDuration { get; private set; }

        /// <summary>
        /// 状态改变事件
        /// </summary>
        public event Action<CharacterState, CharacterState> OnStateChanged;

        /// <summary>
        /// 状态中断请求事件
        /// </summary>
        public event Action<CharacterState> OnStateInterruptRequested;

        // 状态配置
        private readonly bool[] stateInterruptTable;

        public PlayerStateMachine()
        {
            // 初始化状态中断表，定义哪些状态可以中断哪些状态
            stateInterruptTable = new bool[Enum.GetValues(typeof(CharacterState)).Length * Enum.GetValues(typeof(CharacterState)).Length];
            InitializeStateInterruptTable();

            // 初始状态为Idle
            State = CharacterState.Idle;
            CanInterrupt = true;
            StateDuration = 0f;
        }

        /// <summary>
        /// 初始化状态中断表，定义状态之间的中断规则
        /// </summary>
        private void InitializeStateInterruptTable()
        {
            // 默认所有状态都可以中断
            for (int i = 0; i < Enum.GetValues(typeof(CharacterState)).Length; i++)
            {
                for (int j = 0; j < Enum.GetValues(typeof(CharacterState)).Length; j++)
                {
                    stateInterruptTable[i * Enum.GetValues(typeof(CharacterState)).Length + j] = true;
                }
            }

            // 特殊规则：死亡状态不能被任何状态中断
            int deadIndex = (int)CharacterState.Dead;
            for (int i = 0; i < Enum.GetValues(typeof(CharacterState)).Length; i++)
            {
                stateInterruptTable[deadIndex * Enum.GetValues(typeof(CharacterState)).Length + i] = false;
            }

            // 特殊规则：眩晕状态只能被死亡状态中断
            int stunnedIndex = (int)CharacterState.Stunned;
            for (int i = 0; i < Enum.GetValues(typeof(CharacterState)).Length; i++)
            {
                if (i != deadIndex)
                {
                    stateInterruptTable[stunnedIndex * Enum.GetValues(typeof(CharacterState)).Length + i] = false;
                }
            }
        }

        /// <summary>
        /// 更新状态机，应该在每帧调用
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        public void OnTick(float deltaTime)
        {
            StateDuration += deltaTime;
        }

        /// <summary>
        /// 尝试改变状态
        /// </summary>
        /// <param name="newState">新状态</param>
        /// <param name="force">是否强制改变状态</param>
        /// <returns>是否成功改变状态</returns>
        public bool TryChangeState(CharacterState newState, bool force = false)
        {
            // 如果状态相同，不需要改变
            if (State == newState)
                return false;

            // 检查是否可以中断当前状态
            if (!force && !CanInterruptCurrentState(newState))
            {
                OnStateInterruptRequested?.Invoke(newState);
                return false;
            }

            // 改变状态
            CharacterState oldState = State;
            State = newState;
            StateDuration = 0f;

            // 更新可中断性
            UpdateCanInterrupt(newState);

            // 触发状态改变事件
            OnStateChanged?.Invoke(oldState, newState);

            return true;
        }

        /// <summary>
        /// 强制改变状态，无视中断规则
        /// </summary>
        /// <param name="newState">新状态</param>
        public void ForceChangeState(CharacterState newState)
        {
            TryChangeState(newState, true);
        }

        /// <summary>
        /// 检查是否可以中断当前状态
        /// </summary>
        /// <param name="newState">要切换到的新状态</param>
        /// <returns>是否可以中断</returns>
        private bool CanInterruptCurrentState(CharacterState newState)
        {
            if (CanInterrupt)
                return true;

            int currentIndex = (int)State;
            int newIndex = (int)newState;

            return stateInterruptTable[currentIndex * Enum.GetValues(typeof(CharacterState)).Length + newIndex];
        }

        /// <summary>
        /// 根据状态更新可中断性
        /// </summary>
        /// <param name="state">当前状态</param>
        private void UpdateCanInterrupt(CharacterState state)
        {
            switch (state)
            {
                case CharacterState.Idle:
                case CharacterState.Move:
                    CanInterrupt = true;
                    break;
                case CharacterState.Attack:
                    CanInterrupt = false;
                    break;
                //由技能系统控制而不是StateMachine，技能动画与人物无关
                // case CharacterState.Skill:
                //     CanInterrupt = false;
                //     break;
                case CharacterState.Hurt:
                    CanInterrupt = true; // 受伤状态可以被中断
                    break;
                case CharacterState.Stunned:
                    CanInterrupt = false;
                    break;
                case CharacterState.Dead:
                    CanInterrupt = false;
                    break;
                default:
                    CanInterrupt = true;
                    break;
            }
        }

        /// <summary>
        /// 重置状态机到初始状态
        /// </summary>
        public void Reset()
        {
            ForceChangeState(CharacterState.Idle);
        }
    }
}