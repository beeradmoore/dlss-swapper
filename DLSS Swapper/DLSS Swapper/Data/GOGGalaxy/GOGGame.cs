using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.GOGGalaxy
{
    internal class GOGGame : Game
    {
        List<string> _potentialLocalHeaders;
        string _fallbackHeaderUrl;
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

                foreach (var potentialLocalHeader in _potentialLocalHeaders)
                {
                    if (File.Exists(potentialLocalHeader))
                    {
                        _lastHeaderImage = potentialLocalHeader;
                        return _lastHeaderImage;
                    }
                }

                _lastHeaderImage = _fallbackHeaderUrl;
                return _lastHeaderImage;
            }
        }


        public GOGGame(List<string> potentialLocalHeaders, string fallbackHeaderUrl)
        {
            _potentialLocalHeaders = potentialLocalHeaders;
            _fallbackHeaderUrl = fallbackHeaderUrl;

        }
    }
}
