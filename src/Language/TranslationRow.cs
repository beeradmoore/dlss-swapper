using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DLSS_Swapper.Language;

public partial class TranslationRow : ObservableObject
{
    public string Key { get; init; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SourceTranslation { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewTranslation { get; set; } = string.Empty;

    public TranslationRow(string key)
    {
        Key = key;
    }
}
