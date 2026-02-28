using System;
using System.Configuration;
using System.IO;
using WebServer.Utils;

namespace WebServer
{
    class Program
    {
        static SimpleHttpServer server;
        static string _rootDirectory;
        static int    _port;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "Pangya Fresh Up! Web Server";

            // Show ASCII banner on console before switching to UI
            Logger.Initialize("Log");

            try
            {
                _rootDirectory = ConfigurationManager.AppSettings["WebRoot"]
                    ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www");
                _port = int.TryParse(ConfigurationManager.AppSettings["Port"], out int p) ? p : 8080;

                // Start server
                server = new SimpleHttpServer(_rootDirectory, _port);

                // Switch logger to UI mode before server.Start() so all output goes to the panel
                Logger.UIMode = true;

                // Start HTTP server (non-blocking)
                server.Start();

                Logger.Info($"URL: http://localhost:{_port}/");
                Logger.Info("Type 'help' for available commands.");

                // Keep the process alive and handle console commands
                string input;
                while ((input = Console.ReadLine()) != null)
                {
                    input = input.Trim();
                    if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                        input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Info("Shutting down...");
                        break;
                    }
                    if (input.Length > 0)
                        HandleCommand(input);
                }
            }
            catch (Exception ex)
            {
                Logger.UIMode = false;
                Logger.Critical($"[FATAL ERROR] {ex.Message}");
                Console.ReadKey();
            }
        }

        static void HandleCommand(string command)
        {
            switch (command.ToLower())
            {
                case "help":
                    Logger.Info("Commands: help | clear | info | quit");
                    break;
                case "clear":
                    // clear the log view by logging a separator
                    Logger.Info(new string('-', 60));
                    break;
                case "info":
                    ShowInfo();
                    break;
                default:
                    Logger.Error($"Unknown command '{command}' — type 'help'");
                    break;
            }
        }

        static void ShowInfo()
        {
            Logger.Info($"URL    : http://localhost:{_port}/");
            Logger.Info($"Status : Running");

            if (Directory.Exists(_rootDirectory))
            {
                var files = Directory.GetFiles(_rootDirectory, "*.*", SearchOption.AllDirectories);
                var dirs  = Directory.GetDirectories(_rootDirectory, "*", SearchOption.AllDirectories);
                long totalSize = 0;
                foreach (var f in files) totalSize += new FileInfo(f).Length;
                Logger.Info($"Files  : {files.Length} file(s) in {dirs.Length + 1} folder(s)");
                Logger.Info($"Size   : {FormatBytes(totalSize)}");
            }
        }

        static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
