using ByteSizeLib;
using Microsoft.UI.Xaml.Data;
using System;

namespace DLSS_Swapper.Converters
{
    internal class BytesToMegaBytesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {

            if (value is long bytes)
            {
                return ByteSize.FromBytes(bytes).ToString("MB");
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
