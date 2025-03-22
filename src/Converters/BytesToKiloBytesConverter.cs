using System;
using ByteSizeLib;
using Microsoft.UI.Xaml.Data;

namespace DLSS_Swapper.Converters;

internal class BytesToKiloBytesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {

        if (value is long bytes)
        {
            var hBytes = ByteSize.FromBytes(bytes);
            return Math.Ceiling(hBytes.KiloBytes).ToString("n0") + " KB";
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
