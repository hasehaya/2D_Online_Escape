using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using DictionaryEntry = System.Collections.DictionaryEntry;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Save
{
    /// <summary>
    /// 現在シーンのセーブ/ロードを統合するコーディネータ。
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class PairSaveCoordinator : MonoBehaviour
    {
        public static PairSaveCoordinator Instance { get; private set; }

        private bool _loaded;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        private void OnEnable()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnStateChanged += OnInventoryChanged;
            }

            if (GameStateService.Instance != null)
            {
                GameStateService.Instance.OnPropertyChanged += OnGamePropertyChanged;
            }
        }

        private void Start()
        {
            Load();
        }

        private void OnDisable()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnStateChanged -= OnInventoryChanged;
            }

            if (GameStateService.Instance != null)
            {
                GameStateService.Instance.OnPropertyChanged -= OnGamePropertyChanged;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveNow();
            }
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }

        public static void RequestSaveIfAvailable()
        {
            if (Instance != null)
            {
                Instance.SaveNow();
            }
        }

        public void Load()
        {
            if (_loaded)
            {
                return;
            }

            string pairKey = SaveSessionContext.GetCurrentPairKey();
            if (string.IsNullOrEmpty(pairKey))
            {
                return;
            }

            PairSaveData pairData;
            if (!PairSaveRepository.TryLoad(pairKey, out pairData))
            {
                _loaded = true;
                return;
            }

            SaveRole role = SaveSessionContext.GetLocalRole();
            RoleSaveData roleData = pairData.GetRoleData(role);

            ApplyInventory(roleData);
            ApplySaveables(roleData);

            if (PhotonNetwork.IsMasterClient)
            {
                ApplySharedProgress(pairData.sharedProgress);
            }

            _loaded = true;
            Debug.Log($"[PairSaveCoordinator] ロード完了 Pair={pairKey} Role={role}");
        }

        public void SaveNow()
        {
            if (!PhotonNetwork.InRoom)
            {
                return;
            }

            string pairKey = SaveSessionContext.GetCurrentPairKey();
            if (string.IsNullOrEmpty(pairKey))
            {
                return;
            }

            PairSaveData pairData;
            if (!PairSaveRepository.TryLoad(pairKey, out pairData))
            {
                pairData = new PairSaveData();
            }

            pairData.pairKey = pairKey;
            pairData.eliasPlayerId = SaveSessionContext.GetRoomString(SaveSessionContext.EliasPlayerIdRoomPropertyKey);
            pairData.noelPlayerId = SaveSessionContext.GetRoomString(SaveSessionContext.NoelPlayerIdRoomPropertyKey);

            SaveRole role = SaveSessionContext.GetLocalRole();
            RoleSaveData roleData = pairData.GetRoleData(role);
            roleData.sceneName = SceneManager.GetActiveScene().name;
            roleData.savedAtTicks = DateTime.UtcNow.Ticks;

            CaptureInventory(roleData);
            CaptureSaveables(roleData);

            if (PhotonNetwork.IsMasterClient)
            {
                CaptureSharedProgress(pairData.sharedProgress);
            }

            PairSaveRepository.Save(pairData);
        }

        private void OnInventoryChanged()
        {
            SaveNow();
        }

        private void OnGamePropertyChanged(string key, object value)
        {
            SaveNow();
        }

        private void CaptureInventory(RoleSaveData roleData)
        {
            roleData.inventoryItemIds.Clear();
            IReadOnlyList<ItemData> items = InventoryManager.Instance.GetItems();
            for (int i = 0; i < items.Count; i++)
            {
                ItemData item = items[i];
                roleData.inventoryItemIds.Add(GetItemSaveId(item));
            }
        }

        private void ApplyInventory(RoleSaveData roleData)
        {
            Dictionary<string, ItemData> itemTable = BuildSceneItemTable();
            List<ItemData> restored = new List<ItemData>();

            for (int i = 0; i < roleData.inventoryItemIds.Count; i++)
            {
                string itemId = roleData.inventoryItemIds[i];
                ItemData item;
                if (itemTable.TryGetValue(itemId, out item))
                {
                    restored.Add(item);
                }
            }

            InventoryManager.Instance.SetItems(restored);
        }

        private Dictionary<string, ItemData> BuildSceneItemTable()
        {
            Dictionary<string, ItemData> table = new Dictionary<string, ItemData>();

            // 新: Saveable な Pickup コンポーネントを持つオブジェクトを先に収集
            PickupObject[] pickupObjects = FindObjectsOfType<PickupObject>(true);
            for (int i = 0; i < pickupObjects.Length; i++)
            {
                ItemData item;
                if (!pickupObjects[i].TryGetPickupItem(out item)) continue;
                string itemId = GetItemSaveId(item);
                if (!table.ContainsKey(itemId)) table.Add(itemId, item);
            }

            // 従来からの InteractableObject も引き続きサポート（シーンの差し替え前互換）
            InteractableObject[] interactables = FindObjectsOfType<InteractableObject>(true);
            for (int i = 0; i < interactables.Length; i++)
            {
                ItemData item;
                if (!interactables[i].TryGetPickupItem(out item)) continue;
                string itemId = GetItemSaveId(item);
                if (!table.ContainsKey(itemId)) table.Add(itemId, item);
            }

            return table;
        }

        private void CaptureSaveables(RoleSaveData roleData)
        {
            roleData.saveables.Clear();
            SaveableBehaviour[] saveables = FindObjectsOfType<SaveableBehaviour>(true);
            for (int i = 0; i < saveables.Length; i++)
            {
                SaveableBehaviour saveable = saveables[i];
                SaveableObjectData data = new SaveableObjectData();
                data.saveId = saveable.SaveId;
                data.stateJson = saveable.CaptureState();
                roleData.saveables.Add(data);
            }
        }

        private void ApplySaveables(RoleSaveData roleData)
        {
            Dictionary<string, string> stateMap = new Dictionary<string, string>();
            for (int i = 0; i < roleData.saveables.Count; i++)
            {
                SaveableObjectData state = roleData.saveables[i];
                stateMap[state.saveId] = state.stateJson;
            }

            SaveableBehaviour[] saveables = FindObjectsOfType<SaveableBehaviour>(true);
            for (int i = 0; i < saveables.Length; i++)
            {
                SaveableBehaviour saveable = saveables[i];
                string json;
                if (stateMap.TryGetValue(saveable.SaveId, out json))
                {
                    saveable.RestoreState(json);
                }
            }
        }

        private void CaptureSharedProgress(SharedProgressData progress)
        {
            progress.bools.Clear();
            progress.floats.Clear();
            progress.ints.Clear();
            progress.strings.Clear();

            foreach (DictionaryEntry entry in PhotonNetwork.CurrentRoom.CustomProperties)
            {
                string key = entry.Key as string;
                object value = entry.Value;

                if (value is bool)
                {
                    BoolEntry boolEntry = new BoolEntry();
                    boolEntry.key = key;
                    boolEntry.value = (bool)value;
                    progress.bools.Add(boolEntry);
                    continue;
                }

                if (value is float)
                {
                    FloatEntry floatEntry = new FloatEntry();
                    floatEntry.key = key;
                    floatEntry.value = (float)value;
                    progress.floats.Add(floatEntry);
                    continue;
                }

                if (value is int)
                {
                    IntEntry intEntry = new IntEntry();
                    intEntry.key = key;
                    intEntry.value = (int)value;
                    progress.ints.Add(intEntry);
                    continue;
                }

                if (value is string)
                {
                    StringEntry stringEntry = new StringEntry();
                    stringEntry.key = key;
                    stringEntry.value = (string)value;
                    progress.strings.Add(stringEntry);
                }
            }
        }

        private void ApplySharedProgress(SharedProgressData progress)
        {
            Hashtable table = new Hashtable();

            for (int i = 0; i < progress.bools.Count; i++)
            {
                table[progress.bools[i].key] = progress.bools[i].value;
            }

            for (int i = 0; i < progress.floats.Count; i++)
            {
                table[progress.floats[i].key] = progress.floats[i].value;
            }

            for (int i = 0; i < progress.ints.Count; i++)
            {
                table[progress.ints[i].key] = progress.ints[i].value;
            }

            for (int i = 0; i < progress.strings.Count; i++)
            {
                table[progress.strings[i].key] = progress.strings[i].value;
            }

            if (table.Count > 0)
            {
                PhotonNetwork.CurrentRoom.SetCustomProperties(table);
            }
        }

        private string GetItemSaveId(ItemData item)
        {
            return string.IsNullOrEmpty(item.id) ? item.name : item.id;
        }
    }
}