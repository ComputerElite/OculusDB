using ComputerUtils.RegexStuff;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ComputerUtils.Logging {
    public class Logger
    {
        public static string logFile { get; set; } = "";
        public static bool removeUsernamesFromLog { get; set; } = true;
        public static bool displayLogInConsole { get; set; } = false;
        public static bool longLogInConsole { get; set; } = true;
        public static bool saveOutputInVariable { get; set; } = true;
        public static string log { get; set; } = "";
        public static List<string> notAllowedStrings { get; set; } = new List<string>();
        public static ReaderWriterLock locker = new ReaderWriterLock();
        public static string GetLog()
        {
            string l = log;
            log = "";
            return l;
        }

        public static string CensorString(string input)
        {
            foreach (string s in notAllowedStrings) input = input.Replace(s, "");
            return input;
        }

		public static bool savingLog = false;

		public static void Log(string text, LoggingType loggingType = LoggingType.Info)
        {
            //Remove username
            if (removeUsernamesFromLog) text = RegexTemplates.RemoveUserName(text);
            string linePrefix = GetLinePrefix(loggingType);
            text = linePrefix + text.Replace("\n", "\n" + linePrefix);
            text = CensorString(text);
            if (displayLogInConsole)
            {
                switch (loggingType)
                {
                    case LoggingType.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LoggingType.Info:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LoggingType.Crash:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LoggingType.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LoggingType.Debug:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LoggingType.ADBIntern:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LoggingType.ADB:
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        break;
                    case LoggingType.Important:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        break;
                    case LoggingType.WebServer:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                }
                Console.WriteLine(longLogInConsole ? text : text.Replace(linePrefix, ""));
                Console.ForegroundColor = ConsoleColor.White;
            }
            if (saveOutputInVariable)
            {
                log += "\n" + text;
                if (log.Length > 10000)
                {
                    log = log.Substring(log.Length - 10000);
                }
            }
            if (logFile == "") return;
            LogRaw("\n" + text);
		}
        public static void LogRaw(string text)
        {
            if (logFile == "") return;
            try
            {
                // Aquire a writer lock to make sure no other thread is writing to the file
                locker.AcquireWriterLock(10000); //You might wanna change timeout value 
                File.AppendAllText(logFile, text);
            }
            finally
            {
                locker.ReleaseWriterLock();
            }
        }

        public static string GetLinePrefix(LoggingType loggingType)
        {
            DateTime t = DateTime.Now;
            return "[" + t.Day.ToString("d2") + "." + t.Month.ToString("d2") + "." + t.Year.ToString("d4") + "   " + t.Hour.ToString("d2") + ":" + t.Minute.ToString("d2") + ":" + t.Second.ToString("d2") + "." + t.Millisecond.ToString("d5") + "] " + (Enum.GetName(typeof(LoggingType), loggingType) + ":").PadRight(15);
        }

        public static void SetLogFile(string file)
        {
            logFile = file;
        }
    }

    public enum LoggingType
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Debug = 3,
        Crash = 4,
        ADB = 5,
        ADBIntern = 6,
        Important = 7,
        WebServer = 8
    }
}