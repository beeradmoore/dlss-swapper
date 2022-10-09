using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.UbisoftConnect
{
    internal class UbisoftConnectConfigurationItem
    {
        public string Version { get; set; }
        public RootNode Root { get; set; }
        public Dictionary<string, Dictionary<string, string>> Localizations { get; set; }

        internal class RootNode
        {
            public string Name { get; set; }
            public string BackgroundImage { get; set; }
            public string ThumbImage { get; set; }
            public string LogoImage { get; set; }
            public string SplashImage { get; set; }
            public string IconImage { get; set; }
            public string AppId { get; set; }
            public string SpaceId { get; set; }
            public StartGameNode StartGame { get; set; }
            public InstallerNode Installer { get; set; }
        }

        public class InstallerNode
        {
            public string GameIdentifier { get; set; }
        }

        public class StartGameNode
        {
            public StartGameModeNode Online { get; set; }
            public StartGameModeNode Offline { get; set; }

            public List<ExecutablesNode> GetUniqueExecutables()
            {
                var uniqueExecutables = new List<ExecutablesNode>();
                var uniqueRegisters = new List<string>();

                if (Online?.Executables.Any() == true)
                {
                    foreach (var executable in Online?.Executables)
                    {
                        // If there is no registry key then skip.
                        if (String.IsNullOrEmpty(executable.WorkingDirectory?.Register))
                        {
                            continue;
                        }

                        if (uniqueRegisters.Contains(executable.WorkingDirectory.Register) == false)
                        {
                            uniqueRegisters.Add(executable.WorkingDirectory.Register);
                            uniqueExecutables.Add(executable);
                        }
                    }
                }

                if (Offline?.Executables.Any() == true)
                {
                    foreach (var executable in Offline?.Executables)
                    {
                        // If there is no registry key then skip.
                        if (String.IsNullOrEmpty(executable.WorkingDirectory?.Register))
                        {
                            continue;
                        }

                        if (uniqueRegisters.Contains(executable.WorkingDirectory.Register) == false)
                        {
                            uniqueRegisters.Add(executable.WorkingDirectory.Register);
                            uniqueExecutables.Add(executable);
                        }
                    }
                }

                return uniqueExecutables;
            }
        }
        
        public class StartGameModeNode
        {
            public List<ExecutablesNode> Executables { get; set; }
        }

        public class ExecutablesNode
        {
            public string ShortcutName { get; set; }
            public WorkingDirectoryNode WorkingDirectory { get; set; }
        }

        public class WorkingDirectoryNode
        {
            public string Register { get; set; }
        }
    }
}
