using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace LiquidLabyrinth.Utilities;

internal class OtherUtils
{
    static List<string> ConvertToMixedCase(List<string> inputList)
    {
        List<string> resultList = new List<string>();

        foreach (var input in inputList)
        {
            resultList.Add(ToMixedCase(input));
        }

        return resultList;
    }

    static string ToMixedCase(string input)
    {
        TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

        // Capitalize the first letter of each word using ToTitleCase
        string result = textInfo.ToTitleCase(input);

        return result;
    }

    private static Dictionary<int, int> _masksByLayer;

    internal static bool TryDestroyRigidBody(GameObject gameObject)
    {
        if (gameObject.TryGetComponent(out Rigidbody body))
        {
            Plugin.Logger.LogWarning("destroying rigid");
            UnityEngine.Object.Destroy(body);
            return true;
        }
        return false;
    }

    internal static void GenerateLayerMap()
    {
        _masksByLayer = new Dictionary<int, int>();
        for (int i = 0; i < 32; i++)
        {
            int mask = 0;
            for (int j = 0; j < 32; j++)
            {
                if (!Physics.GetIgnoreLayerCollision(i, j))
                {
                    mask |= 1 << j;
                }
            }
            _masksByLayer.Add(i, mask);
        }
    }

    internal static void SetTagRecursively(GameObject obj, string tag)
    {
        if (obj == null) return;
        foreach (Transform child in obj.transform)
        {
            child.gameObject.tag = tag;
            SetTagRecursively(child.gameObject, tag);
        }
    }

    internal static float mapValue(float mainValue, float inValueMin, float inValueMax, float outValueMin, float outValueMax)
    {
        return (mainValue - inValueMin) * (outValueMax - outValueMin) / (inValueMax - inValueMin) + outValueMin;
    }

    internal static int MaskForLayer(int layer)
    {
        return _masksByLayer[layer];
    }
}