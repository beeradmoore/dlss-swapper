﻿using DLSS_Swapper.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Converters
{
    class DLSSStateVisibilityConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty DesierdStateProperty = DependencyProperty.Register(nameof(DesierdState), typeof(string), typeof(DLSSStateVisibilityConverter), new PropertyMetadata(null));
        public string DesierdState
        {
            get { return (string)GetValue(DesierdStateProperty); }
            set { SetValue(DesierdStateProperty, value); }
        }


        public DLSSStateVisibilityConverter()
        {

        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is null)
            {
                return Visibility.Collapsed;
            }

            if (value is LocalRecord localRecord)
            {
                if (DesierdState == "Downloading")
                {
                    return localRecord.IsDownloading ? Visibility.Visible : Visibility.Collapsed;
                }
                else if (DesierdState == "Downloaded")
                {
                    return localRecord.IsDownloaded ? Visibility.Visible : Visibility.Collapsed;
                }
                else if (DesierdState == "NotFound")
                {
                    if (localRecord.IsDownloading)
                    {
                        return Visibility.Collapsed;
                    }

                    if (localRecord.IsDownloaded)
                    {
                        return Visibility.Collapsed;
                    }

                    return Visibility.Visible;
                }
                else if (DesierdState == "Imported")
                {
                    return localRecord.IsImported ? Visibility.Visible : Visibility.Collapsed;
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
