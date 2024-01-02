using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiquidLabyrinth.Utilities;

internal class MarkovChain
{
    private static Dictionary<char, Dictionary<char, Dictionary<char, int>>> markovChain = new();
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
    private static bool IsSpecialCharacter(char character)
    {
        // Add your logic to determine if the character is special
        // For example, you might check if it's a punctuation mark, symbol, etc.
        return char.IsPunctuation(character) || char.IsSymbol(character);
    }

    internal static string GenerateText(int length, int max)
    {
        StringBuilder sb = new StringBuilder();

        // Choose a random initial character
        char currentChar;
        do
        {
            // Choose a random initial character
            currentChar = char.ToUpper(markovChain.Keys.ElementAt(UnityEngine.Random.Range(0, markovChain.Count)));
        } while (IsSpecialCharacter(currentChar));
        if (!markovChain.ContainsKey(currentChar)) currentChar = char.ToLower(currentChar); // If the uppercase variant doesn't exist for some reason, do this.
        sb.Append(currentChar);
        int charactersAfterSpace = 0;
        // Follow the Markov chain until we reach the desired length
        int i;
        for (i = 0; i < length - 1; i++)
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
            charactersAfterSpace++;
            if ((i >= length - 2 || currentChar == ' ') && charactersAfterSpace <= 2)
            {
                Plugin.Logger.Log(BepInEx.Logging.LogLevel.All, "Word ended too small, adding additional characters.");
                i-=4;
                if(currentChar == ' ') continue;
            }
            if(currentChar == ' ')
            {
                charactersAfterSpace = 0;
            }
            sb.Append(currentChar);
        }
        if(sb.Length > max) // This can happen because of the if(i >= length-2 && charactersAfterSpace <= 2) statement. Could generate some very, very long names.
        {
            Plugin.Logger.Log(BepInEx.Logging.LogLevel.All, $"Generated word was bigger than {max} characters, recreating..");
            return GenerateText(length, max);
        }
        return sb.ToString();
    }
}