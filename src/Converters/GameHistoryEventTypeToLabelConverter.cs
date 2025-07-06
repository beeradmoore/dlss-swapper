using System;
using DLSS_Swapper.Data;
using DLSS_Swapper.Helpers;
using Microsoft.UI.Xaml.Data;

namespace DLSS_Swapper.Converters;

internal class GameHistoryEventTypeToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is GameHistoryEventType eventType)
        {
            return eventType switch
            {
                GameHistoryEventType.DLLSwap => ResourceHelper.GetString("GameHistoryEventType_DLLSwap"),
                GameHistoryEventType.DLLReset => ResourceHelper.GetString("GameHistoryEventType_DLLReset"),
                _ => ResourceHelper.GetString("General_Unknown"),
            };
        }

        return ResourceHelper.GetString("General_Unknown");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
