using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CsvHelper;
using CsvHelper.Configuration;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Language;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources.Core;

namespace DLSS_Swapper;

public partial class TranslationToolboxWindowModel : ObservableObject
{
    readonly WeakReference<TranslationToolboxWindow> _weakWindow;

    public List<KeyValuePair<string, string>> SourceLanguages { get; } = new List<KeyValuePair<string, string>>();
    public List<TranslationRow> TranslationRows { get; } = new List<TranslationRow>();

    readonly Dictionary<string, TranslationRow> _translationRowsDictionary = new Dictionary<string, TranslationRow>();

    [ObservableProperty]
    public partial KeyValuePair<string, string> SelectedSourceLanguage { get; set; }

    readonly ResourceMap _resourceMap = ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
    readonly ResourceContext _resourceContext = ResourceContext.GetForViewIndependentUse();

    public TranslationToolboxWindowModelTranslationProperties TranslationProperties => new TranslationToolboxWindowModelTranslationProperties();

    [ObservableProperty]
    public partial string TranslationProgressString { get; set; } = string.Empty;

    public TranslationToolboxWindowModel(TranslationToolboxWindow window)
    {
        _weakWindow = new WeakReference<TranslationToolboxWindow>(window);

        // Load all translation entries from the resource map.
        foreach (var resourceMapKey in _resourceMap.Keys)
        {
            var translationRow = new TranslationRow(resourceMapKey);
            TranslationRows.Add(translationRow);
            _translationRowsDictionary[resourceMapKey] = translationRow;
        }

        // Populate the dropdown for user to load an existing translation to translate from.
        var knownLanguages = LanguageManager.Instance.GetKnownLanguages();
        foreach (var knownLanguage in knownLanguages)
        {
            var languageName = LanguageManager.Instance.GetLanguageName(knownLanguage);
            SourceLanguages.Add(new KeyValuePair<string, string>(knownLanguage, languageName));
        }

        // Select this language based on the users language.
        SelectedSourceLanguage = SourceLanguages.FirstOrDefault(x => x.Key == "en-US");

        // Load the comments from the en-US resw file. That is the only thing this file does.
        // Maybe in future comments should be translated? But not today.
        var defaultResxFile = Path.Combine(AppContext.BaseDirectory, "Translations", "en-US", "Resources.resw");
        if (File.Exists(defaultResxFile))
        {
            try
            {
                var xDocument = System.Xml.Linq.XDocument.Load(defaultResxFile);

                foreach (var data in xDocument.Descendants("data"))
                {
                    var name = data.Attribute("name")?.Value;
                    var comment = data.Element("comment")?.Value;
                    if (string.IsNullOrWhiteSpace(name) == false && string.IsNullOrWhiteSpace(comment) == false)
                    {
                        if (_translationRowsDictionary.TryGetValue(name, out var translationRow) == true)
                        {
                            translationRow.Comment = comment;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Error($"Failed to load default resource file {defaultResxFile}: {err.Message}");
            }
        }

        RecalculateTranslationProgress();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SelectedSourceLanguage))
        {
            ReloadSourceLanguage();
        }
    }

    internal void RecalculateTranslationProgress()
    {
        // This should never happen, but will prevent problems if it does.
        if (TranslationRows.Count == 0)
        {
            TranslationProgressString = string.Empty;
            return;
        }

        var currentTranslated = 0;
        var totalToBeTranslated = TranslationRows.Count;

        foreach (var translationRow in TranslationRows)
        {
            if (string.IsNullOrWhiteSpace(translationRow.NewTranslation) == false)
            {
                ++currentTranslated;
            }
        }

        TranslationProgressString = $"{currentTranslated} / {totalToBeTranslated} ({(currentTranslated / (double)totalToBeTranslated):P0})";
    }

    void ReloadSourceLanguage()
    {
        if (string.IsNullOrWhiteSpace(SelectedSourceLanguage.Key))
        {
            return;
        }

        try
        {
            _resourceContext.Languages = new List<string> { SelectedSourceLanguage.Key };
        }
        catch (Exception err)
        {
            Logger.Error($"Could not load resource map for key {SelectedSourceLanguage.Key}: {err.Message}");
            return;
        }

        foreach (var resourceMapKey in _resourceMap.Keys)
        {
            if (_translationRowsDictionary.TryGetValue(resourceMapKey, out var translationRow))
            {
                var resourceCandidate = _resourceMap.GetValue(resourceMapKey, _resourceContext);
                if (string.IsNullOrWhiteSpace(resourceCandidate?.ValueAsString) == false)
                {
                    translationRow.SourceTranslation = resourceCandidate.ValueAsString;
                }
                else
                {
                    translationRow.SourceTranslation = string.Empty;
                }
            }
        }
    }

    [RelayCommand]
    void ReloadApp()
    {
        ResourceHelper.UpdateFromLiveTranslations(TranslationRows);
    }

    internal bool HasUnsavedChanges()
    {
        foreach (var translationRow in TranslationRows)
        {
            if (string.IsNullOrWhiteSpace(translationRow.NewTranslation) == false)
            {
                return true;
            }
        }

        return false;
    }

    [RelayCommand]
    async Task LoadAsync()
    {
        if (_weakWindow.TryGetTarget(out var window))
        {
            var shouldPromptOverwrite = HasUnsavedChanges();

            if (shouldPromptOverwrite)
            {
                var dialog = new EasyContentDialog(window.Content.XamlRoot)
                {
                    Title = ResourceHelper.GetString("TranslationToolboxPage_ResetProgressTitle"),
                    DefaultButton = ContentDialogButton.Primary,
                    Content = ResourceHelper.GetString("TranslationToolboxPage_ResetProgressMessage"),
                    PrimaryButtonText = ResourceHelper.GetString("TranslationToolboxPage_ResetProgressButton"),
                    CloseButtonText = ResourceHelper.GetString("General_Cancel"),
                };

                var result = await dialog.ShowAsync();
                if (result != ContentDialogResult.Primary)
                {
                    return;
                }
            }


            try
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var fileFilters = new List<FileSystemHelper.FileFilter>()
                {
                    new FileSystemHelper.FileFilter("JSON files", "*.json"),
                    new FileSystemHelper.FileFilter("CSV files", "*.csv")
                };

                var existingFile = FileSystemHelper.OpenFile(hWnd, fileFilters, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), defaultExtension: "json");

                // User cancelled.
                if (string.IsNullOrWhiteSpace(existingFile))
                {
                    return;
                }

                using (var stream = File.OpenRead(existingFile))
                {
                    if (stream is null)
                    {
                        throw new System.Exception("Could not open stream for the selected path.");
                    }


                    if (existingFile.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                            {
                                var loadedTranslationRows = csv.GetRecords<TranslationRow>().ToList();
                                var loadedDictionary = new Dictionary<string, string>(loadedTranslationRows.Count);
                                foreach (var loadedTranslationRow in loadedTranslationRows)
                                {
                                    if (string.IsNullOrWhiteSpace(loadedTranslationRow.NewTranslation) == false)
                                    {
                                        loadedDictionary[loadedTranslationRow.Key] = loadedTranslationRow.NewTranslation;
                                    }
                                }
                                foreach (var translationRow in TranslationRows)
                                {
                                    if (loadedDictionary.TryGetValue(translationRow.Key, out var translation))
                                    {
                                        translationRow.NewTranslation = translation;
                                    }
                                    else
                                    {
                                        translationRow.NewTranslation = string.Empty;
                                    }
                                }
                            }
                        }
                    }
                    else if ( existingFile.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        var loadedDictionary = await JsonSerializer.DeserializeAsync(stream, SourceGenerationContext.Default.DictionaryStringString);
                        if (loadedDictionary is null)
                        {
                            var dialog = new EasyContentDialog(window.Content.XamlRoot)
                            {
                                Title = ResourceHelper.GetString("General_Error"),
                                DefaultButton = ContentDialogButton.Close,
                                Content = ResourceHelper.GetString("TranslationToolboxPage_NotValidTranslationFile"),
                                CloseButtonText = ResourceHelper.GetString("General_Close"),
                            };
                            await dialog.ShowAsync();
                            return;
                        }

                        foreach (var translationRow in TranslationRows)
                        {
                            if (loadedDictionary.TryGetValue(translationRow.Key, out var translation))
                            {
                                translationRow.NewTranslation = translation;
                            }
                            else
                            {
                                translationRow.NewTranslation = string.Empty;
                            }
                        }
                    }

                    
                    RecalculateTranslationProgress();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

                var dialog = new EasyContentDialog(window.Content.XamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Error"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("TranslationToolboxPage_LoadFailedMessage"),
                    CloseButtonText = ResourceHelper.GetString("General_Close"),
                };
                await dialog.ShowAsync();
            }
        }

    }

    [RelayCommand]
    async Task SaveAsync()
    {
        if (_weakWindow.TryGetTarget(out var window))
        {
            var outputData = new Dictionary<string, string>();
            foreach (var translation in TranslationRows)
            {
                if (string.IsNullOrWhiteSpace(translation.NewTranslation) == false)
                {
                    outputData[translation.Key] = translation.NewTranslation;
                }
            }

            if (outputData.Count == 0)
            {
                var dialog = new EasyContentDialog(window.Content.XamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Error"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("TranslationToolboxPage_NoDataToSave"),
                    CloseButtonText = ResourceHelper.GetString("General_Close"),
                };
                await dialog.ShowAsync();
                return;
            }

            try
            {
                var fileFilters = new List<FileSystemHelper.FileFilter>()
                {
                    new FileSystemHelper.FileFilter("JSON files", ".json"),
                    new FileSystemHelper.FileFilter("CSV files", ".csv"),
                };
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var outputPath = FileSystemHelper.SaveFile(hWnd, fileFilters, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "dlss_swapper_translation.json", "json");

                // User cancelled.
                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    return;
                }

                using (var fileStream = File.Create(outputPath))
                {
                    if (fileStream is null)
                    {
                        throw new InvalidOperationException("Could not create fileStream for the selected path.");
                    }

                    if (outputPath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var streamWriter = new StreamWriter(fileStream, System.Text.Encoding.UTF8))
                        {
                            using (var csv = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                            {
                                var translationRows = new List<TranslationRow>(TranslationRows.Count);
                                foreach (var originalTranslationRow in TranslationRows)
                                {
                                    var translationRow = new TranslationRow()
                                    {
                                        Key = originalTranslationRow.Key,
                                        Comment = originalTranslationRow.Comment,
                                        SourceTranslation = originalTranslationRow.SourceTranslation,
                                        NewTranslation = originalTranslationRow.NewTranslation,
                                    };
                                    translationRows.Add(translationRow);
                                }
                                csv.WriteRecords(translationRows);
                            }
                        }
                    }
                    else if (outputPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        await JsonSerializer.SerializeAsync(fileStream, outputData, SourceGenerationContext.Default.DictionaryStringString);

                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

                var dialog = new EasyContentDialog(window.Content.XamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Error"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("TranslationToolboxPage_SaveFailedMessage"),
                    CloseButtonText = ResourceHelper.GetString("General_Close"),
                };
                await dialog.ShowAsync();
            }
        }
    }

    [RelayCommand]
    async Task LoadExistingTranslationAsync()
    {
        if (_weakWindow.TryGetTarget(out var window))
        {
            var shouldPromptOverwrite = HasUnsavedChanges();

            if (shouldPromptOverwrite)
            {
                var dialog = new EasyContentDialog(window.Content.XamlRoot)
                {
                    Title = ResourceHelper.GetString("TranslationToolboxPage_ResetProgressTitle"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("TranslationToolboxPage_ResetProgressMessage"),
                    PrimaryButtonText = ResourceHelper.GetString("TranslationToolboxPage_ResetProgressButton"),
                    CloseButtonText = ResourceHelper.GetString("General_Cancel"),
                };

                var result = await dialog.ShowAsync();
                if (result != ContentDialogResult.Primary)
                {
                    return;
                }
            }

            var sourceLangauges = new List<KeyValuePair<string, string>>(SourceLanguages);


#if DEBUG
            // Remove LANG_HUNT
            var indexToRemove = -1;
            for (var i = 0; i < sourceLangauges.Count; ++i)
            {
                if (sourceLangauges[i].Key == "LANG_HUNT")
                {
                    indexToRemove = i;
                    break;
                }
            }

            if (indexToRemove >= 0)
            {
                sourceLangauges.RemoveAt(indexToRemove);
            }
#endif

            var comboBox = new ComboBox()
            {
                ItemsSource = sourceLangauges,
                DisplayMemberPath = "Value",
                SelectedItem = sourceLangauges[0],
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
            };

            var loadExistingDialog = new EasyContentDialog(window.Content.XamlRoot)
            {
                Title = ResourceHelper.GetString("TranslationToolboxPage_SelectLanguageToLoad"),
                DefaultButton = ContentDialogButton.Primary,
                PrimaryButtonText = ResourceHelper.GetString("General_Load"),
                Content = comboBox,
                CloseButtonText = ResourceHelper.GetString("General_Cancel"),
            };
            var loadExistingDialogResult = await loadExistingDialog.ShowAsync();
            if (loadExistingDialogResult == ContentDialogResult.None)
            {
                return;
            }

            if (comboBox.SelectedItem is KeyValuePair<string, string> selectedLanguage)
            {
                var resourceContext = ResourceContext.GetForViewIndependentUse();
                try
                {
                    resourceContext.Languages = new List<string> { selectedLanguage.Key };
                }
                catch (Exception err)
                {
                    Logger.Error($"Could not load resource map for key {selectedLanguage.Key}: {err.Message}");
                    return;
                }

                foreach (var resourceMapKey in _resourceMap.Keys)
                {
                    if (_translationRowsDictionary.TryGetValue(resourceMapKey, out var translationRow))
                    {
                        var resourceCandidate = _resourceMap.GetValue(resourceMapKey, resourceContext);

                        translationRow.NewTranslation = string.Empty;

                        // Make sure there is a value before we start caring about it.
                        if (string.IsNullOrWhiteSpace(resourceCandidate?.ValueAsString) == false)
                        {
                            if (resourceCandidate.Qualifiers.Count == 0)
                            {
                                // this should never happen
                            }
                            else
                            {
                                // Special case to allow en-US translations to be loaded.
                                if (selectedLanguage.Key == "en-US")
                                {
                                    translationRow.NewTranslation = resourceCandidate.ValueAsString;
                                }
                                else
                                {
                                    // This should always just be 1 item, not more than 1, maybe?
                                    var qualifier = resourceCandidate.Qualifiers.First();

                                    // If the qualifier has a value of en-US, then we don't want to use it.
                                    if (qualifier.QualifierValue.Equals("EN-US", StringComparison.InvariantCultureIgnoreCase) == false)
                                    {
                                        translationRow.NewTranslation = resourceCandidate.ValueAsString;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            RecalculateTranslationProgress();
        }
    }

    [RelayCommand]
    async Task PublishAsync()
    {
        if (_weakWindow.TryGetTarget(out var window))
        {
            var outputData = new Dictionary<string, string>();
            foreach (var translation in TranslationRows)
            {
                if (string.IsNullOrWhiteSpace(translation.NewTranslation) == false)
                {
                    outputData[translation.Key] = translation.NewTranslation;
                }
            }

            if (outputData.Count == 0)
            {
                var dialog = new EasyContentDialog(window.Content.XamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Error"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("TranslationToolboxPage_NoDataToPublish"),
                    CloseButtonText = ResourceHelper.GetString("General_Close"),
                };
                await dialog.ShowAsync();
                return;
            }

            try
            {
                var fileFilters = new List<FileSystemHelper.FileFilter>()
                {
                    new FileSystemHelper.FileFilter("ZIP files", ".zip"),
                };
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var outputPath = FileSystemHelper.SaveFile(hWnd, fileFilters, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "dlss_swapper_published_translation.zip", "zip");

                // User cancelled.
                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    return;
                }

                using (var fileStream = File.Create(outputPath))
                {
                    using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Update, true))
                    {
                        var entry = zipArchive.CreateEntry("Resources.resw", CompressionLevel.Optimal);
                        using (var entryStream = entry.Open())
                        {
                            if (entryStream is null)
                            {
                                throw new Exception("Could not create exported zip.");
                            }

                            var template = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <!--
    Microsoft ResX Schema

    Version 2.0

    The primary goals of this format is to allow a simple XML format
    that is mostly human readable. The generation and parsing of the
    various data types are done through the TypeConverter classes
    associated with the data types.

    Example:

    ... ado.net/XML headers & schema ...
    <resheader name=""resmimetype"">text/microsoft-resx</resheader>
    <resheader name=""version"">2.0</resheader>
    <resheader name=""reader"">System.Resources.ResXResourceReader, System.Windows.Forms, ...</resheader>
    <resheader name=""writer"">System.Resources.ResXResourceWriter, System.Windows.Forms, ...</resheader>
    <data name=""Name1""><value>this is my long string</value><comment>this is a comment</comment></data>
    <data name=""Color1"" type=""System.Drawing.Color, System.Drawing"">Blue</data>
    <data name=""Bitmap1"" mimetype=""application/x-microsoft.net.object.binary.base64"">
        <value>[base64 mime encoded serialized .NET Framework object]</value>
    </data>
    <data name=""Icon1"" type=""System.Drawing.Icon, System.Drawing"" mimetype=""application/x-microsoft.net.object.bytearray.base64"">
        <value>[base64 mime encoded string representing a byte array form of the .NET Framework object]</value>
        <comment>This is a comment</comment>
    </data>

    There are any number of ""resheader"" rows that contain simple
    name/value pairs.

    Each data row contains a name, and value. The row also contains a
    type or mimetype. Type corresponds to a .NET class that support
    text/value conversion through the TypeConverter architecture.
    Classes that don't support this are serialized and stored with the
    mimetype set.

    The mimetype is used for serialized objects, and tells the
    ResXResourceReader how to depersist the object. This is currently not
    extensible. For a given mimetype the value must be set accordingly:

    Note - application/x-microsoft.net.object.binary.base64 is the format
    that the ResXResourceWriter will generate, however the reader can
    read any of the formats listed below.

    mimetype: application/x-microsoft.net.object.binary.base64
    value   : The object must be serialized with
            : System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
            : and then encoded with base64 encoding.

    mimetype: application/x-microsoft.net.object.soap.base64
    value   : The object must be serialized with
            : System.Runtime.Serialization.Formatters.Soap.SoapFormatter
            : and then encoded with base64 encoding.

    mimetype: application/x-microsoft.net.object.bytearray.base64
    value   : The object must be serialized into a byte array
            : using a System.ComponentModel.TypeConverter
            : and then encoded with base64 encoding.
    -->
  <xsd:schema id=""root"" xmlns="""" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
    <xsd:import namespace=""http://www.w3.org/XML/1998/namespace"" />
    <xsd:element name=""root"" msdata:IsDataSet=""true"">
      <xsd:complexType>
        <xsd:choice maxOccurs=""unbounded"">
          <xsd:element name=""metadata"">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" />
              </xsd:sequence>
              <xsd:attribute name=""name"" use=""required"" type=""xsd:string"" />
              <xsd:attribute name=""type"" type=""xsd:string"" />
              <xsd:attribute name=""mimetype"" type=""xsd:string"" />
              <xsd:attribute ref=""xml:space"" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name=""assembly"">
            <xsd:complexType>
              <xsd:attribute name=""alias"" type=""xsd:string"" />
              <xsd:attribute name=""name"" type=""xsd:string"" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name=""data"">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""1"" />
                <xsd:element name=""comment"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""2"" />
              </xsd:sequence>
              <xsd:attribute name=""name"" type=""xsd:string"" use=""required"" msdata:Ordinal=""1"" />
              <xsd:attribute name=""type"" type=""xsd:string"" msdata:Ordinal=""3"" />
              <xsd:attribute name=""mimetype"" type=""xsd:string"" msdata:Ordinal=""4"" />
              <xsd:attribute ref=""xml:space"" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name=""resheader"">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""1"" />
              </xsd:sequence>
              <xsd:attribute name=""name"" type=""xsd:string"" use=""required"" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <resheader name=""resmimetype"">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name=""version"">
    <value>2.0</value>
  </resheader>
  <resheader name=""reader"">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name=""writer"">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
</root>";
                            var doc = XDocument.Parse(template);

                            if (doc.Root is not null)
                            {
                                foreach (var translationRow in TranslationRows)
                                {
                                    if (string.IsNullOrWhiteSpace(translationRow.NewTranslation) == false)
                                    {
                                        var newNode = new XElement("data",
                                            new XAttribute("name", translationRow.Key),
                                            new XAttribute(XNamespace.Xml + "space", "preserve"),
                                            new XElement("value", translationRow.NewTranslation)
                                        );
                                        doc.Root.Add(newNode);

                                    }
                                }
                            }

                            doc.Save(entryStream);
                        }
                    }
                }

                var filename = Path.GetFileName(outputPath);

                var dialog = new EasyContentDialog(window.Content.XamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Success"),
                    DefaultButton = ContentDialogButton.Primary,
                    PrimaryButtonCommand = OpenTranslationsGuideCommand,
                    PrimaryButtonText = ResourceHelper.GetString("TranslationToolboxPage_TranslationGuideButton"),
                    Content = ResourceHelper.GetFormattedResourceTemplate("TranslationToolboxPage_TranslationGuideMessage", filename),
                    CloseButtonText = ResourceHelper.GetString("General_Close"),
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

                var dialog = new EasyContentDialog(window.Content.XamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Error"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("TranslationToolboxPage_PublishFailedMessage"),
                    CloseButtonText = ResourceHelper.GetString("General_Close"),
                };
                await dialog.ShowAsync();
            }
        }
    }

    [RelayCommand]
    async Task OpenTranslationsGuideAsync()
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/beeradmoore/dlss-swapper/wiki/Translation-Guide"));
    }
}
