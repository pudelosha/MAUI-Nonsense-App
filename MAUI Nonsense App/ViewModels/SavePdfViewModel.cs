using System.ComponentModel;
using System.Runtime.CompilerServices;
using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Services;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.ViewModels;

public class SavePdfViewModel : INotifyPropertyChanged
{
    private readonly IDocumentBuilderService _docService;
    private readonly List<ImagePageModel> _pages;

    private string _name = string.Empty;
    private string? _password;
    private int _compressionPercent = 30; // default

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string? Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    /// <summary>0..100; higher = smaller file.</summary>
    public int CompressionPercent
    {
        get => _compressionPercent;
        set
        {
            if (_compressionPercent == value) return;
            _compressionPercent = Math.Clamp(value, 0, 100);
            OnPropertyChanged();
            OnPropertyChanged(nameof(EstimatedSizeText));
            OnPropertyChanged(nameof(EstimatedColor));
        }
    }

    public SavePdfViewModel(IDocumentBuilderService documentBuilderService, List<ImagePageModel> pages)
    {
        _docService = documentBuilderService;
        _pages = pages ?? new();
        _name = $"Document_{DateTime.Now:yyyyMMdd_HHmmss}";
    }

    public string GetSafeFileName()
    {
        var trimmed = Name?.Trim();
        return !string.IsNullOrWhiteSpace(trimmed)
            ? trimmed
            : $"Document_{DateTime.Now:yyyyMMdd_HHmmss}";
    }

    // --- Size estimation (very rough but responsive) ---
    private long OriginalTotalBytes =>
        _pages.Sum(p => Math.Max(1L, p.FileSizeBytes));

    private long EstimatedBytes
    {
        get
        {
            // “quality factor”  (0..1): 1 - compression%
            double q = 1.0 - (CompressionPercent / 100.0);

            // Derived from typical JPEG behaviour (hand-tuned, not exact).
            // Prevent going below ~35% of original and above ~95%.
            double factor = 0.35 + 0.60 * q; // 0% comp => ~95%, 100% comp => ~35%
            return (long)(OriginalTotalBytes * factor);
        }
    }

    private const long MailLimitBytes = 25L * 1024 * 1024; // 25 MB

    public string EstimatedSizeText
    {
        get
        {
            double mb = EstimatedBytes / 1024.0 / 1024.0;
            return $"Estimated size: {mb:0.0} MB";
        }
    }

    public Color EstimatedColor =>
        EstimatedBytes > MailLimitBytes ? Colors.Red : Colors.Green;

    // Save
    public Task<bool> SaveDocumentAsync(string fileName, string? password, List<ImagePageModel> pages, int jpegQuality)
        => _docService.CreatePdfAsync(fileName, password, pages, jpegQuality);

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
