using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.UbisoftConnect
{
    internal class UbisoftConnectGame : Game
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

                if (System.IO.File.Exists(_localHeaderImage))
                {
                    _lastHeaderImage = _localHeaderImage;
                    return _localHeaderImage;
                }

                return _remoteHeaderImage;
            }
        }

        string _localHeaderImage;
        string _remoteHeaderImage;

        public UbisoftConnectGame(string localHeaderImage, string remoteHeaderImage)
        {
            _localHeaderImage = localHeaderImage;
            _remoteHeaderImage = remoteHeaderImage;
        }
    }
}
