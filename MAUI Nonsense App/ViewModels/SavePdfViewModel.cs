using System.ComponentModel;
using System.Runtime.CompilerServices;
using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.ViewModels;

public class SavePdfViewModel : INotifyPropertyChanged
{
    private readonly IDocumentBuilderService _docService;

    private string _name = string.Empty;
    private string? _password;

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

    public SavePdfViewModel(IDocumentBuilderService documentBuilderService)
    {
        _docService = documentBuilderService;
        _name = $"Document_{DateTime.Now:yyyyMMdd_HHmmss}";
    }

    public string GetSafeFileName()
    {
        var trimmed = Name?.Trim();
        return !string.IsNullOrWhiteSpace(trimmed)
            ? trimmed
            : $"Document_{DateTime.Now:yyyyMMdd_HHmmss}";
    }

    public Task<bool> SaveDocumentAsync(string fileName, string? password, List<ImagePageModel> pages)
        => _docService.CreatePdfAsync(fileName, password, pages);

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
