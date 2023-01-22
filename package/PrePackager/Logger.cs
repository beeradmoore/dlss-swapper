using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper
{
    internal static class Logger
    {
        public static void Verbose(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            Console.WriteLine("VERBOSE: " + FormatLine(message, memberName, sourceFilePath, sourceLineNumber));
        }

        public static void Debug(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            Console.WriteLine("DEBUG: " + FormatLine(message, memberName, sourceFilePath, sourceLineNumber));
        }

        public static void Info(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            Console.WriteLine("INFO: " + FormatLine(message, memberName, sourceFilePath, sourceLineNumber));
        }

        public static void Warning(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            Console.WriteLine("WARNING: " + FormatLine(message, memberName, sourceFilePath, sourceLineNumber));
        }

        public static void Error(string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            Console.WriteLine("ERROR: " + FormatLine(message, memberName, sourceFilePath, sourceLineNumber));
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
