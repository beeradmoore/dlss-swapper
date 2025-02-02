using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DLSS_Swapper.UserControls;

public class EasyContentDialog : ContentDialog
{
    public EasyContentDialog(XamlRoot xamlRoot) : base()
    {
        XamlRoot = xamlRoot;
        RequestedTheme = Settings.Instance.AppTheme;
    }
}
