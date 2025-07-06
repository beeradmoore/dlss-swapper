using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.UserControls;

public class GameHistoryControlModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string EventTimeHeader => ResourceHelper.GetString("GameHistoryControl_EventTimeHeader");

    [TranslationProperty]
    public string EventTypeHeader => ResourceHelper.GetString("GameHistoryControl_EventTypeHeader");

    [TranslationProperty]
    public string AssetTypeHeader => ResourceHelper.GetString("GameHistoryControl_AssetTypeHeader");

    [TranslationProperty]
    public string VersionHeader => ResourceHelper.GetString("GameHistoryControl_VersionHeader");

    [TranslationProperty]
    public string NoHistoryText => ResourceHelper.GetString("GameHistoryControl_NoHistoryLabel");
    
}
