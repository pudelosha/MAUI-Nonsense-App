using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.ViewModels;

public class ImageArrangeViewModel : INotifyPropertyChanged
{
    public ObservableCollection<ImagePageModel> Pages { get; }

    public ImageArrangeViewModel(PdfCreationSession session)
    {
        Pages = new ObservableCollection<ImagePageModel>(session.Pages);
    }

    public void MoveUp(ImagePageModel page)
    {
        int index = Pages.IndexOf(page);
        if (index > 0)
        {
            Pages.Move(index, index - 1);
        }
    }

    public void MoveDown(ImagePageModel page)
    {
        int index = Pages.IndexOf(page);
        if (index < Pages.Count - 1)
        {
            Pages.Move(index, index + 1);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
