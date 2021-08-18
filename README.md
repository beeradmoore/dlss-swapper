# DLSS Swapper


More detailed readme coming soon.


### What is this?
Tool for downloading, managing, and swapping out DLSS (Deep Learning Super Sampling) dlls in games on your computer.


### But why?
DLSS gets updates from nVidia, but often studios are not updating DLSS within their own games. See [this](https://www.youtube.com/watch?v=dtbqJXb1UDw) video from Digital Foundry as to why you may want to manually update DLSS yourself.


### What games are supported?
Any Steam game that suppoorts DLSS __should__ work.


### Where are the dlls coming from?
DLSS Swapper has an in-built tool to download zips from [TechPowerUp](https://www.techpowerup.com/download/nvidia-dlss-dll/). If you want to source your own dlls you can do so by putting them your My Documents folder in the following path `Documents/DLSS Swapper/dlls/<dll_version>/nvngx_dlss.dll`


### But what if TechPowerUp upload a malicious dll?
Like any tool, please use at your risk. I'll add a way to verify dlls with known file hashes. I have this hand curated list of dlls [here](https://github.com/beeradmoore/dlss_version_tracker) and their hashes. Maybe this will be incorperated at some point (assuming you trust me to give correct hashes 🤓)