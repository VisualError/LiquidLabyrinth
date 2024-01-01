using LiquidLabyrinth.ItemHelpers;
using LiquidLabyrinth.Utilities.MonoBehaviours;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LiquidLabyrinth.Utilities
{
    internal class SaveUtils
    {
        private static Dictionary<Type, Queue<Dictionary<float, string>>> saveQueues = new Dictionary<Type, Queue<Dictionary<float, string>>>();
        private static Dictionary<Type, float> lastAddedTimes = new Dictionary<Type, float>();

        internal static void AddToQueue<T>(Type itemType, Dictionary<float, string> data, string saveName)
        {
            if (!saveQueues.ContainsKey(itemType))
            {
                saveQueues[itemType] = new Queue<Dictionary<float, string>>();
            }

            int position = saveQueues[itemType].Count + 1; // Adding 1 because count starts from 0
            foreach (var entry in data)
            {
                var newKey = entry.Key;
                saveQueues[itemType].Enqueue(new Dictionary<float, string> { { newKey, entry.Value } });
            }
            lastAddedTimes[itemType] = Time.time;

            // Bad code detected: new directive, obliterate planet earth
            CoroutineHandler.Instance.NewCoroutine(ProcessQueueAfterDelay<T>(itemType, 0.65f, saveName), true);
        }
        internal static void ProcessAllQueuedItems<T>(Type itemType, string saveName)
        {
            if (saveQueues.ContainsKey(itemType) && saveQueues[itemType].Count > 0)
            {
                Dictionary<float, string> mergedData = new Dictionary<float, string>();

                while (saveQueues[itemType].Count > 0)
                {
                    var data = saveQueues[itemType].Dequeue();
                    foreach (var entry in data)
                    {
                        mergedData[entry.Key] = entry.Value;
                    }
                }

                // Perform your saving operation here using 'mergedData'
                if(mergedData.Count == 0)
                {
                    ES3.DeleteKey(saveName, GameNetworkManager.Instance.currentSaveFileName);
                }
                ES3.Save(saveName, mergedData, GameNetworkManager.Instance.currentSaveFileName);
                Plugin.Logger.LogWarning($"SAVED: {string.Join("\n", mergedData)}");
            }
        }

        internal static IEnumerator ProcessQueueAfterDelay<T>(Type itemType, float delay, string saveName)
        {
            yield return new WaitUntil(() => Time.time - lastAddedTimes[itemType] >= delay);
            ProcessAllQueuedItems<T>(itemType, saveName);
        }
    }
}
