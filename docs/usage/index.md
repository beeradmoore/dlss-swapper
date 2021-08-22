## Usage

DLSS Swapper needs you to either download DLSS dlls with the in-built downloader, or place them in a folder manually.

Currently DLSS Swapper will detect games installed via:
- Steam

More platforms may be added over time. Please [create a feature request](https://github.com/beeradmoore/dlss-swapper/issues) or upvote an existing one.

### Downloading new DLSS versions in the app

Go to the `Downloads` side menu option.

On the left of this screen is the TechPowerUp website which allows you to download new dlls. On the right is a list of downloadable dlls zip packages. If you have that dll zip downloaded it will indicate so with `Extract` and `Delete` buttons. If everything goes to plan you never have to click these two buttons.

To download a dll use the TechPowerUp site and hit the green `Download` button followed by choosing a download mirror. This will initiate a download which will display progress in the right menu. When the download completes the dll will automatcially be extracted and placed into the correct directory, ready for you to use.

![Gif animation showing the download process](https://beeradmoore.github.io/dlss-swapper/images/usage/usage_1.gif)


### Manually managing DLSS versions

Upon first launch a folder called `DLSS Swapper` will be created in your `Documents` directory.

To manually add your own DLSS dlls you will need to: 
1. Create a `dlls` folder within the `DLSS Swapper` folder
2. Inside that folder create another folder with the version of the dll you have (eg. 2.2.16.0)
3. Inside that folder paste in your `nvngx_dlss.dll` file.

The final path for the dll in this example would be `Documents\DLSS Swapper\dlls\2.2.16.0\nvngx_dlss.dll`. You may need to restart DLSS Swapper for it to detect this dll.


### Changing DLSS version for a game

Once you have your desired DLSS dlls setup in your Documents folder you can use the `Games` menu option to browse installed games.


Click on a game you wish to swap DLSS versions. This will display a drop down of all available DLSS versions. Clicking `Update` will update your DLSS version for that game. 

![Gif animation showing the user change DLSS version](https://beeradmoore.github.io/dlss-swapper/images/usage/usage_2.gif)

The existing `nvngx_dlss.dll` will be renamed to `nvngx_dlss.dll.dlsss` (if one doesn't already exist) so you can restore it later.


### Restoring DLSS version back to original

If your game does not function correctly you can restore the original DLSS version by using the same popup used to change the version, but instead you should now click `Reset`.

![Gif animation showing the user restoring the original DLSS version](https://beeradmoore.github.io/dlss-swapper/images/usage/usage_3.gif)

If a file named `nvngx_dlss.dll.dlsss` is not found the `Reset` option will not be available. If you are unable to reset for some reason you may need to repair your game installation. 
