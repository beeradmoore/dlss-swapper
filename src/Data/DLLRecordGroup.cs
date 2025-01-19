using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data;

internal class DLLRecordGroup
{
    public string Name { get; init; }
    public ObservableCollection<DLLRecord> DLLRecords { get; init; }

    public DLLRecordGroup(string name, ObservableCollection<DLLRecord> dllRecords)
    {
        Name = name;
        DLLRecords = dllRecords;
    }
}
