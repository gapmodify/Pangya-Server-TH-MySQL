using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Linq;
using WebServer.Utils;

namespace WebServer
{
    public class SimpleHttpServer
    {
        private HttpListener _listener;
        private string _rootDirectory;
        private bool _isRunning;
        private int _port;
        private int _activeConnections = 0;
        private readonly object _lockObj = new object();
        private readonly string[] _defaultFiles = { "index.html", "index.htm", "default.html", "default.htm" };

        public SimpleHttpServer(string rootDirectory, int port = 8080)
        {
            _rootDirectory = Path.GetFullPath(rootDirectory);
            _port = port;
            _listener = new HttpListener();
        }

        public void Start()
        {
            try
            {
                if (!Directory.Exists(_rootDirectory))
                {
                    Directory.CreateDirectory(_rootDirectory);
                    Logger.Info($"Directory created: {_rootDirectory}");
                }

                _listener.Prefixes.Add($"http://*:{_port}/");
                _listener.Start();
                _isRunning = true;

                Logger.Success($"Listening on http://localhost:{_port}/");

                var thread = new Thread(HandleRequests);
                thread.Start();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to start: {ex.Message}");
                Logger.Warning("Try running as Administrator or change the port");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            Logger.Warning("Web Server stopped");
        }

        private void HandleRequests()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem((_) => ProcessRequest(context));
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        Logger.Error(ex.Message);
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request  = context.Request;
            var response = context.Response;

            lock (_lockObj) { _activeConnections++; }
           

            string clientIP      = request.RemoteEndPoint.Address.ToString();
            bool   isGameRequest = false;

            try
            {
                string urlPath = request.Url.AbsolutePath;
                while (urlPath.StartsWith("/"))
                    urlPath = urlPath.Substring(1);

                urlPath = urlPath.Replace('/', Path.DirectorySeparatorChar);
                string filePath = Path.Combine(_rootDirectory, urlPath);

                if (Path.GetFileName(filePath).Equals("Read.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    isGameRequest = true;
                    Logger.Info($"[Game] Translation check from {clientIP}");
                    HandleReadAspx(request, response, clientIP, filePath);
                    return;
                }

                if (Directory.Exists(filePath))
                {
                    string defaultFile = null;
                    foreach (var defFile in _defaultFiles)
                    {
                        string testPath = Path.Combine(filePath, defFile);
                        if (File.Exists(testPath)) { defaultFile = testPath; break; }
                    }

                    if (defaultFile != null)
                    {
                        filePath = defaultFile;
                    }
                    else
                    {
                        response.StatusCode = 404;
                        byte[] errorMsg = Encoding.UTF8.GetBytes("<html><body><h1>404 - Not Found</h1></body></html>");
                        response.OutputStream.Write(errorMsg, 0, errorMsg.Length);
                        Logger.Warning($"404 Not Found - No default document");
                        return;
                    }
                }

                if (File.Exists(filePath))
                {
                    byte[] buffer = File.ReadAllBytes(filePath);
                    response.ContentType     = GetContentType(filePath);
                    response.ContentLength64 = buffer.Length;
                    response.StatusCode      = 200;
                    response.OutputStream.Write(buffer, 0, buffer.Length);

                    if (request.Url.AbsolutePath.Contains("updatelist") ||
                        request.Url.AbsolutePath.Contains("extracontents"))
                    {
                        isGameRequest = true;
                        Logger.Success($"[Game] {Path.GetFileName(filePath)} sent ({buffer.Length} bytes)");
                    }
                }
                else
                {
                    response.StatusCode = 404;
                    byte[] errorMsg = Encoding.UTF8.GetBytes($"<html><body><h1>404 - File Not Found</h1><p>{request.Url.AbsolutePath}</p></body></html>");
                    response.OutputStream.Write(errorMsg, 0, errorMsg.Length);
                    Logger.Warning($"404 {request.Url.AbsolutePath}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                response.StatusCode = 500;
            }
            finally
            {
                response.OutputStream.Close();
                lock (_lockObj) { _activeConnections--; }
              
            }
        }

        private void HandleReadAspx(HttpListenerRequest request, HttpListenerResponse response, string clientIP, string filePath)
        {
            try
            {
                var    queryString = request.QueryString;
                string action      = queryString["action"] ?? "";
                string file        = queryString["file"]   ?? "";

                var    result      = new StringBuilder();
                string requestDir  = Path.GetDirectoryName(request.Url.AbsolutePath.TrimStart('/'));
                string targetDir   = Path.Combine(_rootDirectory, requestDir);

                Logger.Info($"[Read.aspx] Action:{action} File:{file}");

                if (action == "info")
                {
                    result.AppendLine("STATUS=OK");
                    result.AppendLine("VERSION=1.0.0");
                    result.AppendLine("SERVER=Pangya Fresh Up!");
                    int fileCount = Directory.Exists(targetDir)
                        ? Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories).Length : 0;
                    result.AppendLine($"RESOURCES={fileCount}");
                }
                else if (action == "list" || action == "")
                {
                    if (Directory.Exists(targetDir))
                    {
                        var patchFiles = Directory.GetFiles(targetDir, "*.*", SearchOption.TopDirectoryOnly)
                            .Where(f => !Path.GetFileName(f).Equals("Read.aspx", StringComparison.OrdinalIgnoreCase))
                            .ToArray();

                        if (patchFiles.Length > 0)
                        {
                            result.AppendLine(patchFiles.Length.ToString());
                            foreach (var pf in patchFiles)
                            {
                                FileInfo fi = new FileInfo(pf);
                                result.AppendLine($"{Path.GetFileName(pf)}|{fi.Length}|{fi.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                            }
                            Logger.Info($"[Read.aspx] Sending {patchFiles.Length} file(s)");
                        }
                        else
                        {
                            result.Append("0");
                            Logger.Info("[Read.aspx] No patch files - skip");
                        }
                    }
                    else
                    {
                        result.Append("0");
                        Logger.Warning("[Read.aspx] Directory not found - skip");
                    }
                }
                else if (action == "check" && !string.IsNullOrEmpty(file))
                {
                    string checkPath = Path.Combine(targetDir, file);
                    if (File.Exists(checkPath))
                    {
                        FileInfo fi = new FileInfo(checkPath);
                        result.AppendLine("OK");
                        result.AppendLine($"{fi.Length}");
                        result.AppendLine($"{fi.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                        Logger.Success($"[Read.aspx] File check OK: {file}");
                    }
                    else
                    {
                        result.Append("0");
                        Logger.Warning($"[Read.aspx] File not found: {file}");
                    }
                }
                else
                {
                    result.Append("0");
                    Logger.Warning($"[Read.aspx] Unknown action: {action}");
                }

                byte[] buffer = Encoding.ASCII.GetBytes(result.ToString());
                response.ContentType     = "text/plain; charset=us-ascii";
                response.ContentLength64 = buffer.Length;
                response.StatusCode      = 200;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Read.aspx] {ex.Message}");
                byte[] errorMsg = Encoding.ASCII.GetBytes("0");
                response.OutputStream.Write(errorMsg, 0, errorMsg.Length);
            }
        }

        private string GetContentType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            string fileName = Path.GetFileName(filePath).ToLowerInvariant();
            
            // Special handling for updatelist files - they are binary patch files
            if (fileName.StartsWith("updatelist"))
            {
                return "application/octet-stream";
            }
            
            switch (extension)
            {
                case ".html": return "text/html";
                case ".htm": return "text/html";
                case ".css": return "text/css";
                case ".js": return "application/javascript";
                case ".json": return "application/json";
                case ".xml": return "application/xml";
                case ".txt": return "text/plain; charset=us-ascii";
                case ".png": return "image/png";
                case ".jpg": return "image/jpeg";
                case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".ico": return "image/x-icon";
                case ".zip": return "application/zip";
                case ".rar": return "application/x-rar-compressed";
                case ".exe": return "application/octet-stream";
                case ".dll": return "application/octet-stream";
                case ".pdf": return "application/pdf";
                case ".mp3": return "audio/mpeg";
                case ".mp4": return "video/mp4";
                case ".iff": return "application/octet-stream";
                case ".pet": return "application/octet-stream";
                case ".tga": return "image/tga";
                case ".aspx": return "text/plain";
                default: return "application/octet-stream";
            }
        }
    }
}
