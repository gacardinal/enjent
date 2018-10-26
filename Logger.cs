using System;

namespace NarcityMedia.Log
{
    static public class Logger
    {
        public enum LogType
        {
            Success, Warning, Error, Info
        }

        // ANSI color tokens must match the index of the LogType enum
        private static readonly string[] ANSITokens = { "\u001b[32m", "\u001b[33m", "\u001b[31m", "\u001b[34m" };

        private static readonly string ANSIReset = "\u001b[0m";

        public static void Log(string message, LogType logType)
        {
            Console.WriteLine(String.Format("{0} | {1}{2}{3}", DateTime.Now.ToString("dd/mm/yyyy - HH:mm:ss"), ANSITokens[(int)logType], message, ANSIReset));
        }
    }
}
