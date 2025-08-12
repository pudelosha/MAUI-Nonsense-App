using System.Globalization;

namespace MAUI_Nonsense_App.Helpers
{
    public class MultiplyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return 0.0;
            var totalWidth = values[0] is double d ? d : 0.0;
            var progress = values[1] is double p ? p : 0.0; // 0..1
            progress = Math.Clamp(progress, 0.0, 1.0);
            return totalWidth * progress;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
