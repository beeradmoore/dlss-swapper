using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DLSS_Swapper.Acknowledgements;

public partial class Acknowledgement : ObservableObject
{
    public string Name { get; init; }

    public string? NotesResourceName { get; set; }

    [ObservableProperty]
    public partial string? Notes { get; set; }

    public string? LicenseResourceName { get; set; }

    [ObservableProperty]
    public partial string? License { get; set; }

    public Acknowledgement(string name)
    {
        Name = name;
    }

    public override string ToString()
    {
        return Name;
    }

    public void LoadResource()
    {
        if (NotesResourceName is not null)
        {
            using (var stream = GetType().Assembly.GetManifestResourceStream(NotesResourceName))
            {
                if (stream is not null)
                {
                    using (var streamReader = new StreamReader(stream))
                    {
                        Notes = streamReader.ReadToEnd();
                    }
                }
            }
        }

        if (LicenseResourceName is not null)
        {
            using (var stream = GetType().Assembly.GetManifestResourceStream(LicenseResourceName))
            {
                if (stream is not null)
                {
                    using (var streamReader = new StreamReader(stream))
                    {
                        License = streamReader.ReadToEnd();
                    }
                }
            }
        }
    }
}
