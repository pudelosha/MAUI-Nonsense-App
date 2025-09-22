using System;
using System.Threading.Tasks;
using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Services;
using MAUI_Nonsense_App.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel; // MainThread

#if ANDROID
using Android.Widget;
#endif

namespace MAUI_Nonsense_App.Pages.Office
{
    public partial class SavePdfPage : ContentPage
    {
        private readonly SavePdfViewModel _viewModel;
        private readonly PdfCreationSession _session;

        public SavePdfPage(PdfCreationSession session)
        {
            InitializeComponent();

            _session = session ?? throw new ArgumentNullException(nameof(session));

            var docService = App.Services.GetService<IDocumentBuilderService>()
                             ?? throw new InvalidOperationException("DocumentBuilderService not available");

            _viewModel = new SavePdfViewModel(docService, session.Pages);
            BindingContext = _viewModel;
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            var fileName = _viewModel.GetSafeFileName();
            var password = _viewModel.Password;

            // 0..100 (higher = smaller) → JPEG quality 1..95 (higher = better)
            int jpegQuality = Math.Clamp(100 - _viewModel.CompressionPercent, 1, 95);

            bool ok = await _viewModel.SaveDocumentAsync(fileName, password, _session.Pages, jpegQuality);
            if (!ok)
            {
                await DisplayAlert("Error", "Failed to save PDF.", "OK");
                return;
            }

            // Let the list page optionally toast + refresh
            MessagingCenter.Send(this, "PdfSaved");

            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // 1) Close any modals first (if any)
                while (Navigation.ModalStack?.Count > 0)
                    await Navigation.PopModalAsync(animated: false);

                // 2) Find ImageToPdfPage in the back stack and pop directly to it
                var stack = Navigation.NavigationStack;
                int targetIndex = -1;
                for (int i = 0; i < stack.Count; i++)
                {
                    if (stack[i] is MAUI_Nonsense_App.Pages.Office.ImageToPdfPage)
                    {
                        targetIndex = i;
                        break;
                    }
                }

                if (targetIndex >= 0)
                {
                    // Remove intermediate pages above the target (but below current)
                    for (int j = stack.Count - 2; j > targetIndex; j--)
                        Navigation.RemovePage(stack[j]);

                    // Pop current (SavePdfPage) -> lands on ImageToPdfPage
                    await Navigation.PopAsync(animated: false);
                }
                else
                {
                    // If not found (rare), just fall back to pushing a new instance
                    // (Do NOT PopToRoot here, or you'll end up on MainPage again)
                    await Navigation.PopAsync(animated: false); // go back one
                    await Navigation.PushAsync(new MAUI_Nonsense_App.Pages.Office.ImageToPdfPage());
                }
            });

#if ANDROID
    try { Android.Widget.Toast.MakeText(Android.App.Application.Context, "PDF saved", Android.Widget.ToastLength.Short)?.Show(); } catch { }
#endif
        }
    }
}
