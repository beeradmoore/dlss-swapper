using System.Collections.Generic;
using System.Linq;

namespace DLSS_Swapper.Data.UbisoftConnect
{
    internal class UbisoftConnectConfigurationItem
    {
        public string Version { get; set; } = string.Empty;
        public RootNode? Root { get; set; } = null;
        public Dictionary<string, Dictionary<string, string>>? Localizations { get; set; } = null;

        internal class RootNode
        {
            public string Name { get; set; } = string.Empty;
            public string BackgroundImage { get; set; } = string.Empty;
            public string ThumbImage { get; set; } = string.Empty;
            public string LogoImage { get; set; } = string.Empty;
            public string SplashImage { get; set; } = string.Empty;
            public string IconImage { get; set; } = string.Empty;
            public string AppId { get; set; } = string.Empty;
            public string SpaceId { get; set; } = string.Empty;
            public StartGameNode? StartGame { get; set; } = null;
            public InstallerNode? Installer { get; set; } = null;
        }

        public class InstallerNode
        {
            public string GameIdentifier { get; set; } = string.Empty;
        }

        public class StartGameNode
        {
            public StartGameModeNode? Online { get; set; } = null;
            public StartGameModeNode? Offline { get; set; } = null;

            public List<ExecutablesNode> GetUniqueExecutables()
            {
                var uniqueExecutables = new List<ExecutablesNode>();
                var uniqueRegisters = new List<string>();

                if (Online?.Executables?.Any() == true)
                {
                    foreach (var executable in Online.Executables)
                    {
                        // If there is no registry key then skip.
                        if (string.IsNullOrEmpty(executable.WorkingDirectory?.Register))
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

                if (Offline?.Executables?.Any() == true)
                {
                    foreach (var executable in Offline.Executables)
                    {
                        // If there is no registry key then skip.
                        if (string.IsNullOrEmpty(executable.WorkingDirectory?.Register))
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
            public List<ExecutablesNode>? Executables { get; set; } = null;
        }

        public class ExecutablesNode
        {
            public string ShortcutName { get; set; } = string.Empty;
            public WorkingDirectoryNode? WorkingDirectory { get; set; } = null;
        }

        public class WorkingDirectoryNode
        {
            public string Register { get; set; } = string.Empty;
        }
    }
}
