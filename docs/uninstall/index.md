## Uninstall DLSS Swapper Beta
Versions of DLSS Swapper before v1.0 could be installed from the Microsoft Store or by installing from GitHub (which also involved installing and trusting our custom certificates). As of v1.0 the app is no longer available on Microsoft Store, nor does it require installing custom certificates.

These instructions are specifically for people to remove the Microsoft Store version of the game and/or our custom certificates.

1. Open apps & features by opening `Settings` > `Apps` > `Apps & features` (or you can try click [here](ms-settings:appsfeatures))

2. Search `DLSS Swapper`.

![Apps and features screen showing search results of dlss-swapper](https://beeradmoore.github.io/dlss-swapper/images/uninstall/uninstall_settings_1.png)

3. Expand `DLSS Swapper` and click `Uninstall`

![DLSS Swapper uninstall button visible by expanding the search result in apps and features](https://beeradmoore.github.io/dlss-swapper/images/uninstall/uninstall_settings_2.png)


## Additional instructions if you installed via GitHub
If you have only ever installed via the Microsoft Store this section does not apply to you. This is only for those who originally installed from GitHub which also required that you install a developer certificate.

1. Open `Manage user certificates`

![Start menu showing manage user certificates app](https://beeradmoore.github.io/dlss-swapper/images/uninstall/manage_user_certificates.png)


2. Expand `Trusted People` and select `Certificates` to see `dlss-swapper`

![Start menu showing manage user certificates app](https://beeradmoore.github.io/dlss-swapper/images/uninstall/manage_user_certificates_certs.png)

If you don't see `Certificates` option or `dlss-swapper` then it is possible you either:
- Installed certificate into a different certificate store when you followed the (https://beeradmoore.github.io/dlss-swapper/install/)[install instructions]
- Possibly have already removed the certificate. You can use `Action` -> `Find Certificates...` and enter `dlss-swapper` into the `Contains` field to try search for it.

3. Right mouse click the `dlss-swapper` certificate and select `Delete`. 

4. Select `Yes` when prompted to permanently delete.

![Prompt showing user the option to permanently delete the dlss-swapper certificate](https://beeradmoore.github.io/dlss-swapper/images/uninstall/manage_user_certificates_delete.png)
