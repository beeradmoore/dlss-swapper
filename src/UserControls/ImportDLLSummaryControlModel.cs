using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLSS_Swapper.Data;

namespace DLSS_Swapper.UserControls;

class ImportDLLSummaryControlModel
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }

    public ImportDLLSummaryControlModelTranslationProperties TranslationProperties { get; } = new ImportDLLSummaryControlModelTranslationProperties();

    public ImportDLLSummaryControlModel(IReadOnlyList<DLLImportResult> dllImportResults)
    {
        SuccessCount = dllImportResults.Count(x => x.Success == true);
        FailedCount = dllImportResults.Count(x => x.Success == false);

        foreach (var dllImportResult in dllImportResults)
        {
            Logger.Verbose($"DLLImportResult: {dllImportResult.ToString()}");
        }
    }
}
