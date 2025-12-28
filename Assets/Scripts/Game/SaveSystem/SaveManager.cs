using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Core.Events;
using RogueGame.Events;
using CDTU.Utils;
using Character.Player;
using Character.Components;

namespace RogueGame.SaveSystem
{
    /// <summary>
    /// 存档管理器 - 负责游戏数据的保存和加载
    /// 双层存档系统：Run Save（单局）+ Meta Save（元游戏）
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        private const string SAVE_FOLDER = "Saves";
        private const string RUN_AUTO_SAVE_FILE = "RunAutoSave.json";
        private const string META_SAVE_FILE = "MetaSave.json";

        [Header("存档设置")]
        [SerializeField] private bool enableAutoSave = true;
        [SerializeField] private bool autoSaveOnRoomEnter = true;  // 每次进入新房间自动保存

        private RunSaveData _currentRunSave;
        private MetaSaveData _metaSave;

        public RunSaveData CurrentRunSave => _currentRunSave;
        public MetaSaveData MetaSave => _metaSave;

        public static event Action<RunSaveData> OnRunSaveLoaded;
        public static event Action OnRunSaved;
        public static event Action<MetaSaveData> OnMetaSaveLoaded;
        public static event Action OnMetaSaved;

        protected override void Awake()
        {
            base.Awake();
            InitializeSaveFolder();

            // 订阅游戏事件
            EventBus.Subscribe<RoomEnteredEvent>(OnRoomEntered);
            EventBus.Subscribe<EntityKilledEvent>(OnEntityKilled);
        }

