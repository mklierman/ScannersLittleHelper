using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace SimplePhotoEditor.Converters
{
    public class BooleanToCursorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled && isEnabled)
            {
                return parameter?.ToString()?.ToLower() switch
                {
                    "cross" => Cursors.Cross,
                    _ => Cursors.Arrow
                };
            }
            return Cursors.Arrow;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 