using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiquidLabyrinth.Utilities
{
    internal class MarkovChain
    {
        private static Dictionary<char, Dictionary<char, Dictionary<char, int>>> markovChain = new Dictionary<char, Dictionary<char, Dictionary<char, int>>>();
        internal static void TrainMarkovChain(string text)
        {
            for (int i = 0; i < text.Length - 2; i++)
            {
                char currentChar = text[i];
                char nextChar = text[i + 1];
                char nextNextChar = text[i + 2];
                // If the current character does not exist in the Markov chain yet, add it
                if (!markovChain.ContainsKey(currentChar))
                {
                    markovChain[currentChar] = new Dictionary<char, Dictionary<char, int>> ();
                }
                if (!markovChain[currentChar].ContainsKey(nextChar))
                {
                    markovChain[currentChar][nextChar] = new Dictionary<char, int>();
                }
                if (!markovChain[currentChar][nextChar].ContainsKey(nextNextChar))
                {
                    markovChain[currentChar][nextChar][nextNextChar] = 0;
                }

                // Increment the count of the transition from the current character to the next character
                markovChain[currentChar][nextChar][nextNextChar]++;
            }
        }
        internal static string GenerateText(int length)
        {
            StringBuilder sb = new StringBuilder();

            // Choose a random initial character
            char currentChar = char.ToUpper(markovChain.Keys.ElementAt(UnityEngine.Random.Range(0, markovChain.Count)));
            sb.Append(currentChar);

            // Follow the Markov chain until we reach the desired length
            for (int i = 0; i < length - 1; i++)
            {
                // Choose a random next character based on the counts in the Markov chain
                var possibleNextChars = markovChain[currentChar];
                var totalCount = possibleNextChars.Count;
                var rand = UnityEngine.Random.Range(0, totalCount);
                foreach (var pair in possibleNextChars)
                {
                    var innerTotalCount = pair.Value.Count;
                    if (rand < innerTotalCount)
                    {
                        currentChar = pair.Key;
                        break;
                    }
                    rand -= innerTotalCount;
                }

                sb.Append(currentChar);
            }

            return sb.ToString();
        }
    }
}
