using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DLSS_Swapper.UserControls
{
    internal class EasyContentDialog : ContentDialog
    {
        public EasyContentDialog(XamlRoot xamlRoot) : base()
        {
            XamlRoot = xamlRoot;
            RequestedTheme = Settings.Instance.AppTheme;
        }
    }
}
