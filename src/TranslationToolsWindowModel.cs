using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Language;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources.Core;
using Windows.Storage.Pickers;

namespace DLSS_Swapper;

public partial class TranslationToolsWindowModel : ObservableObject
{
    readonly WeakReference<TranslationToolsWindow> _weakWindow;

    public List<KeyValuePair<string, string>> SourceLanguages { get; } = new List<KeyValuePair<string, string>>();
    public List<TranslationRow> TranslationRows { get; } = new List<TranslationRow>();

    readonly Dictionary<string, TranslationRow> _translationRowsDictionary = new Dictionary<string, TranslationRow>();

    [ObservableProperty]
    public partial KeyValuePair<string, string> SelectedSourceLanguage { get; set; }
    
    readonly ResourceMap _resourceMap = ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
    readonly ResourceContext _resourceContext = ResourceContext.GetForViewIndependentUse();

    public TranslationToolsWindowModelTranslationProperties TranslationProperties => new TranslationToolsWindowModelTranslationProperties();

    public TranslationToolsWindowModel(TranslationToolsWindow window)
    {
        _weakWindow = new WeakReference<TranslationToolsWindow>(window);

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
        SelectedSourceLanguage = SourceLanguages.FirstOrDefault(x => x.Key == Settings.Instance.Language);

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
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SelectedSourceLanguage))
        {
            ReloadSourceLanguage();
        }
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

    [RelayCommand]
    async Task LoadAsync()
    {
        if (_weakWindow.TryGetTarget(out var window))
        {
            var shouldPromptOverwrite = false;
            foreach (var translationRow in TranslationRows)
            {
                if (string.IsNullOrWhiteSpace(translationRow.NewTranslation) == false)
                {
                    shouldPromptOverwrite = true;
                    break;
                }
            }

            if (shouldPromptOverwrite)
            {
                var dialog = new EasyContentDialog(window.Content.XamlRoot)
                {
                    Title = ResourceHelper.GetString("TranslationTools_ResetProgressTitle"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("TranslationTools_ResetProgressMessage"),
                    PrimaryButtonText = ResourceHelper.GetString("TranslationTools_ResetProgressButton"),
                    CloseButtonText = ResourceHelper.GetString("Cancel"),
                };

                var result = await dialog.ShowAsync();
                if (result != ContentDialogResult.Primary)
                {
                    return;
                }
            }

           
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var fileOpenPicker = new FileOpenPicker()
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                };
                fileOpenPicker.FileTypeFilter.Add(".json");
                WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, hwnd);

                var existingFile = await fileOpenPicker.PickSingleFileAsync();

                // User cancelled.
                if (existingFile is null)
                {
                    return;
                }

                using (var stream = await existingFile.OpenStreamForReadAsync())
                {
                    if (stream is null)
                    {
                        throw new System.Exception("Could not open stream for the selected path.");
                    }
                    var loadedDictionary = await JsonSerializer.DeserializeAsync(stream, SourceGenerationContext.Default.DictionaryStringString);
                    if (loadedDictionary is null)
                    {
                        var dialog = new EasyContentDialog(window.Content.XamlRoot)
                        {
                            Title = ResourceHelper.GetString("Error"),
                            DefaultButton = ContentDialogButton.Close,
                            Content = ResourceHelper.GetString("TranslationTools_NotValidTranslationFile"),
                            CloseButtonText = ResourceHelper.GetString("Close"),
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
                            translationRow.NewTranslation = string.Empty; ;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

                var dialog = new EasyContentDialog(window.Content.XamlRoot)
                {
                    Title = ResourceHelper.GetString("Error"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("TranslationTools_LoadFailedMessage"),
                    CloseButtonText = ResourceHelper.GetString("Close"),
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
                    Title = ResourceHelper.GetString("Error"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("TranslationTools_NoDataToSave"),
                    CloseButtonText = ResourceHelper.GetString("Close"),
                };
                await dialog.ShowAsync();
                return;
            }

            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("json", new List<string>() { ".json" });
                savePicker.SuggestedFileName = "dlss_swapper_translation.json";
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
                var saveFile = await savePicker.PickSaveFileAsync();

                // User cancelled.
                if (saveFile is null)
                {
                    return;
                }

                var outputPath = saveFile.Path;

                using (var fileStream = File.Create(outputPath))
                {
                    if (fileStream is null)
                    {
                        throw new InvalidOperationException("Could not create fileStream for the selected path.");
                    }
                    await JsonSerializer.SerializeAsync(fileStream, outputData, SourceGenerationContext.Default.DictionaryStringString);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

                var dialog = new EasyContentDialog(window.Content.XamlRoot)
                {
                    Title = ResourceHelper.GetString("Error"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("TranslationTools_SaveFailedMessage"),
                    CloseButtonText = ResourceHelper.GetString("Close"),
                };
                await dialog.ShowAsync();
            }
        }
    }

    [RelayCommand]
    async Task ImportAsTranslationAsync()
    {
        if (_weakWindow.TryGetTarget(out var window))
        {
            var shouldPromptOverwrite = false;
            foreach (var translationRow in TranslationRows)
            {
                if (string.IsNullOrWhiteSpace(translationRow.NewTranslation) == false)
                {
                    shouldPromptOverwrite = true;
                    break;
                }
            }

            if (shouldPromptOverwrite)
            {
                var dialog = new EasyContentDialog(window.Content.XamlRoot)
                {
                    Title = ResourceHelper.GetString("TranslationTools_ResetProgressTitle"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("TranslationTools_ResetProgressMessage"),
                    PrimaryButtonText = ResourceHelper.GetString("TranslationTools_ResetProgressButton"),
                    CloseButtonText = ResourceHelper.GetString("Cancel"),
                };

                var result = await dialog.ShowAsync();
                if (result != ContentDialogResult.Primary)
                {
                    return;
                }
            }

            foreach (var translationRow in TranslationRows)
            {
                translationRow.NewTranslation = translationRow.SourceTranslation;
            }
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
                    Title = ResourceHelper.GetString("Error"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("TranslationTools_NoDataToPublish"),
                    CloseButtonText = ResourceHelper.GetString("Close"),
                };
                await dialog.ShowAsync();
                return;
            }

            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("zip", new List<string>() { ".zip" });
                savePicker.SuggestedFileName = "dlss_swapper_published_translation.zip";
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
                var saveFile = await savePicker.PickSaveFileAsync();

                // User cancelled.
                if (saveFile is null)
                {
                    return;
                }

                var outputPath = saveFile.Path;

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

                            using (var streamWriter = new StreamWriter(entryStream))
                            {
                                streamWriter.WriteLine(@"""<?xml version=""1.0"" encoding=""utf-8""?>
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
  </resheader>""");
                                foreach (var translationRow in TranslationRows)
                                {
                                    if (string.IsNullOrWhiteSpace(translationRow.NewTranslation) == false)
                                    {
                                        streamWriter.WriteLine(@$"""  <data name=""{translationRow.Key}"" xml:space=""preserve"">
    <value>{translationRow.NewTranslation}</value>
  </data>""");
                                    }
                                }

                                streamWriter.WriteLine(" </root>");
                                streamWriter.Flush();
                            }
                        }
                    }
                }

                var filename = Path.GetFileName(outputPath);

                var dialog = new EasyContentDialog(window.Content.XamlRoot)
                {
                    Title = ResourceHelper.GetString("Success"),
                    DefaultButton = ContentDialogButton.Primary,
                    PrimaryButtonCommand = OpenTranslationsGuideCommand,
                    PrimaryButtonText = ResourceHelper.GetString("TranslationTools_TranslationGuideButton"),
                    Content = string.Format(CultureInfo.InvariantCulture, ResourceHelper.GetString("TranslationTools_TranslationGuideMessage"), filename),
                    CloseButtonText = ResourceHelper.GetString("Close"),
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

                var dialog = new EasyContentDialog(window.Content.XamlRoot)
                {
                    Title = ResourceHelper.GetString("Error"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("TranslationTools_PublishFailedMessage"),
                    CloseButtonText = ResourceHelper.GetString("Close"),
                };
                await dialog.ShowAsync();
            }
        }
    }

    [RelayCommand]
    async Task OpenTranslationsGuideAsync()
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/beeradmoore/dlss-swapper/wiki/Translations"));
    }
}
