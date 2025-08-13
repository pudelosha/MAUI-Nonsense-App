using MAUI_Nonsense_App.Services;
using Microsoft.Maui.ApplicationModel;
using System.ComponentModel;

namespace MAUI_Nonsense_App.ViewModels;

public class MorseViewModel : INotifyPropertyChanged
{
    private readonly ILightService _light;
    private CancellationTokenSource? _cts;
    private bool _useScreenFlashOnly;

    public event PropertyChangedEventHandler? PropertyChanged;

    // UI callbacks
    public event Action<string>? PreviewChanged;
    public event Action<bool>? ScreenFlashChanged;

    public int Wpm { get; set; } = 10;

    public MorseViewModel(ILightService lightService)
    {
        _light = lightService;
    }

    // --- Public API used by the page ---

    public void UpdatePreview(string text)
    {
        PreviewChanged?.Invoke(BuildPrettyPreview(text ?? string.Empty));
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        _cts = null;
        _ = SafeTorch(false);
        ScreenFlashChanged?.Invoke(false);
    }

    public async Task PlayAsync(string message)
    {
        Stop();
        if (string.IsNullOrWhiteSpace(message)) return;

        // Compute standard Morse unit (dot) in ms
        var unitMs = Math.Max(20, (int)Math.Round(1200.0 / Math.Clamp(Wpm, 5, 25)));

        // Check permission & probe torch once; fall back to screen if not available
        _useScreenFlashOnly = !await EnsureTorchPermissionAsync() || !await ProbeTorchOnceAsync();

        // Build an explicit on/off plan (sequence of segments)
        var plan = BuildBlinkPlan(message, unitMs);
        if (plan.Count == 0) return;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        // Update preview text
        UpdatePreview(message);

        try
        {
            foreach (var seg in plan)
            {
                token.ThrowIfCancellationRequested();

                if (seg.on)
                {
                    if (_useScreenFlashOnly)
                    {
                        ScreenFlashChanged?.Invoke(true);
                    }
                    else
                    {
                        if (!await SafeTorch(true))
                        {
                            // Torch failed mid-run; switch to screen for the rest
                            _useScreenFlashOnly = true;
                            ScreenFlashChanged?.Invoke(true);
                        }
                    }
                }
                else
                {
                    if (_useScreenFlashOnly)
                    {
                        ScreenFlashChanged?.Invoke(false);
                    }
                    else
                    {
                        await SafeTorch(false);
                    }
                }

                if (seg.ms > 0)
                    await Task.Delay(seg.ms, token);
            }
        }
        catch (OperationCanceledException) { /* user stopped or navigated away */ }
        finally
        {
            await SafeTorch(false);
            ScreenFlashChanged?.Invoke(false);
        }
    }

    // --- Internal helpers ---

    private record struct Seg(bool on, int ms);

    // ITU timing:
    // dot = 1u on; dash = 3u on
    // gap between symbols in a letter = 1u off
    // gap between letters = 3u off (we already have 1u -> add +2u)
    // gap between words  = 7u off (we already have 1u -> add +6u)
    private List<Seg> BuildBlinkPlan(string message, int unitMs)
    {
        var plan = new List<Seg>(1024);
        var words = (message ?? string.Empty)
            .ToUpperInvariant()
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        bool firstWord = true;

        foreach (var word in words)
        {
            if (!firstWord)
            {
                // ensure previous off-gap totals 7u (we already have at least 1u)
                AppendOffExtend(plan, unitMs * 6);
            }
            firstWord = false;

            bool firstLetter = true;

            foreach (var ch in word)
            {
                if (!_morseMap.TryGetValue(ch, out var code) || code.Length == 0)
                    continue;

                if (!firstLetter)
                {
                    // ensure previous off-gap totals 3u (we already have at least 1u)
                    AppendOffExtend(plan, unitMs * 2);
                }
                firstLetter = false;

                for (int i = 0; i < code.Length; i++)
                {
                    var symbol = code[i];

                    // ON: dot=1u, dash=3u
                    plan.Add(new Seg(true, symbol == '.' ? unitMs : unitMs * 3));

                    // OFF: 1u intra-letter gap after each symbol
                    AppendOffExtend(plan, unitMs);
                }
            }
        }

        // Trim trailing OFF to avoid a long blackout at the end
        if (plan.Count > 0 && !plan[^1].on)
            plan.RemoveAt(plan.Count - 1);

        return plan;
    }

    private static void AppendOffExtend(List<Seg> plan, int extraMs)
    {
        if (extraMs <= 0) return;

        if (plan.Count > 0 && !plan[^1].on)
        {
            // extend existing OFF segment
            plan[^1] = plan[^1] with { ms = plan[^1].ms + extraMs };
        }
        else
        {
            // start a new OFF segment
            plan.Add(new Seg(false, extraMs));
        }
    }

    private string BuildPrettyPreview(string message)
    {
        // Pretty: dot '·', dash '—', 1 space between symbols, 3 spaces between letters, 7 spaces between words
        var sb = new System.Text.StringBuilder();

        bool firstWord = true;
        foreach (var word in (message ?? string.Empty).ToUpperInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!firstWord) sb.Append(' ', 7);
            firstWord = false;

            bool firstLetter = true;
            foreach (var ch in word)
            {
                if (!_morseMap.TryGetValue(ch, out var code) || code.Length == 0)
                    continue;

                if (!firstLetter) sb.Append(' ', 3);
                firstLetter = false;

                for (int i = 0; i < code.Length; i++)
                {
                    if (i > 0) sb.Append(' ');
                    sb.Append(code[i] == '.' ? '·' : '—');
                }
            }
        }

        return sb.Length == 0 ? "—" : sb.ToString();
    }

    private async Task<bool> ProbeTorchOnceAsync()
    {
        try
        {
            await _light.TurnOnAsync();
            await Task.Delay(30);
            await _light.TurnOffAsync();
            return true;
        }
        catch { return false; }
    }

    private async Task<bool> EnsureTorchPermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Camera>();
            return status == PermissionStatus.Granted;
        }
        catch { return false; }
    }

    private async Task<bool> SafeTorch(bool on)
    {
        try
        {
            if (on) await _light.TurnOnAsync();
            else await _light.TurnOffAsync();
            return true;
        }
        catch { return false; }
    }

    // A–Z, 0–9 and a few punctuation chars
    private static readonly Dictionary<char, string> _morseMap = new()
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
        ['&'] = ".-...",
        [':'] = "---...",
        [';'] = "-.-.-.",
        ['='] = "-...-",
        ['+'] = ".-.-.",
        ['-'] = "-....-",
        ['_'] = "..--.-",
        ['"'] = ".-..-.",
        ['$'] = "...-..-",
        ['@'] = ".--.-."
    };
}
