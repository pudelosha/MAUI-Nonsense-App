using System.Globalization;

namespace MAUI_Nonsense_App.Helpers
{
    public sealed class BoolToStyleConverter : IValueConverter
    {
        public static readonly BoolToStyleConverter Instance = new();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // parameter packs "normalStyleKey,selectedStyleKey"
            if (parameter is not Style[] styles || styles.Length != 2)
                return null;
            return (value is bool b && b) ? styles[1] : styles[0];
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }
}
