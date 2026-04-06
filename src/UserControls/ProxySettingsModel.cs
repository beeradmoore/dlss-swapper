using CommunityToolkit.Mvvm.ComponentModel;

namespace DLSS_Swapper.UserControls;

public partial class ProxySettingsModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUseAuthentication))]
    public partial bool UseProxySettings { get; set; }

    [ObservableProperty]
    public partial string Server { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUseAuthentication))]
    public partial bool UseAuthentication { get; set; }

    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    public bool CanUseAuthentication => UseProxySettings && UseAuthentication;

#if PORTABLE
    public bool IsPortable { get; } = true;
#else
    public bool IsPortable { get; } = false;
#endif

    public ProxySettingsTranslationProperties TranslationProperties { get; set; } = new ProxySettingsTranslationProperties();

    public ProxySettingsModel()
    {
        Server = Settings.ProxySettings.Server;
        UseProxySettings = string.IsNullOrWhiteSpace(Server) == false;
        Username = Settings.ProxySettings.Username;
        Password = Settings.ProxySettings.Password;
        UseAuthentication = string.IsNullOrWhiteSpace(Username) == false && string.IsNullOrWhiteSpace(Password) == false;
    }
}
