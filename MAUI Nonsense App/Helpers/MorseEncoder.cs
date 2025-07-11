namespace MAUI_Nonsense_App.Helpers;

public static class MorseEncoder
{
    private static readonly Dictionary<char, string> MorseMap = new()
    {
        { 'A', ".-" }, { 'B', "-..." }, { 'C', "-.-." }, { 'D', "-.." },
        { 'E', "." }, { 'F', "..-." }, { 'G', "--." }, { 'H', "...." },
        { 'I', ".." }, { 'J', ".---" }, { 'K', "-.-" }, { 'L', ".-.." },
        { 'M', "--" }, { 'N', "-." }, { 'O', "---" }, { 'P', ".--." },
        { 'Q', "--.-" }, { 'R', ".-." }, { 'S', "..." }, { 'T', "-" },
        { 'U', "..-" }, { 'V', "...-" }, { 'W', ".--" }, { 'X', "-..-" },
        { 'Y', "-.--" }, { 'Z', "--.." }, { '1', ".----" }, { '2', "..---" },
        { '3', "...--" }, { '4', "....-" }, { '5', "....." }, { '6', "-...." },
        { '7', "--..." }, { '8', "---.." }, { '9', "----." }, { '0', "-----" },
        { ' ', " " }
    };

    public static string Encode(string text)
    {
        return string.Join(" ", text.ToUpperInvariant()
            .Select(c => MorseMap.TryGetValue(c, out var morse) ? morse : ""));
    }
}
