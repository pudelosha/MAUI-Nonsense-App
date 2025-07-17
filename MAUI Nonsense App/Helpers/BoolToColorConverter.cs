using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MAUI_Nonsense_App.Helpers;

public class BoolToColorConverter : IValueConverter
{
    public Color TrueColor { get; set; } = Colors.LightGreen;
    public Color FalseColor { get; set; } = Colors.Transparent;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? TrueColor : FalseColor;

        return FalseColor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
