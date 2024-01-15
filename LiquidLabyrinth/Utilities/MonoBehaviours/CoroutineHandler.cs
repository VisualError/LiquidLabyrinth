using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LiquidLabyrinth.Utilities.MonoBehaviours;

internal class CoroutineHandler : MonoBehaviour
{
    private static CoroutineHandler instance;

    public static CoroutineHandler Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject coroutineHandlerObj = new GameObject("CoroutineHandler");
                instance = coroutineHandlerObj.AddComponent<CoroutineHandler>();
                DontDestroyOnLoad(coroutineHandlerObj);
            }
            return instance;
        }
    }
    private Dictionary<object, Dictionary<Type, IEnumerator>> objectsRunningCoroutines = new();
    private Dictionary<Type, IEnumerator> runningCoroutines = new();

    public IEnumerator NewCoroutine(object instance, IEnumerator coroutine, bool stopWhenRunning = false)
    {
        Type coroutineType = coroutine.GetType();
        if (objectsRunningCoroutines.ContainsKey(instance) && objectsRunningCoroutines[instance].ContainsKey(coroutineType))
        {
            if (stopWhenRunning)
            {
                StopCoroutine(objectsRunningCoroutines[instance][coroutineType]);
                objectsRunningCoroutines[instance].Remove(coroutineType);
            }
            else
            {
                Plugin.Logger.LogWarning($"Coroutine {coroutineType.Name} is already running for {instance}");
                return objectsRunningCoroutines[instance][coroutineType];
            }
        }
        if (!objectsRunningCoroutines.ContainsKey(instance))
        {
            objectsRunningCoroutines[instance] = new Dictionary<Type, IEnumerator>();
        }
        Plugin.Logger.LogWarning($"Starting coroutine {coroutineType.Name} for {instance}");
        objectsRunningCoroutines[instance][coroutineType] = coroutine;
        StartCoroutine(ExecuteCoroutine(instance,coroutine));
        return objectsRunningCoroutines[instance][coroutineType];
    }

    private IEnumerator ExecuteCoroutine(object instance,IEnumerator coroutine)
    {
        yield return StartCoroutine(coroutine);
        // Remove the coroutine from the list when it's done
        if(objectsRunningCoroutines.TryGetValue(instance, out Dictionary<Type, IEnumerator> value) && value != null)
        {
            value.Remove(coroutine.GetType());
        }
    }

    public new void StopAllCoroutines()
    {
        base.StopAllCoroutines();
        runningCoroutines.Clear();
    }
}