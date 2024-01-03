using LiquidLabyrinth.Utilities.MonoBehaviours;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LiquidLabyrinth.Utilities;

internal class SaveUtils
{
    private static Dictionary<object, Queue<Dictionary<int, string>>> saveQueues = new();
    private static Dictionary<object, float> lastAddedTimes = new();

    internal static void AddToQueue<T>(T itemType, Dictionary<int, string> data, string saveName)
    {
        if (itemType == null) return;
        if (!saveQueues.ContainsKey(itemType))
        {
            saveQueues[itemType] = new Queue<Dictionary<int, string>>();
        }

        int position = saveQueues[itemType].Count + 1; // Adding 1 because count starts from 0
        foreach (var entry in data)
        {
            var newKey = entry.Key;
            saveQueues[itemType].Enqueue(new Dictionary<int, string> { { newKey, entry.Value } });
        }
        lastAddedTimes[itemType] = Time.time;

        // Bad code detected: new directive, obliterate planet earth
        CoroutineHandler.Instance.NewCoroutine(ProcessQueueAfterDelay<T>(itemType, 0.65f, saveName), true);
    }

    internal static void ProcessAllQueuedItems<T>(T itemType, string saveName)
    {
        if (itemType == null) return;
        if (saveQueues.ContainsKey(itemType) && saveQueues[itemType].Count > 0)
        {
            Dictionary<int, string> mergedData = new Dictionary<int, string>();

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

    internal static IEnumerator ProcessQueueAfterDelay<T>(T itemType, float delay, string saveName)
    {
        if (itemType == null) yield break;
        yield return new WaitUntil(() => Time.time - lastAddedTimes[itemType] >= delay);
        ProcessAllQueuedItems<T>(itemType, saveName);
    }
}