        //由GameRoot加载元游戏存档
        // private void Start()
        // {
        //     // 启动时加载元游戏存档
        //     LoadMeta();
        // }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<RoomEnteredEvent>(OnRoomEntered);
            EventBus.Unsubscribe<EntityKilledEvent>(OnEntityKilled);
            base.OnDestroy();
        }

        #region 文件系统

        /// <summary>
        /// 初始化存档文件夹
        /// </summary>
        private void InitializeSaveFolder()
        {
            string savePath = GetSaveFolderPath();
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                CDTU.Utils.CDLogger.Log($"[SaveManager] 创建存档文件夹: {savePath}");
            }
        }

        /// <summary>
        /// 获取存档文件夹路径
        /// </summary>
        private string GetSaveFolderPath()
        {
            return Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
        }

        /// <summary>
        /// 获取完整存档文件路径
        /// </summary>
        private string GetSaveFilePath(string fileName)
        {
            return Path.Combine(GetSaveFolderPath(), fileName);
        }

        #endregion

        #region Run Save（单局存档）

        /// <summary>
        /// 保存当前 Run 进度
        /// </summary>
        /// <returns>是否保存成功</returns>
        public bool SaveRun()
        {
            try
            {
                var data = CreateRunSaveData();
                if (data == null)
                {
                    CDTU.Utils.CDLogger.LogWarning("[SaveManager] 无法创建 Run 存档数据（缺少必要的游戏组件）");
                    return false;
                }

                // 先保存文件，成功后再更新内部状态
                if (!SaveToFile(RUN_AUTO_SAVE_FILE, data))
                {
                    CDTU.Utils.CDLogger.LogError("[SaveManager] Run 存档文件写入失败");
                    return false;
                }

                // 仅在文件写入成功后才更新状态和触发事件
                _currentRunSave = data;
                OnRunSaved?.Invoke();
                CDTU.Utils.CDLogger.Log("[SaveManager] Run 存档保存成功");
                return true;
            }
            catch (Exception ex)
            {
                CDTU.Utils.CDLogger.LogError($"[SaveManager] Run 存档保存失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 加载 Run 存档
        /// </summary>
        public void LoadRun()
        {
            try
            {
                var data = LoadFromFile<RunSaveData>(RUN_AUTO_SAVE_FILE);
                if (data == null)
                {
                    CDTU.Utils.CDLogger.Log("[SaveManager] 无 Run 存档，跳过加载");
                    return;
                }

                _currentRunSave = data;
                SaveRestoreUtility.RestoreRunSave(data);
                OnRunSaveLoaded?.Invoke(data);
                CDTU.Utils.CDLogger.Log($"[SaveManager] Run 存档加载成功: Layer {data.CurrentLayer}, Room {data.CurrentRoomId}");
            }
            catch (Exception ex)
            {
                CDTU.Utils.CDLogger.LogError($"[SaveManager] Run 存档加载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除 Run 存档（Run 结束时调用）
        /// </summary>
        public void ClearRunSave()
        {
            try
            {
                string filePath = GetSaveFilePath(RUN_AUTO_SAVE_FILE);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    CDTU.Utils.CDLogger.Log("[SaveManager] Run 存档已清除");
                }
                _currentRunSave = null;
            }
            catch (Exception ex)
            {
                CDTU.Utils.CDLogger.LogError($"[SaveManager] 清除 Run 存档失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建当前 Run 存档数据
        /// </summary>
        /// <returns>存档数据，如果缺少关键依赖则返回 null</returns>
        private RunSaveData CreateRunSaveData()
        {
            // 验证关键依赖
            var gsm = GameRoot.Instance?.GameFlowCoordinator;
            var pm = PlayerManager.Instance;
            var inv = InventoryServiceManager.Instance;

            // 检查关键组件是否存在
            bool hasRequiredComponents = true;
            if (gsm == null)
            {
                CDTU.Utils.CDLogger.LogWarning("[SaveManager] GameFlowCoordinator not found");
                hasRequiredComponents = false;
            }
            if (pm == null)
            {
                CDTU.Utils.CDLogger.LogWarning("[SaveManager] PlayerManager not found");
                hasRequiredComponents = false;
            }
            if (inv == null)
            {
                CDTU.Utils.CDLogger.LogWarning("[SaveManager] InventoryManager not found");
                hasRequiredComponents = false;
            }

            // 如果缺少关键组件，返回 null
            if (!hasRequiredComponents)
            {
                return null;
            }

            var saveData = new RunSaveData();

            // 获取游戏状态
            if (gsm != null)
            {
                saveData.CurrentLayer = gsm.CurrentLayer;
                saveData.CurrentRoomId = gsm.CurrentRoomId;
            }

            // 获取玩家数据
            var player = pm?.GetLocalPlayerState();
            if (player?.Controller != null)
            {
                var stats = player.Controller.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    saveData.PlayerData = new PlayerSaveData
                    {
                        CurrentHP = stats.CurrentHP,
                        MaxHP = stats.MaxHP.Value,
                        AttackPower = stats.AttackPower.Value,
                        AttackSpeed = stats.AttackSpeed.Value,
                        MoveSpeed = stats.MoveSpeed.Value,
                        Armor = stats.Armor.Value,
                        Dodge = stats.Dodge.Value,
                        SkillCooldownReductionRate = stats.SkillCooldownReductionRate.Value
                    };
                }

                // 获取装备的卡牌（改进空值检查）
                var skillComp = player.Controller.GetComponent<PlayerSkillComponent>();
                if (skillComp != null)
                {
                    for (int i = 0; i < skillComp.PlayerSkillSlots.Length; i++)
                    {
                        var slot = skillComp.PlayerSkillSlots[i];
                        // 使用 IsNullOrEmpty 检查 CardId
                        if (slot?.Runtime != null && !string.IsNullOrEmpty(slot.Runtime.CardId))
                        {
                            saveData.EquippedCards.Add(new EquippedCardData
                            {
                                SlotIndex = i,
                                CardId = slot.Runtime.CardId,
                                InstanceId = slot.Runtime.InstanceId
                            });
                        }
                    }
                }
            }

            // 获取背包数据
            if (inv != null)
            {
                saveData.Coins = inv.Coins;

                // 使用扩展方法直接转换（零拷贝代码）
                saveData.ActiveCards = inv.ActiveCardStates.ToSaveDataList();

                // 保存被动卡牌
                foreach (var info in inv.PassiveCards)
                {
                    saveData.PassiveCards.Add(new PassiveCardSaveData
                    {
                        CardId = info.CardId,
                        Count = info.Count
                    });
                }
            }

            return saveData;
        }

        #endregion

        #region Meta Save（元游戏存档）

        /// <summary>
        /// 保存元游戏数据
        /// </summary>
        /// <returns>是否保存成功</returns>
        public bool SaveMeta()
        {
            try
            {
                if (_metaSave == null)
                {
                    _metaSave = new MetaSaveData();
                }

                // 更新时间戳
                _metaSave.LastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (!SaveToFile(META_SAVE_FILE, _metaSave))
                {
                    CDTU.Utils.CDLogger.LogError("[SaveManager] 元游戏存档文件写入失败");
                    return false;
                }

                OnMetaSaved?.Invoke();
                CDTU.Utils.CDLogger.Log("[SaveManager] 元游戏存档保存成功");
                return true;
            }
            catch (Exception ex)
            {
                CDTU.Utils.CDLogger.LogError($"[SaveManager] 元游戏存档保存失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 加载元游戏存档
        /// </summary>
        public void LoadMeta()
        {
            try
            {
                var data = LoadFromFile<MetaSaveData>(META_SAVE_FILE);
                if (data == null)
                {
                    // 首次游戏，创建新的元存档
                    _metaSave = new MetaSaveData();
                    CDTU.Utils.CDLogger.Log("[SaveManager] 首次游戏，创建新元存档");
                    return;
                }

                _metaSave = data;
                SaveRestoreUtility.ApplyMetaSave(data);
                OnMetaSaveLoaded?.Invoke(data);
                CDTU.Utils.CDLogger.Log($"[SaveManager] 元游戏存档加载成功: {data.TotalRuns} 次 Run");
            }
            catch (Exception ex)
            {
                CDTU.Utils.CDLogger.LogError($"[SaveManager] 元游戏存档加载失败: {ex.Message}");
                _metaSave = new MetaSaveData();
            }
        }

        /// <summary>
        /// 记录 Run 开始
        /// </summary>
        public void OnRunStarted()
        {
            if (_metaSave != null)
            {
                _metaSave.TotalRuns++;
            }
        }

        /// <summary>
        /// 记录 Run 结束（通关或死亡）
        /// </summary>
        public void OnRunEnded(bool isVictory, int killsThisRun, int layerReached)
        {
            if (_metaSave == null) return;

            if (isVictory)
            {
                _metaSave.SuccessfulRuns++;
            }
            else
            {
                _metaSave.TotalDeaths++;
            }

            // 更新统计
            _metaSave.TotalKills += killsThisRun;

            // 更新最佳记录
            if (layerReached > _metaSave.HighestLayerReached)
            {
                _metaSave.HighestLayerReached = layerReached;
            }
            if (killsThisRun > _metaSave.MostKillsInOneRun)
            {
                _metaSave.MostKillsInOneRun = killsThisRun;
            }

            // 清除单局存档
            ClearRunSave();

            // 保存元游戏数据
            SaveMeta();
        }

        #endregion

        #region 底层文件操作

        /// <summary>
        /// 将数据保存到文件（原子写入，防止崩溃损坏存档）
        /// </summary>
        /// <returns>是否保存成功</returns>
        private bool SaveToFile<T>(string fileName, T data)
        {
            try
            {
                string filePath = GetSaveFilePath(fileName);
                string tempPath = filePath + ".tmp";

                // 序列化为 JSON
                string json = JsonUtility.ToJson(data, true);

                // 先写入临时文件
                File.WriteAllText(tempPath, json);

                // 原子替换（如果目标文件存在，会先删除）
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                File.Move(tempPath, filePath);

                return true;
            }
            catch (Exception ex)
            {
                CDTU.Utils.CDLogger.LogError($"[SaveManager] SaveToFile '{fileName}' failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从文件加载数据
        /// </summary>
        private T LoadFromFile<T>(string fileName) where T : class
        {
            string filePath = GetSaveFilePath(fileName);
            if (!File.Exists(filePath))
            {
                return null;
            }

            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<T>(json);
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 房间进入事件 - 自动保存
        /// </summary>
        private void OnRoomEntered(RoomEnteredEvent evt)
        {
            if (enableAutoSave && autoSaveOnRoomEnter)
            {
                SaveRun();
            }
        }

        /// <summary>
        /// 击杀事件 - 更新统计
        /// </summary>
        private void OnEntityKilled(EntityKilledEvent evt)
        {
            if (_currentRunSave != null && evt.Attacker != null)
            {
                var player = evt.Attacker.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    _currentRunSave.KillsThisRun++;
                }
            }
        }

        #endregion

        #region 公共 API

        /// <summary>
        /// 检查是否存在 Run 存档
        /// </summary>
        public bool HasRunSave()
        {
            string filePath = GetSaveFilePath(RUN_AUTO_SAVE_FILE);
            return File.Exists(filePath);
        }

        /// <summary>
        /// 删除所有存档
        /// </summary>
        public void DeleteAllSaves()
        {
            try
            {
                string savePath = GetSaveFolderPath();
                if (Directory.Exists(savePath))
                {
                    Directory.Delete(savePath, true);
                    Directory.CreateDirectory(savePath);
                    _currentRunSave = null;
                    _metaSave = new MetaSaveData();
                    CDTU.Utils.CDLogger.Log("[SaveManager] 所有存档已删除");
                }
            }
            catch (Exception ex)
            {
                CDTU.Utils.CDLogger.LogError($"[SaveManager] 删除存档失败: {ex.Message}");
            }
        }

        public void SaveRunToMetaOnDeath()
        {
            if (_currentRunSave != null)
            {
                OnRunEnded(false, _currentRunSave.KillsThisRun, _currentRunSave.CurrentLayer);
            }
        }

        #endregion
    }
}