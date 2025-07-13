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
                GameHistoryEventType.DLLSwapped => ResourceHelper.GetString("GameHistoryEventType_DLLSwapped"),
                GameHistoryEventType.DLLReset => ResourceHelper.GetString("GameHistoryEventType_DLLReset"),
                GameHistoryEventType.DLLDetected => ResourceHelper.GetString("GameHistoryEventType_DLLDetected"),
                GameHistoryEventType.DLLChangedExternally => ResourceHelper.GetString("GameHistoryEventType_DLLChangedExternally"),
                GameHistoryEventType.DLLBackupRemoved => ResourceHelper.GetString("GameHistoryEventType_DLLBackupRemoved"),
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
