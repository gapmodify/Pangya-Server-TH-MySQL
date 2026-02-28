using System;
using System.IO;

namespace WebServer.Utils
{
    /// <summary>
    /// Enhanced logger class for file and console logging with colors
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logDirectory = "Log";
        private static string _currentLogFile;
        private static string _serverName = "WebServer";
        private static bool _initialized = false;

        // When true, log lines are routed to ServerUI instead of Console
        public static bool UIMode { get; set; } = false;

        public static void Initialize(string logDirectory, bool useColors = true)
        {
            _logDirectory = logDirectory;

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            _currentLogFile = Path.Combine(_logDirectory, $"{_serverName}_{DateTime.Now:yyyy-MM-dd}.log");
            _initialized = true;

            // Welcome banner
            Console.WriteLine();

            string[] art = AsciiBanner.Render(_serverName);

            // Box width = art width + 4 padding (2 each side), minimum 50
            int artWidth   = art[0].Length;
            int innerWidth = Math.Max(artWidth + 4, 50);
            // make innerWidth even so centering is exact
            if ((innerWidth - artWidth) % 2 != 0) innerWidth++;

            string hLine  = "  +" + new string('-', innerWidth) + "+";
            string emptyRow = "  |" + new string(' ', innerWidth) + "|";

            WriteColored(hLine,     ConsoleColor.Cyan);
            WriteColored(emptyRow,  ConsoleColor.Cyan);

            foreach (string row in art)
            {
                int padLeft  = (innerWidth - row.Length) / 2;
                int padRight = innerWidth - row.Length - padLeft;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("  |");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(new string(' ', padLeft));
                Console.Write(row);
                Console.Write(new string(' ', padRight));
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("|");
                Console.ResetColor();
            }

            WriteColored(emptyRow,  ConsoleColor.Cyan);
            WriteColored(hLine,     ConsoleColor.Cyan);
            WriteBannerRow("Server ", _serverName,                                                             innerWidth);
            WriteBannerRow("Started", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),                           innerWidth);
            string logDir = _logDirectory.Length > innerWidth - 16 ? _logDirectory.Substring(0, innerWidth - 19) + "..." : _logDirectory;
            WriteBannerRow("Log Dir", logDir,                                                                  innerWidth);
            WriteColored(hLine, ConsoleColor.Cyan);
            Console.WriteLine();
        }

        private static void WriteColored(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void WriteBannerRow(string label, string value, int innerWidth)
        {
            // "  | >> Label : value    |"
            string prefix = $" >> {label} : ";
            int valueWidth = innerWidth - prefix.Length - 2; // -2 for leading space + trailing space
            string padded = value.Length > valueWidth ? value.Substring(0, valueWidth) : value.PadRight(valueWidth);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("  |");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(prefix);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(padded);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(" |");
            Console.ResetColor();
        }

        public static void Write(string message, ConsoleColor color = ConsoleColor.Gray, ConsoleColor labelColor = ConsoleColor.Gray, string label = "")
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");

            lock (_lock)
            {
                
                
                    // Console output with colors
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("[");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(timestamp);
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("]");

                    if (!string.IsNullOrEmpty(label))
                    {
                        Console.Write(" ");
                        Console.ForegroundColor = labelColor;
                        Console.Write(label);
                    }

                    Console.Write(" ");
                    Console.ForegroundColor = color;
                    Console.WriteLine(message);
                    Console.ResetColor();
                

                // File output (plain text)
                try
                {
                    if (_currentLogFile != null && _initialized)
                    {
                        var fileTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        var logMessage = $"[{fileTimestamp}] {label} {message}";
                        File.AppendAllText(_currentLogFile, logMessage + Environment.NewLine);
                    }
                }
                catch
                {
                    // Ignore file write errors
                }
            }
        }

        public static void Info(string message)
        {
            Write(message, ConsoleColor.White, ConsoleColor.Cyan, "[INFO]");
        }

        public static void Success(string message)
        {
            Write(message, ConsoleColor.Green, ConsoleColor.Green, "[✓]");
        }

        public static void Warning(string message)
        {
            Write(message, ConsoleColor.Yellow, ConsoleColor.Yellow, "[⚠]");
        }

        public static void Error(string message)
        {
            Write(message, ConsoleColor.Red, ConsoleColor.Red, "[✗]");
        }

        public static void Debug(string message)
        {
#if DEBUG
            Write(message, ConsoleColor.Magenta, ConsoleColor.Magenta, "[DEBUG]");
#endif
        }

        public static void Critical(string message)
        {
            Write(message, ConsoleColor.White, ConsoleColor.Red, "[CRITICAL]");
        }

        public static void Trace(string message)
        {
#if DEBUG
            Write(message, ConsoleColor.DarkGray, ConsoleColor.DarkGray, "[TRACE]");
#endif
        }
    }
}
