using System.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data;

internal class GameLibrarySelector : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public IGameLibrary GameLibrary { get; init; }
    public string Name => GameLibrary.Name;
    public string OffContentLabel => ResourceHelper.GetFormattedResourceTemplate("SettingsPage_LibraryDisabled", GameLibrary.Name);
    public string OnContentLabel => ResourceHelper.GetFormattedResourceTemplate("SettingsPage_LibraryEnabled", GameLibrary.Name);

    public bool IsEnabled
    {
        get
        {
            return GameLibrary.IsEnabled;
        }
        set
        {
            if (value == GameLibrary.IsEnabled)
            {
                return;
            }

            if (value == true)
            {
                GameLibrary.Enable();
            }
            else
            {
                GameLibrary.Disable();
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
            WeakReferenceMessenger.Default.Send(new Messages.GameLibrariesStateChangedMessage());
        }
    }

    public GameLibrarySelector(IGameLibrary gameLibrary)
    {
        GameLibrary = gameLibrary;
    }

    internal void ReloadLabels()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OffContentLabel)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OnContentLabel)));
    }
}
