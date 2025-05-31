using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Translations.UserControls;

namespace DLSS_Swapper.UserControls;

public partial class DLLRecordInfoViewModel : ObservableObject
{
    public DLLRecordInfoViewModel()
    {

    }

    public DLLRecordInfoTranslationPropertiesViewModel TranslationProperties { get; } = new DLLRecordInfoTranslationPropertiesViewModel();
}
