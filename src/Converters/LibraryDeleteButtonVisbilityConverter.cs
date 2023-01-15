using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLSS_Swapper.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DLSS_Swapper.Converters
{
    internal class LibraryDeleteButtonVisbilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            if (value is LocalRecord localRecord)
            {
                if (localRecord.IsImported == true)
                {
                    return Visibility.Visible;
                }

                if (localRecord.IsDownloaded == true && App.IsWindowsStoreBuild == false)
                {
                    return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
