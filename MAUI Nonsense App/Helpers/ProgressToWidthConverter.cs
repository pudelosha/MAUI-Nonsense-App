using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MAUI_Nonsense_App.Helpers
{
    public sealed class ProgressToWidthConverter : IMultiValueConverter
    {
        public double MinPixels { get; set; } = 10;

        /// <summary>
        /// If true, the converter will also apply <see cref="MinPixels"/> when progress == 0.
        /// </summary>
        public bool MinWhenZero { get; set; } = true;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length >= 2 &&
                values[0] is double trackWidth &&
                values[1] is double progress)
            {
                if (double.IsNaN(trackWidth) || trackWidth <= 0)
                    return 0d;

                var p = Math.Clamp(progress, 0d, 1d);
                var w = trackWidth * p;

                // Enforce minimum width
                if ((MinWhenZero && p == 0d) || (p > 0d && w < MinPixels))
                    w = MinPixels;

                if (w > trackWidth)
                    w = trackWidth;

                return w;
            }

            return 0d;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
