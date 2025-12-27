using System;
using System.Globalization;
using ByteSizeLib;
using Microsoft.UI.Xaml.Data;

namespace DLSS_Swapper.Converters;

class BytesToMegaBytesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {

        if (value is long bytes)
        {
            return ByteSize.FromBytes(bytes).ToString("MB", CultureInfo.CurrentCulture);
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
