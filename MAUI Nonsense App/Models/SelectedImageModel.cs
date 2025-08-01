using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUI_Nonsense_App.Models
{
    public class SelectedImageModel
    {
        public string FilePath { get; set; } = string.Empty;
        public string ThumbnailPath => FilePath;
        public string FileName => Path.GetFileName(FilePath);
        public string DisplayDate => File.GetCreationTime(FilePath).ToString("yyyy-MM-dd HH:mm");
        public string DisplaySize => new FileInfo(FilePath).Length / 1024 + " KB";
        public string Source { get; set; } = "Gallery"; // or "Photo"
    }
}
