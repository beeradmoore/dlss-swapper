using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Helpers;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace DLSS_Swapper;

public partial class FailToLaunchWindowModel : ObservableObject
{
    public string SystemData { get; set; } = string.Empty;

    public FailToLaunchWindowModel()
    {
        var systemDetails = new SystemDetails();
        SystemData = systemDetails.GetSystemData();
    }

    [RelayCommand]
    void CopyText()
    {
        var package = new DataPackage();
        package.SetText(SystemData);
        Clipboard.SetContent(package);
    }

    #region TranslationProperties
    public string ApplicationFailToLaunchWindowText => ResourceHelper.GetString("ApplicationTitle") + " - " + ResourceHelper.GetString("FailedToLaunch");
    public string PleaseOpenIssuePartial1Text => ResourceHelper.GetString("PleaseOpenIssuePartial1");
    public string PleaseOpenIssuePartial2Text => ResourceHelper.GetString("PleaseOpenIssuePartial2");
    public string PleaseOpenIssuePartial3Text => ResourceHelper.GetString("PleaseOpenIssuePartial3");
    public string ClickToCopyDetailsText => ResourceHelper.GetString("ClickToCopyDetails");
    public string DlssSwapperFailedToLaunchText => ResourceHelper.GetString("DlssSwapperFailedToLaunch");
    #endregion
}
