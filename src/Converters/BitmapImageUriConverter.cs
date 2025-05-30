using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace DLSS_Swapper.Converters
{
    internal class BitmapImageUriConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string stringValue)
            {
                if (string.IsNullOrEmpty(stringValue))
                {
                    return null;
                }

                return new Uri(stringValue);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
