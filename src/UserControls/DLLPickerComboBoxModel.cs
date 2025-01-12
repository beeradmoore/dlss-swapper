using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Data;

namespace DLSS_Swapper.UserControls;

public partial class DLLPickerComboBoxModel : ObservableObject
{
    [ObservableProperty]
    GameAssetType _gameAssetType = GameAssetType.Unknown;

    public Game Game { get; set; }

    [ObservableProperty]
    List<DLLRecord> _dllRecords;

    /*
    public ObservableCollection<DLLRecord> DLLRecords { get; set; }
    public Game Game { get; set; }
    public GameAssetType GameAssetType { get; set; }*/

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(Game))
        {
            UpdateLayoutIfReady();
        }
        else if (e.PropertyName == nameof(GameAssetType))
        {
            UpdateLayoutIfReady();
        }
    }

    void UpdateLayoutIfReady()
    {
        if (GameAssetType == GameAssetType.Unknown || Game is null)
        {
            return;
        }

        if (GameAssetType == GameAssetType.DLSS)
        {
            DllRecords = new List<DLLRecord>(DLLManager.Instance.DLSSRecords);

        }
        else if (GameAssetType == GameAssetType.DLSS_G)
        {
            DllRecords = new List<DLLRecord>(DLLManager.Instance.DLSSGRecords);
        }
        else if (GameAssetType == GameAssetType.DLSS_D)
        {
            DllRecords = new List<DLLRecord>(DLLManager.Instance.DLSSDRecords);
        }
        else if (GameAssetType == GameAssetType.FSR_31_DX12)
        {
            DllRecords = new List<DLLRecord>(DLLManager.Instance.FSR31DX12Records);
        }
        else if (GameAssetType == GameAssetType.FSR_31_VK)
        {
            DllRecords = new List<DLLRecord>(DLLManager.Instance.FSR31VKRecords);
        }
        else if (GameAssetType == GameAssetType.XeSS)
        {
            DllRecords = new List<DLLRecord>(DLLManager.Instance.XeSSRecords);
        }
        else if (GameAssetType == GameAssetType.XeLL)
        {
            DllRecords = new List<DLLRecord>(DLLManager.Instance.XeLLRecords);
        }
        else if (GameAssetType == GameAssetType.XeSS_FG)
        {
            DllRecords = new List<DLLRecord>(DLLManager.Instance.XeSSFGRecords);
        }
        else
        {
            throw new Exception($"Unknown GameAssetType: {GameAssetType}");
        }


    }
}
