using System.Collections.Generic;
using System.Text;

namespace MAUI_Nonsense_App.Helpers;

public enum MorseChunk { Dot, Dash, LetterGap, WordGap }

public static class MorseEncoder
{
    private static readonly Dictionary<char, string> Map = new()
    {
        ['A'] = ".-",
        ['B'] = "-...",
        ['C'] = "-.-.",
        ['D'] = "-..",
        ['E'] = ".",
        ['F'] = "..-.",
        ['G'] = "--.",
        ['H'] = "....",
        ['I'] = "..",
        ['J'] = ".---",
        ['K'] = "-.-",
        ['L'] = ".-..",
        ['M'] = "--",
        ['N'] = "-.",
        ['O'] = "---",
        ['P'] = ".--.",
        ['Q'] = "--.-",
        ['R'] = ".-.",
        ['S'] = "...",
        ['T'] = "-",
        ['U'] = "..-",
        ['V'] = "...-",
        ['W'] = ".--",
        ['X'] = "-..-",
        ['Y'] = "-.--",
        ['Z'] = "--..",
        ['0'] = "-----",
        ['1'] = ".----",
        ['2'] = "..---",
        ['3'] = "...--",
        ['4'] = "....-",
        ['5'] = ".....",
        ['6'] = "-....",
        ['7'] = "--...",
        ['8'] = "---..",
        ['9'] = "----.",
        ['.'] = ".-.-.-",
        [','] = "--..--",
        ['?'] = "..--..",
        ['!'] = "-.-.--",
        ['/'] = "-..-.",
        ['('] = "-.--.",
        [')'] = "-.--.-",
        [':'] = "---...",
        [';'] = "-.-.-.",
        ['='] = "-...-",
        ['+'] = ".-.-.",
        ['-'] = "-....-",
        ['_'] = "..--.-",
        ['"'] = ".-..-.",
        ['@'] = ".--.-.",
        ['\''] = ".----."
    };

    public static string Encode(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var sb = new StringBuilder();
        var words = text.Trim().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (int w = 0; w < words.Length; w++)
        {
            var word = words[w];
            for (int i = 0; i < word.Length; i++)
            {
                var ch = word[i];
                if (Map.TryGetValue(ch, out var code))
                {
                    if (sb.Length > 0) sb.Append("   "); // 3 spaces between letters visually
                    sb.Append(code.Replace('.', '·').Replace('-', '—')); // nice preview glyphs
                }
            }
            if (w < words.Length - 1) sb.Append("       "); // 7 spaces between words (preview)
        }

        return sb.ToString();
    }

    /// <summary>
    /// Emits chunks in timing order from the raw text. Player applies unit-based durations.
    /// </summary>
    public static IEnumerable<MorseChunk> EnumerateChunks(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var words = text.Trim().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (int w = 0; w < words.Length; w++)
        {
            var word = words[w];

            bool emittedInLetter = false;

            for (int i = 0; i < word.Length; i++)
            {
                var ch = word[i];
                if (!Map.TryGetValue(ch, out var code))
                    continue;

                if (emittedInLetter)
                    yield return MorseChunk.LetterGap; // gap between previous letter and this one
                emittedInLetter = true;

                // symbols inside a letter
                for (int s = 0; s < code.Length; s++)
                {
                    yield return code[s] == '.' ? MorseChunk.Dot : MorseChunk.Dash;
                    if (s < code.Length - 1)
                        yield return MorseChunk.Dot; // we interpret "Dot" here as 1u gap (player handles as pause of 1u)
                }
            }

            if (w < words.Length - 1)
                yield return MorseChunk.WordGap;
        }
    }
}
