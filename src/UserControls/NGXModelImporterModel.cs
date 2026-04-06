using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Data.NVIDIA;

namespace DLSS_Swapper.UserControls;

public partial class NGXModelImporterModel : ObservableObject
{
    WeakReference<NGXModelImporter> _weakControl;

    public List<NGXModelRow> Models { get; }

    public NGXModelImporterModelTranslationProperties TranslationProperties { get; } = new NGXModelImporterModelTranslationProperties();

    public NGXModelImporterModel(NGXModelImporter control, List<NGXModel> models)
    {
        _weakControl = new WeakReference<NGXModelImporter>(control);

        Models = new List<NGXModelRow>(models.Count);

        foreach (var model in models.OrderByDescending(a => a.Version).ThenBy(a => a.GameAssetType))
        {
            Models.Add(new NGXModelRow(model));
        }
    }
}
