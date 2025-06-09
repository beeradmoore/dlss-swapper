using CommunityToolkit.Mvvm.ComponentModel;

namespace DLSS_Swapper.UserControls;

public partial class DLLRecordInfoViewModel : ObservableObject
{
    public DLLRecordInfoViewModelTranslationProperties TranslationProperties { get; } = new DLLRecordInfoViewModelTranslationProperties();

    public DLLRecordInfoViewModel()
    {

    }
}
