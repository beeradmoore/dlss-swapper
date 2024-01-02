using System;
using System.Collections.Generic;
using System.IO;
using DLSS_Swapper.Interfaces;
using SQLite;

namespace DLSS_Swapper.Data.Xbox
{
    [Table("XboxGame")]
    public class XboxGame : Game
    {
        public override GameLibrary GameLibrary => GameLibrary.XboxApp;

        List<string> _localHeaderImages = new List<string>();

        public XboxGame()
        {

        }

        public XboxGame(string familyName)
        {
            PlatformId = familyName;
            SetID();
        }

        internal void SetLocalHeaderImages(List<string> localHeaderImages)
        {
            _localHeaderImages = localHeaderImages;
        }

        protected override void UpdateCacheImage()
        {
            foreach (var localHeaderImage in _localHeaderImages)
            {
                var headerImage = Path.Combine(InstallPath, localHeaderImage);
                if (File.Exists(headerImage))
                {
                    ResizeCover(headerImage);
                    return;
                }
            }
        }
    }
}
