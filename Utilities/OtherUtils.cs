using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LiquidLabyrinth.Utilities
{
    internal class OtherUtils
    {
        private static Dictionary<int, int> _masksByLayer;

        internal static AudioClip MakeSubclip(AudioClip clip, float start, float stop)
        {
            /* Create a new audio clip */
            int frequency = clip.frequency;
            float timeLength = stop - start;
            int samplesLength = (int)(frequency * timeLength);
            AudioClip newClip = AudioClip.Create(clip.name + "-sub", samplesLength, 1, frequency, false);

            /* Create a temporary buffer for the samples */
            float[] data = new float[samplesLength];
            /* Get the data from the original clip */
            clip.GetData(data, (int)(frequency * start));
            /* Transfer the data to the new clip */
            newClip.SetData(data, 0);

            /* Return the sub clip */
            return newClip;
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
}
