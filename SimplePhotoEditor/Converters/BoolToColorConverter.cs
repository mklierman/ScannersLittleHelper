using ControlzEx.Theming;
using SimplePhotoEditor.Contracts.Services;
using SimplePhotoEditor.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace SimplePhotoEditor.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public Type EnumType { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return ThemeManager.Current.DetectTheme(SimplePhotoEditor.App.Current).BaseColorScheme == "Dark"
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#194d34"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b1e7cd"));
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
