using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Models
{
    public class ProtractorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public Point Line1End { get; private set; } = new(100, 0);
        public Point Line2End { get; private set; } = new(-100, 0);

        public double Angle => CalculateAngle();

        public void SetLine1(Point vec)
        {
            Line1End = vec;
            OnPropertyChanged(nameof(Line1End));
            OnPropertyChanged(nameof(Angle));
        }

        public void SetLine2(Point vec)
        {
            Line2End = vec;
            OnPropertyChanged(nameof(Line2End));
            OnPropertyChanged(nameof(Angle));
        }

        private double CalculateAngle()
        {
            double dot = (Line1End.X * Line2End.X + Line1End.Y * Line2End.Y);
            double mag1 = Math.Sqrt(Line1End.X * Line1End.X + Line1End.Y * Line1End.Y);
            double mag2 = Math.Sqrt(Line2End.X * Line2End.X + Line2End.Y * Line2End.Y);

            if (mag1 == 0 || mag2 == 0) return 0;

            double cosTheta = dot / (mag1 * mag2);
            cosTheta = Math.Clamp(cosTheta, -1.0, 1.0);

            return Math.Acos(cosTheta) * (180.0 / Math.PI);
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
