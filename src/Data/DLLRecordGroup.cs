using System;
using System.Collections.ObjectModel;
using CommunityToolkit.WinUI.Collections;

namespace DLSS_Swapper.Data;

internal class DLLRecordGroup
{
    public string Name { get; init; }
    //public ObservableCollection<DLLRecord> DLLRecords { get; init; }
    public AdvancedCollectionView AdvancedDLLRecordsCollectionView { get; init; }

    public DLLRecordGroup(string name, ObservableCollection<DLLRecord> dllRecords)
    {
        Name = name;
        //DLLRecords = dllRecords;
        AdvancedDLLRecordsCollectionView = new AdvancedCollectionView(dllRecords, true)
        {
            Filter = PredicateForDebugDlls()
        };
    }

    private Predicate<object> PredicateForDebugDlls()
    {
        if (Settings.Instance.AllowDebugDlls == false)
        {
            return (obj) =>
            {
                if (obj is DLLRecord dllrecord)
                {
                    return (dllrecord.IsDevFile == false);
                }

                return false;
            };
        }

        return (obj) =>
        {
            return true;
        };
    }

}
