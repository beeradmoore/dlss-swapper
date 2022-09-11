using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace DLSS_Swapper
{
    public enum LoggingLevel : int
    {
        Off = 0,
        Info = 10,
        Debug = 20,
    }

    internal static class Logger
    {

        private static readonly ILog logger = LogManager.GetLogger(typeof(App));

        internal static void Init()
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();

            var patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%date [%thread] %-5level - %message%newline";
            patternLayout.ActivateOptions();

            var roller = new RollingFileAppender();
            roller.AppendToFile = true;
            roller.File = Path.Combine(Path.GetTempPath(), "dlss_swapper");
            roller.Layout = patternLayout;
            roller.MaxSizeRollBackups = 5;
            roller.RollingStyle = RollingFileAppender.RollingMode.Date;
            roller.StaticLogFileName = false;
            roller.DatePattern = ".yyyy.MM.dd'.log'";
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);

            var console = new ConsoleAppender();
            console.ActivateOptions();
            hierarchy.Root.AddAppender(console);

            var debug = new DebugAppender();
            debug.ActivateOptions();
            hierarchy.Root.AddAppender(debug);

            var memory = new MemoryAppender();
            memory.ActivateOptions();
            hierarchy.Root.AddAppender(memory);

            hierarchy.Root.Level = Settings.LoggingLevel switch
            {
                LoggingLevel.Debug => Level.Debug,
                LoggingLevel.Info => Level.Info,
                _ => Level.Off,
            };
            BasicConfigurator.Configure(hierarchy);
        }

        public static void ChangeLoggingLevel(LoggingLevel loggingLevel)
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();
            hierarchy.Root.Level = Settings.LoggingLevel switch
            {
                LoggingLevel.Debug => Level.Debug,
                LoggingLevel.Info => Level.Info,
                _ => Level.Off,
            };
            hierarchy.RaiseConfigurationChanged(EventArgs.Empty);
        }

        [Conditional("DEBUG")]
        public static void Debug(string message)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG - {message}");
            logger.Debug(message);
        }

        public static void Info(string message)
        {
            System.Diagnostics.Debug.WriteLine($"INFO - {message}");
            logger.Info(message);
        }
    }
}
