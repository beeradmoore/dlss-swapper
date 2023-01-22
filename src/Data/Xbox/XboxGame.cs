using System;
using System.Collections.Generic;

namespace DLSS_Swapper.Data.Xbox
{
    public class XboxGame : Game
    {
        string _lastHeaderImage = String.Empty;
        public override string HeaderImage
        {
            get
            {
                // If we have detected this, return it other wise we figure it out.
                if (String.IsNullOrEmpty(_lastHeaderImage) == false)
                {
                    return _lastHeaderImage;
                }

                foreach (var localHeaderImage in _localHeaderImages)
                {
                    var headerImage = System.IO.Path.Combine(InstallPath, localHeaderImage);
                    if (System.IO.File.Exists(headerImage))
                    {
                        _lastHeaderImage = headerImage;
                        return headerImage;
                    }
                }

                return _lastHeaderImage;
            }
        }

        List<string> _localHeaderImages = new List<string>();

        public XboxGame(List<string> localHeaderImages)
        {
            _localHeaderImages = localHeaderImages;
        }
    }
}
