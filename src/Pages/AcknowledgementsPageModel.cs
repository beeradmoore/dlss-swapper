using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Controls;
using DLSS_Swapper.Acknowledgements;
using DLSS_Swapper.Converters;
using Microsoft.UI.Xaml;

namespace DLSS_Swapper.Pages;

public partial class AcknowledgementsPageModel : ObservableObject
{
    public const string AcknowledgementsPrefix = "DLSS_Swapper.Acknowledgements.";
    readonly WeakReference<AcknowledgementsPage> _weakPage;

    public List<Acknowledgement> Acknowlegements { get; } = new List<Acknowledgement>();

    [ObservableProperty]
    public partial Acknowledgement? SelectedItem { get; set; }

    public MarkdownConfig MarkdownConfig { get; set; } = new MarkdownConfig();

    public AcknowledgementsPageModelTranslationProperties TranslationProperties { get; } = new AcknowledgementsPageModelTranslationProperties();

    public AcknowledgementsPageModel(AcknowledgementsPage page)
    {
        _weakPage = new WeakReference<AcknowledgementsPage>(page);

        var nameOrder = new string[]
        {
            "You",
            "DLSS",
            "Streamline",
            "FidelityFX-SDK",
            "XeSS"
        };

        var acknowlegements = new List<Acknowledgement>();

        var regex = new Regex(@"^(?<name>.*)\.(?<file>license\.txt|notes\.md)$");
        var acknowlegementResourceNames = GetType().Assembly.GetManifestResourceNames().Where(x => x.StartsWith(AcknowledgementsPrefix, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var acknowlegementResourceName in acknowlegementResourceNames)
        {
            var acknowlegementName = acknowlegementResourceName.Substring(AcknowledgementsPrefix.Length);
            var match = regex.Match(acknowlegementName);
            if (match.Success)
            {

                var name = match.Groups["name"].Value;

                if (name == "FidelityFX_SDK")
                {
                    name = "FidelityFX-SDK";
                }
                else if (name == "SQLite_net")
                {
                    name = "SQLite-net";
                }

                var file = match.Groups["file"].Value;
                var acknowlegement = acknowlegements.FirstOrDefault(x => x.Name == name);
                if (acknowlegement is null)
                {
                    acknowlegement = new Acknowledgement(name);
                    acknowlegements.Add(acknowlegement);
                }

                if (file == "notes.md")
                {
                    acknowlegement.NotesResourceName = acknowlegementResourceName;
                }
                else if (file == "license.txt")
                {
                    acknowlegement.LicenseResourceName = acknowlegementResourceName;
                }
            }
        }

        acknowlegements.Sort();

        foreach (var name in nameOrder)
        {
            var index = acknowlegements.FindIndex(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (index > -1)
            {
                Acknowlegements.Add(acknowlegements[index]);
                acknowlegements.RemoveAt(index);
            }
        }

        foreach (var acknowlegement in acknowlegements)
        {
            Acknowlegements.Add(acknowlegement);
        }

        SelectedItem = Acknowlegements.FirstOrDefault();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SelectedItem))
        {
            if (SelectedItem is not null)
            {
                SelectedItem.LoadResource();
            }
        }
    }
}
