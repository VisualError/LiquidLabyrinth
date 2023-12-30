using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LiquidLabyrinth.Utilities
{
    public static class AudioExt
    {
        internal static AudioClip MakeSubclip(this AudioClip clip, float start, float stop)
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
        public static IEnumerator FadeOut(this AudioSource audioSource, float fadeTime)
        {
            float startVolume = audioSource.volume;
            while (audioSource.volume > 0)
            {
                audioSource.volume -= startVolume * Time.deltaTime / fadeTime;
                yield return null;
            }
            audioSource.Stop();
            audioSource.volume = audioSource.maxDistance-audioSource.minDistance;
        }
    }
}
