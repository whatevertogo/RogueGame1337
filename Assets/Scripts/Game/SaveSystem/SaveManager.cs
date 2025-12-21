using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
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

        private void Start()
        {
            // 启动时加载元游戏存档
            LoadMeta();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<RoomEnteredEvent>(OnRoomEntered);
            EventBus.Unsubscribe<EntityKilledEvent>(OnEntityKilled);
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
                CDTU.Utils.Logger.Log($"[SaveManager] 创建存档文件夹: {savePath}");
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
        public void SaveRun()
        {
            try
            {
                var data = CreateRunSaveData();
                if (data == null)
                {
                    Debug.LogWarning("[SaveManager] 无法创建 Run 存档数据");
                    return;
                }

                _currentRunSave = data;
                SaveToFile(RUN_AUTO_SAVE_FILE, data);
                OnRunSaved?.Invoke();
                CDTU.Utils.Logger.Log("[SaveManager] Run 存档保存成功");
            }
            catch (Exception ex)
            {
                CDTU.Utils.Logger.LogError($"[SaveManager] Run 存档保存失败: {ex.Message}");
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
                    CDTU.Utils.Logger.Log("[SaveManager] 无 Run 存档，跳过加载");
                    return;
                }

                _currentRunSave = data;
                SaveRestoreUtility.RestoreRunSave(data);
                OnRunSaveLoaded?.Invoke(data);
                CDTU.Utils.Logger.Log($"[SaveManager] Run 存档加载成功: Layer {data.CurrentLayer}, Room {data.CurrentRoomId}");
            }
            catch (Exception ex)
            {
                CDTU.Utils.Logger.LogError($"[SaveManager] Run 存档加载失败: {ex.Message}");
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
                    CDTU.Utils.Logger.Log("[SaveManager] Run 存档已清除");
                }
                _currentRunSave = null;
            }
            catch (Exception ex)
            {
                CDTU.Utils.Logger.LogError($"[SaveManager] 清除 Run 存档失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建当前 Run 存档数据
        /// </summary>
        private RunSaveData CreateRunSaveData()
        {
            var saveData = new RunSaveData();

            // 获取游戏状态
            var gsm = GameRoot.Instance?.GameStateManager;
            if (gsm != null)
            {
                saveData.CurrentLayer = gsm.CurrentLayer;
                saveData.CurrentRoomId = gsm.CurrentRoomId;
            }

            // 获取玩家数据
            var pm = PlayerManager.Instance;
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

                // 获取装备的卡牌
                var skillComp = player.Controller.GetComponent<PlayerSkillComponent>();
                if (skillComp != null)
                {
                    for (int i = 0; i < skillComp.PlayerSkillSlots.Length; i++)
                    {
                        var slot = skillComp.PlayerSkillSlots[i];
                        if (slot?.Runtime?.CardId != null)
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
            var inv = InventoryManager.Instance;
            if (inv != null)
            {
                saveData.Coins = inv.Coins;

                // 使用扩展方法直接转换（零拷贝代码）
                saveData.ActiveCards = inv.ActiveCardStates.ToSaveDataList();

                // 保存被动卡牌
                foreach (var info in inv.PassiveCardIdInfos)
                {
                    saveData.PassiveCards.Add(new PassiveCardSaveData
                    {
                        CardId = info.cardId,
                        Count = info.count
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
        public void SaveMeta()
        {
            try
            {
                if (_metaSave == null)
                {
                    _metaSave = new MetaSaveData();
                }

                // 更新时间戳
                _metaSave.LastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                SaveToFile(META_SAVE_FILE, _metaSave);
                OnMetaSaved?.Invoke();
                CDTU.Utils.Logger.Log("[SaveManager] 元游戏存档保存成功");
            }
            catch (Exception ex)
            {
                CDTU.Utils.Logger.LogError($"[SaveManager] 元游戏存档保存失败: {ex.Message}");
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
                    CDTU.Utils.Logger.Log("[SaveManager] 首次游戏，创建新元存档");
                    return;
                }

                _metaSave = data;
                SaveRestoreUtility.ApplyMetaSave(data);
                OnMetaSaveLoaded?.Invoke(data);
                CDTU.Utils.Logger.Log($"[SaveManager] 元游戏存档加载成功: {data.TotalRuns} 次 Run");
            }
            catch (Exception ex)
            {
                CDTU.Utils.Logger.LogError($"[SaveManager] 元游戏存档加载失败: {ex.Message}");
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
        public void OnRunEnded(bool isVictory, int killsThisRun, int damageThisRun, int layerReached)
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
            _metaSave.TotalDamageDealt += damageThisRun;

            // 更新最佳记录
            if (layerReached > _metaSave.HighestLayerReached)
            {
                _metaSave.HighestLayerReached = layerReached;
            }
            if (killsThisRun > _metaSave.MostKillsInOneRun)
            {
                _metaSave.MostKillsInOneRun = killsThisRun;
            }
            if (damageThisRun > _metaSave.MostDamageInOneRun)
            {
                _metaSave.MostDamageInOneRun = damageThisRun;
            }

            // 清除单局存档
            ClearRunSave();

            // 保存元游戏数据
            SaveMeta();
        }

        #endregion

        #region 底层文件操作

        /// <summary>
        /// 将数据保存到文件
        /// </summary>
        private void SaveToFile<T>(string fileName, T data)
        {
            string filePath = GetSaveFilePath(fileName);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
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
                    CDTU.Utils.Logger.Log("[SaveManager] 所有存档已删除");
                }
            }
            catch (Exception ex)
            {
                CDTU.Utils.Logger.LogError($"[SaveManager] 删除存档失败: {ex.Message}");
            }
        }

        #endregion
    }
}