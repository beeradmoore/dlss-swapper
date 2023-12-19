using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.GOG
{
    internal class GOGGame : Game
    {
        public int Id { get; set; } = -1;

        public List<string> PotentialLocalHeaders { get; } = new List<string>();
        public string FallbackHeaderUrl { get; set; } = String.Empty;

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

                foreach (var potentialLocalHeader in PotentialLocalHeaders)
                {
                    if (File.Exists(potentialLocalHeader))
                    {
                        _lastHeaderImage = potentialLocalHeader;
                        return _lastHeaderImage;
                    }
                }

                _lastHeaderImage = FallbackHeaderUrl;
                return _lastHeaderImage;
            }
        }


        public GOGGame()
        {

        }
    }
}
