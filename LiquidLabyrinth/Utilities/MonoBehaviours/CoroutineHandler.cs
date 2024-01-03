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
    private Dictionary<Type, Dictionary<Type, IEnumerator>> classesRunningCoroutines = new();
    private Dictionary<Type, IEnumerator> runningCoroutines = new();

    public IEnumerator NewCoroutine(Type classType,IEnumerator coroutine, bool stopWhenRunning = false)
    {
        Type coroutineType = coroutine.GetType();
        if (classesRunningCoroutines.ContainsKey(classType) && classesRunningCoroutines[classType].ContainsKey(coroutineType))
        {
            if (stopWhenRunning)
            {
                StopCoroutine(classesRunningCoroutines[classType][coroutineType]);
                classesRunningCoroutines[classType].Remove(coroutineType);
            }
            else
            {
                Plugin.Logger.LogWarning($"Coroutine {coroutineType.Name} is already running for class {classType.Name}");
                return classesRunningCoroutines[classType][coroutineType];
            }
        }
        if (!classesRunningCoroutines.ContainsKey(classType))
        {
            classesRunningCoroutines[classType] = new Dictionary<Type, IEnumerator>();
        }
        Plugin.Logger.LogWarning($"Starting coroutine {coroutineType.Name} for class {classType.Name}");
        classesRunningCoroutines[classType][coroutineType] = coroutine;
        StartCoroutine(ExecuteCoroutine(classType,coroutine));
        return classesRunningCoroutines[classType][coroutineType];
    }

    public IEnumerator NewCoroutine<T>(IEnumerator coroutine, bool stopWhenRunning = false) where T : class
    {
        Type classType = typeof(T);
        Type coroutineType = coroutine.GetType();
        if (classesRunningCoroutines.ContainsKey(classType) && classesRunningCoroutines[classType].ContainsKey(coroutineType))
        {
            if (stopWhenRunning)
            {
                StopCoroutine(classesRunningCoroutines[classType][coroutineType]);
                classesRunningCoroutines[classType].Remove(coroutineType);
            }
            else
            {
                Plugin.Logger.LogWarning($"Coroutine {coroutineType.Name} is already running for class {classType.Name}");
                return classesRunningCoroutines[classType][coroutineType];
            }
        }
        if (!classesRunningCoroutines.ContainsKey(classType))
        {
            classesRunningCoroutines[classType] = new Dictionary<Type, IEnumerator>();
        }
        classesRunningCoroutines[classType][coroutineType] = coroutine;
        StartCoroutine(ExecuteCoroutine(classType, coroutine));
        return classesRunningCoroutines[classType][coroutineType];
    }

    private IEnumerator ExecuteCoroutine<T>(T type,IEnumerator coroutine) where T : Type
    {
        yield return StartCoroutine(coroutine);
        // Remove the coroutine from the list when it's done
        if(classesRunningCoroutines.TryGetValue(type, out Dictionary<Type, IEnumerator> value) && value != null)
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