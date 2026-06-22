using System.Net;
using System.Text;
using System.Text.Json;

namespace TrackerApp;

public class WebServer
{
    private readonly HttpListener _listener;
    private readonly Database _db;
    private readonly int _port;
    private bool _running;

    public int Port => _port;

    public WebServer(Database db, int startPort = 5005)
    {
        _db = db;
        _listener = new HttpListener();

        // Try to find a free port from startPort to startPort + 10
        for (int p = startPort; p <= startPort + 10; p++)
        {
            try
            {
                _listener.Prefixes.Clear();
                _listener.Prefixes.Add($"http://localhost:{p}/");
                _listener.Start(); // test binding
                _port = p;
                _listener.Stop(); // stop test
                break;
            }
            catch (HttpListenerException)
            {
                if (p == startPort + 10)
                    throw; // No available port found
            }
        }
    }

    public void Start()
    {
        _running = true;
        _listener.Start();
        Task.Run(ListenLoop);
    }

    public void Stop()
    {
        _running = false;
        try
        {
            _listener.Stop();
        }
        catch
        {
            // Ignore potential exceptions on shutdown
        }
    }

    private async Task ListenLoop()
    {
        while (_running)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context));
            }
            catch (HttpListenerException)
            {
                // Listener was stopped or error occurred
            }
            catch (Exception)
            {
                // General error, ignore and continue listening
            }
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        var req = context.Request;
        var resp = context.Response;

        try
        {
            // Simple CORS support just in case
            resp.AddHeader("Access-Control-Allow-Origin", "*");
            resp.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            resp.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            if (req.HttpMethod == "OPTIONS")
            {
                resp.StatusCode = (int)HttpStatusCode.OK;
                resp.Close();
                return;
            }

            if (req.HttpMethod == "GET" && req.Url?.AbsolutePath == "/")
            {
                var html = Dashboard.BuildHtml(_db);
                var buf = Encoding.UTF8.GetBytes(html);
                resp.ContentType = "text/html; charset=utf-8";
                resp.ContentLength64 = buf.Length;
                await resp.OutputStream.WriteAsync(buf);
            }
            else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/api/map")
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                var json = await reader.ReadToEndAsync();
                var data = JsonSerializer.Deserialize<MappingRequest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data != null && !string.IsNullOrWhiteSpace(data.Process))
                {
                    _db.SaveProcessSettings(data.Process, data.Category, data.CustomName);
                    resp.StatusCode = (int)HttpStatusCode.OK;
                    var successMsg = Encoding.UTF8.GetBytes("{\"success\":true}");
                    resp.ContentType = "application/json";
                    resp.ContentLength64 = successMsg.Length;
                    await resp.OutputStream.WriteAsync(successMsg);
                }
                else
                {
                    resp.StatusCode = (int)HttpStatusCode.BadRequest;
                    var errMsg = Encoding.UTF8.GetBytes("{\"error\":\"Invalid request data\"}");
                    resp.ContentType = "application/json";
                    resp.ContentLength64 = errMsg.Length;
                    await resp.OutputStream.WriteAsync(errMsg);
                }
            }
            else if (req.HttpMethod == "GET" && req.Url?.AbsolutePath == "/api/timeline")
            {
                var start = req.QueryString["start"] ?? DateTime.Now.ToString("yyyy-MM-dd");
                var end = req.QueryString["end"] ?? DateTime.Now.ToString("yyyy-MM-dd");

                var timeline = _db.QueryTimeline(start, end);
                var json = JsonSerializer.Serialize(timeline);
                var buf = Encoding.UTF8.GetBytes(json);

                resp.ContentType = "application/json; charset=utf-8";
                resp.ContentLength64 = buf.Length;
                await resp.OutputStream.WriteAsync(buf);
            }
            else
            {
                resp.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
        catch (Exception ex)
        {
            resp.StatusCode = (int)HttpStatusCode.InternalServerError;
            var err = Encoding.UTF8.GetBytes(ex.ToString());
            resp.ContentType = "text/plain";
            resp.ContentLength64 = err.Length;
            try
            {
                await resp.OutputStream.WriteAsync(err);
            }
            catch
            {
                // Ignore writing error
            }
        }
        finally
        {
            try
            {
                resp.Close();
            }
            catch
            {
                // Ignore close error
            }
        }
    }

    private class MappingRequest
    {
        public string Process { get; set; } = "";
        public string? Category { get; set; }
        public string? CustomName { get; set; }
    }
}
