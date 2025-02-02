using System;
using System.IO;
using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace DLSS_Swapper
{
    public enum LoggingLevel : int
    {
        Off = 0,
        Verbose = 10,
        Debug = 20,
        Info = 30,
        Warning = 40,
        Error = 50,
    }

    internal static class Logger
    {
        public static string LogDirectory => Path.Combine(Storage.GetTemp(), "logs");
        static string loggingFile => Path.Combine(LogDirectory, "dlss_swapper_.log");
        static LoggingLevelSwitch levelSwitch = new(LogEventLevel.Fatal);

        internal static void Init()
        {
            if (Directory.Exists(LogDirectory) == false)
            {
                Directory.CreateDirectory(LogDirectory);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Debug()
                .WriteTo.File(loggingFile, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                .CreateLogger();

            ChangeLoggingLevel(Settings.Instance.LoggingLevel);
        }

        public static string GetCurrentLogPath()
        {
            var withoutExtension = Path.GetFileNameWithoutExtension(loggingFile);
            var justExtension = Path.GetExtension(loggingFile);
            return Path.Combine(LogDirectory, $"{withoutExtension}{DateTime.Now.ToString("yyyyMMdd")}{justExtension}");
        }

        public static void ChangeLoggingLevel(LoggingLevel loggingLevel)
        {
            // Off is secretly fatal as I don't know how to turn off logging :|
            levelSwitch.MinimumLevel = Settings.Instance.LoggingLevel switch
            {
                LoggingLevel.Verbose => LogEventLevel.Verbose,
                LoggingLevel.Debug => LogEventLevel.Debug,
                LoggingLevel.Info => LogEventLevel.Information,
                LoggingLevel.Warning => LogEventLevel.Warning,
                LoggingLevel.Error => LogEventLevel.Error,
                _ => LogEventLevel.Fatal,
            };
        }


        public static void Verbose(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log.Verbose(FormatLine(message, memberName, sourceFilePath, sourceLineNumber));
        }

        public static void Debug(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log.Debug(FormatLine(message, memberName, sourceFilePath, sourceLineNumber));
        }

        public static void Info(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log.Information(FormatLine(message, memberName, sourceFilePath, sourceLineNumber));
        }

        public static void Warning(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log.Warning(FormatLine(message, memberName, sourceFilePath, sourceLineNumber));
        }

        public static void Error(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log.Error(FormatLine(message, memberName, sourceFilePath, sourceLineNumber));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string FormatLine(string message, string? memberName, string? sourceFilePath, int sourceLineNumber)
        {
            if (memberName is null || sourceFilePath is null || sourceLineNumber == 0)
            {
                return message;
            }

            return $"{Path.GetFileName(sourceFilePath)}:{sourceLineNumber} {memberName} - {message}";
        }
    }
}
