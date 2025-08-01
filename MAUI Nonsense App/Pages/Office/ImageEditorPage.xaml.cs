using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.Pages.Office;

public partial class ImageEditorPage : ContentPage
{
    private readonly ImagePageModel _imagePage;

    public ImageEditorPage(ImagePageModel imagePage)
    {
        InitializeComponent();
        _imagePage = imagePage;
        LoadImage();
    }

    private void LoadImage()
    {
        if (File.Exists(_imagePage.FilePath))
        {
            EditableImage.Source = ImageSource.FromFile(_imagePage.FilePath);
        }
    }

    private async void OnDoneClicked(object sender, EventArgs e)
    {
        // In future: Save cropped frame coordinates to _imagePage.FrameCrop
        await Navigation.PopAsync(); // Go back to arrange page
    }
}
