using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LiquidLabyrinth.Utilities.MonoBehaviours
{
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

        private Dictionary<Type, IEnumerator> runningCoroutines = new Dictionary<Type, IEnumerator>();

        public IEnumerator NewCoroutine(IEnumerator coroutine, bool stopWhenRunning = false)
        {
            // Check if the same IEnumerator instance is already running
            if (stopWhenRunning && IsCoroutineRunning(coroutine.GetType()))
            {
                StopCoroutine(runningCoroutines[coroutine.GetType()]);
                runningCoroutines.Remove(coroutine.GetType());
            }
            if (!IsCoroutineRunning(coroutine.GetType()))
            {
                // Start the coroutine and store the reference
                runningCoroutines[coroutine.GetType()] = coroutine;
                StartCoroutine(ExecuteCoroutine(coroutine));
            }
            else if (!stopWhenRunning)
            {
                Plugin.Logger.LogWarning($"Coroutine {coroutine.GetType().Name} is already running");
            }
            return runningCoroutines[coroutine.GetType()];
        }

        private bool IsCoroutineRunning(Type coroutine)
        {
            // Check if any IEnumerator in the list is equal to the given coroutine
            return runningCoroutines.ContainsKey(coroutine);
        }

        private IEnumerator ExecuteCoroutine(IEnumerator coroutine)
        {
            yield return StartCoroutine(coroutine);
            // Remove the coroutine from the list when it's done
            runningCoroutines.Remove(coroutine.GetType());
        }

        public new void StopAllCoroutines()
        {
            base.StopAllCoroutines();
            runningCoroutines.Clear();
        }
    }
}
