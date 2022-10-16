using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.EpicGameStore
{
    internal class EpicGameStoreGame : Game
    {
        string _id = String.Empty;
        string _remoteHeaderImage = String.Empty;

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

                // TODO: Add remote image to queue to download for next time.

                // If the remote image doens't already have query arguments lets add some to load a smaller image.
                if (_remoteHeaderImage.Contains("?"))
                {
                    return _remoteHeaderImage;
                }
                return _remoteHeaderImage + "?h=300&resize=1&w=200";
            }
        }


        public EpicGameStoreGame(string id, string remoteHeaderImage)
        {
            _id = id;
            _remoteHeaderImage = remoteHeaderImage;
        }
    }
}
