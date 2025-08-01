namespace MAUI_Nonsense_App.Models
{
    public class PdfCreationSession
    {
        public List<ImagePageModel> Pages { get; set; } = new();
        public string? Name { get; set; }
        public string? Password { get; set; }
    }
}
