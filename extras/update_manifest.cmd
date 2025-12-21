set current_directory=%~dp0
curl.exe --output "%current_directory%..\src\Assets\static_manifest.json" --url https://raw.githubusercontent.com/beeradmoore/dlss-swapper-manifest-builder/refs/heads/main/manifest.json
copy "%current_directory%..\src\Assets\static_manifest.json" "%current_directory%..\docs\manifest.json"