using DLSS_Swapper.Data;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper
{
    static class DLSSDLLSearcher
    {
        private class DLLSearcher
        {
            public string rootPath { get; set; }
            public ConcurrentQueue<Game> searchQueue = new ConcurrentQueue<Game>();
            public bool isSearching = false;
        }

        private static List<DLLSearcher> searchers = new List<DLLSearcher>();

        public static void AddToSearchQueue(Game game)
        {
            var rootPath = Path.GetPathRoot(game.InstallPath);

            DLLSearcher rootSearcher = searchers.FirstOrDefault(x => x.rootPath == rootPath);
            if (rootSearcher != null)
            {
                //add game to existing queue for that root path
                rootSearcher.searchQueue.Enqueue(game);

                if (!rootSearcher.isSearching)
                    Task.Run(() => DLLSearch(rootSearcher));
            }
            else
            {
                //spin up a new searcher for the root path
                DLLSearcher newSearcher = new DLLSearcher();
                newSearcher.rootPath = rootPath;
                newSearcher.searchQueue.Enqueue(game);
                searchers.Add(newSearcher);

                if (!newSearcher.isSearching)
                    Task.Run(() => DLLSearch(newSearcher));
            }
        }

        private static void DLLSearch(DLLSearcher searcher)
        {
            searcher.isSearching = true;
            while (searcher.searchQueue.Count > 0)
            {
                //get game to search
                Game game;
                if(searcher.searchQueue.TryDequeue(out game))
                {
                    var dlssDlls = Directory.EnumerateFiles(game.InstallPath, "nvngx_dlss.dll", SearchOption.AllDirectories);
                    var dlssDll = dlssDlls.FirstOrDefault();

                    //fetch first found dll
                    if (dlssDll != null)
                    {
                        var dllVersionInfo = FileVersionInfo.GetVersionInfo(dlssDll);
                        ((App)Application.Current).Window.DispatcherQueue.TryEnqueue(() =>
                        {
                            game.HasDLSS = true;
                            game.CurrentDLSSVersion = new Version(dllVersionInfo.FileVersion.Replace(",", "."));
                        });

                        //found a version of DLSS, check for base DLL (will be next to original)
                        string basePath = Path.Combine(Path.GetDirectoryName(dlssDll), "nvngx_dlss.dll.dlsss");
                        if (File.Exists(basePath))
                        {
                            FileVersionInfo dllBaseVersionInfo = FileVersionInfo.GetVersionInfo(basePath);
                            ((App)Application.Current).Window.DispatcherQueue.TryEnqueue(() =>
                            {
                                game.BaseDLSSVersion = new Version(dllBaseVersionInfo.FileVersion.Replace(',', '.'));
                            });
                        }
                    }
                    else
                    {
                        ((App)Application.Current).Window.DispatcherQueue.TryEnqueue(() =>
                        {
                            game.HasDLSS = false;
                        });
                    }

                    //report game DLSS has been checked
                    ((App)Application.Current).Window.DispatcherQueue.TryEnqueue(() =>
                    {
                        game.DLSSChecked = true;
                    });
                }
            }
            searcher.isSearching = false;
        }
    }
}
