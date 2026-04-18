using System;
using System.Text;

namespace DLSS_Swapper;

internal class ProxySettings
{
    bool _hasLoaded;
    public string Server { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    internal void LoadIfNeeded()
    {
        if (_hasLoaded == false)
        {
            // Only bother trying to load once.
            _hasLoaded = true;

            try
            {
                var vault = new Windows.Security.Credentials.PasswordVault();
                var proxySettings = vault.Retrieve("DLSS Swapper", "proxy");
                if (proxySettings is not null)
                {
                    proxySettings.RetrievePassword();
                    var proxyEncodedDetails = proxySettings.Password.Split('|');
                    if (proxyEncodedDetails.Length == 3)
                    {
                        Settings.ProxySettings.Server = Encoding.UTF8.GetString(Convert.FromBase64String(proxyEncodedDetails[0]));
                        Settings.ProxySettings.Username = Encoding.UTF8.GetString(Convert.FromBase64String(proxyEncodedDetails[1]));
                        Settings.ProxySettings.Password = Encoding.UTF8.GetString(Convert.FromBase64String(proxyEncodedDetails[2]));
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == -2147023728)
            {
                // NOOP
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }

    internal void SaveIfRequired(string? server, string? username, string? password)
    {
        try
        {
            var vault = new Windows.Security.Credentials.PasswordVault();

            if (server is not null)
            {
                // Save new proxy settings
                var proxyServer = Convert.ToBase64String(Encoding.UTF8.GetBytes(server));
                var proxyUsername = username is not null ? Convert.ToBase64String(Encoding.UTF8.GetBytes(username)) : string.Empty;
                var proxyPassword = password is not null ? Convert.ToBase64String(Encoding.UTF8.GetBytes(password)) : string.Empty;

                vault.Add(new Windows.Security.Credentials.PasswordCredential("DLSS Swapper", "proxy", $"{proxyServer}|{proxyUsername}|{proxyPassword}"));

                Settings.ProxySettings.Server = server;
                Settings.ProxySettings.Username = username ?? string.Empty;
                Settings.ProxySettings.Password = password ?? string.Empty;
            }
            else
            {
                // Try delete existing proxy settings
                var proxyCredentails = vault.Retrieve("DLSS Swapper", "proxy");
                if (proxyCredentails is not null)
                {
                    vault.Remove(proxyCredentails);
                }

                Settings.ProxySettings.Server = string.Empty;
                Settings.ProxySettings.Username = string.Empty;
                Settings.ProxySettings.Password = string.Empty;
            }

        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
}
