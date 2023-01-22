## Recommended Install Instructions

> **Note:**
> DLSS Swapper is currently unavailable on the Microsoft Store. It will hopefully be restored soon. If you'd like to know more please see this [comment](https://github.com/beeradmoore/dlss-swapper/issues/96#issuecomment-1320977249).



~~DLSS Swapper is now on the Microsoft Store!~~ You can install builds directly from GitHub using the manual method below, however they will not automatically update when a new version is available (although you will still be notified in app about updates).

<!--
<a href="https://www.microsoft.com/store/apps/9NNL4H1PTJBL" target="_blank"><img src="https://beeradmoore.github.io/dlss-swapper/images/microsoft_store_badge.png" alt="Microsoft Store install link" width="300" /></a>-->

## Manual Install Instructions
To install a GitHub build of DLSS Swapper you will need to trust our signing certificate and download installer from Github, or build the application from source.

### Trusting Signing Certificate
Downloading, installing, and trusting the signing certificate only needs to be done once. After this you can install and update DLSS Swapper any time and as often as you want.

_NOTE: Only install and trust certificates from sources you trust_

1 - Download our [signing certificate](https://beeradmoore.github.io/dlss-swapper/downloads/dlss-swapper.cer). 

2 - Double click the `dlss-swapper.cer` file and at the properties prompt select `Install Certificate...`

![Windows certificate summary showing that this certificate is currently not trusted](https://beeradmoore.github.io/dlss-swapper/images/install/certificate_1.png)

3 - Change `Store Location` to `Local Machine` and click `Next`

![Certificate Import Wizard asking user what store location they want to us eto install the current certificate. Options being current user and local machine](https://beeradmoore.github.io/dlss-swapper/images/install/certificate_2.png)

4 - Select the option for `Place all certificates in the following store` and then click `Browse...`

![Certificate Import Wizard asking user if they want to import the certificate into a certificate store based on what type of certificate it is or if you want to install them to a specific certificate store](https://beeradmoore.github.io/dlss-swapper/images/install/certificate_3.png)

5 - In the `Select Certificate Store` popup select `Trusted People` and then click `OK`. This will take you back to the screen displayed in step 5, click `Next`.

![Select Certificate Store window showing Trusted People is selected](https://beeradmoore.github.io/dlss-swapper/images/install/certificate_4.png)

6 - Your final certifcate import summary should look like this. Select `Finish` to install the certificate.

![Certificate Import Wizard showing the certificate store is selected by the user, is Trusted People, and Content is Certificate](https://beeradmoore.github.io/dlss-swapper/images/install/certificate_5.png)


### Downloading Installer
Builds will be uploaded to the [releases section](https://github.com/beeradmoore/dlss-swapper/releases) in our Github repository. Download `DLSS-Swapper.appinstaller` for the latest release.

### Running The Installer
If all goes as planned running `DLSS-Swapper.appinstaller` should be all you need to do. This will install any additional dependencies you require to run DLSS Swapper.

![Animated gif showing the user click install and then seeing the installing progress bar go from 0-100% and give the option for the user to then launch the application](https://beeradmoore.github.io/dlss-swapper/images/install/installer_1.gif)

If your installer says `Untrusted App` please review and re-attempt the steps from `Trusting Signing Certificate` section above.

![Installer showing an error that it is not trusted](https://beeradmoore.github.io/dlss-swapper/images/install/installer_2.png)

If you have any problems installing please try install each of the `dependency-xxx.msix` files on the associated with that release. Then check if someone has already reported a similar problem in the [issues section](https://github.com/beeradmoore/dlss-swapper/issues) on our Github repository. If that doesn't solve your problem please create a new issue so we can try get you up and running.

### Updating
~~If you installed via the Microsoft Store DLSS Swapper should update automatically.~~

If you installed via GitHub you will be notified about a new update on launch. You can also manually check for updates in settings. Unlike the Microsoft Store version, DLSS Swapper from GitHub will not update automatically. If you are ever unsure you can always check the [releases section](https://github.com/beeradmoore/dlss-swapper/releases) in our Github repository.
