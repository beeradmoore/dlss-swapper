using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper.Configuration.Attributes;

namespace DLSS_Swapper.Language;

public partial class TranslationRow : ObservableObject
{
    [Name("key")]
    [JsonPropertyName("Key")]
    public string Key { get; set; } = string.Empty;

    [Name("Comment")]
    [JsonPropertyName("Comment")]
    public string Comment { get; set; } = string.Empty;

    [Name("SourceTranslation")]
    [JsonPropertyName("SourceTranslation")]
    [ObservableProperty]
    public partial string SourceTranslation { get; set; } = string.Empty;

    [Name("NewTranslations")]
    [JsonPropertyName("NewTranslation")]
    [ObservableProperty]
    public partial string NewTranslation { get; set; } = string.Empty;

    public TranslationRow()
    {

    }

    public TranslationRow(string key)
    {
        Key = key;
    }
}
