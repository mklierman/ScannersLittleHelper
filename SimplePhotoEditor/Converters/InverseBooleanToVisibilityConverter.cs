using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SimplePhotoEditor.Converters
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter is string isEnabledParam && isEnabledParam == "IsEnabled")
                {
                    return !boolValue;
                }
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return parameter is string defaultParam && defaultParam == "IsEnabled" ? true : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 