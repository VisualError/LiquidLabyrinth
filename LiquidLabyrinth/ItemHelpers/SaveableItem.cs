using LiquidLabyrinth.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Netcode;

namespace LiquidLabyrinth.ItemHelpers
{
    internal class SaveableItem : GrabbableObject
    {
        private Dictionary<int, string> ES3data = new Dictionary<int, string>();
        public object? Data = null;
        public Type? DataType;
        public override int GetItemDataToSave()
        {
            Dictionary<int, string> data = new Dictionary<int, string>();
            if (!Plugin.Instance.SaveableItemDict.ContainsKey(this.GetType())) Plugin.Instance.SaveableItemDict.Add(this.GetType(), 0);
            Plugin.Instance.SaveableItemDict[this.GetType()]++;
            int Count = Plugin.Instance.SaveableItemDict[this.GetType()];
            data.Add(Count, JsonUtility.ToJson(Data));
            SaveUtils.AddToQueue(GetType(), data, $"ship{itemProperties.itemName}Data");
            return Count;
        }

        public override void LoadItemSaveData(int saveData)
        {
            base.LoadItemSaveData(saveData);
            int key = saveData;
            if (!NetworkManager.Singleton.IsHost || !NetworkManager.Singleton.IsServer) return; // Return if not host or server.
            if (!(ES3.KeyExists($"ship{itemProperties.itemName}Data", GameNetworkManager.Instance.currentSaveFileName))) return;
            ES3data = ES3.Load<Dictionary<int, string>>($"ship{itemProperties.itemName}Data", GameNetworkManager.Instance.currentSaveFileName);
            if (ES3data == null) return;
            if (!ES3data.TryGetValue(key, out string value)) return;
            var dataObject = JsonUtility.FromJson(value, DataType);
            if (dataObject == null)
            {
                Plugin.Logger.LogWarning($"Object data was null/empty for {itemProperties.itemName}. Item save not loaded.");
                // Create new data here. (or just handle it in the item actually, might be more flexible)
                Data = null;
                return; // Don't return once you figure out how to populate the data from the constructor inheriting SaveableItem.
            }
            Plugin.Logger.Log(BepInEx.Logging.LogLevel.All,$"LoadItemSaveData called by {itemProperties.itemName}! Got: {saveData}");
            Data = dataObject;
        }
    }
}
