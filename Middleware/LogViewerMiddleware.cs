using System.Text;
using System.Text.Json;
using EzLib.Models;
using EzLib.Services;
using Microsoft.AspNetCore.Http;

namespace EzLib.Middleware
{
    /// <summary>
    /// 提供宿主專案 log 檔案瀏覽 UI 與 API 的 middleware。
    /// </summary>
    public class LogViewerMiddleware
    {
        private static readonly JsonSerializerOptions JsonOut = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly RequestDelegate _next;
        private readonly LogViewerSettings _settings;
        private readonly ILogViewerService _logViewer;

        public LogViewerMiddleware(
            RequestDelegate next,
            LogViewerSettings settings,
            ILogViewerService logViewer)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logViewer = logViewer ?? throw new ArgumentNullException(nameof(logViewer));
        }

        /// <summary>
        /// 處理 Log Viewer 路徑下的 UI 與 API 請求。
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var basePath = NormalizeBasePath(_settings.ViewerPath);

            if (!IsViewerPath(path, basePath))
            {
                await _next(context);
                return;
            }

            if (!ValidateViewerKey(context))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            var subPath = path.Substring(basePath.Length).TrimStart('/');
            if (string.IsNullOrEmpty(subPath) || subPath.Equals("index.html", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(BuildViewerHtml(basePath), Encoding.UTF8);
                return;
            }

            if (subPath.Equals("api/files", StringComparison.OrdinalIgnoreCase))
            {
                await HandleFilesApiAsync(context);
                return;
            }

            if (subPath.Equals("api/content", StringComparison.OrdinalIgnoreCase))
            {
                await HandleContentApiAsync(context);
                return;
            }

            await _next(context);
        }

        private async Task HandleFilesApiAsync(HttpContext context)
        {
            try
            {
                var files = await _logViewer.ListLogFilesAsync(context.RequestAborted);
                await WriteJsonAsync(context, files);
            }
            catch (Exception ex)
            {
                await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        private async Task HandleContentApiAsync(HttpContext context)
        {
            try
            {
                var query = context.Request.Query;
                var request = new LogReadRequest
                {
                    FileName = query["file"].FirstOrDefault() ?? string.Empty,
                    From = ParseDateTimeOffset(query["from"].FirstOrDefault()),
                    To = ParseDateTimeOffset(query["to"].FirstOrDefault()),
                    TailLines = int.TryParse(query["tailLines"].FirstOrDefault(), out var tailLines)
                        ? tailLines
                        : null
                };

                var result = await _logViewer.ReadLogFileAsync(request, context.RequestAborted);
                await WriteJsonAsync(context, result);
            }
            catch (FileNotFoundException ex)
            {
                await WriteErrorAsync(context, StatusCodes.Status404NotFound, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        private bool ValidateViewerKey(HttpContext context)
        {
            if (!_settings.RequireViewerKey)
                return true;

            var provided = context.Request.Headers["X-Log-Viewer-Key"].FirstOrDefault();
            return !string.IsNullOrWhiteSpace(provided) && provided == _settings.ViewerKey;
        }

        private static DateTimeOffset? ParseDateTimeOffset(string? value)
        {
            return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
        }

        private static string NormalizeBasePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "/logs";

            return path.StartsWith("/", StringComparison.Ordinal) ? path.TrimEnd('/') : "/" + path.TrimEnd('/');
        }

        private static bool IsViewerPath(string path, string basePath)
        {
            return path.Equals(basePath, StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith(basePath + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task WriteJsonAsync(HttpContext context, object value)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(JsonSerializer.Serialize(value, JsonOut), Encoding.UTF8);
        }

        private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            await WriteJsonAsync(context, new { error = message });
        }

        private static string BuildViewerHtml(string basePath)
        {
            return $@"<!DOCTYPE html>
<html lang=""zh-TW"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width,initial-scale=1"">
<link rel=""icon"" href=""data:,"">
<title>Log Viewer</title>
<style>
*{{box-sizing:border-box}}
body{{margin:0;height:100vh;display:flex;overflow:hidden;background:#111827;color:#e5e7eb;font-family:Segoe UI,Arial,sans-serif}}
aside{{width:300px;min-width:260px;background:#0f172a;border-right:1px solid #243044;display:flex;flex-direction:column}}
header{{height:54px;padding:14px 18px;border-bottom:1px solid #243044;font-size:18px;font-weight:700;color:#f9fafb}}
#files{{flex:1;overflow:auto;padding:8px}}
.file{{width:100%;display:block;text-align:left;background:transparent;color:#cbd5e1;border:0;border-left:3px solid transparent;padding:10px 12px;cursor:pointer;font-size:13px;line-height:1.35}}
.file:hover{{background:#172033}}
.file.active{{background:#1f2937;border-left-color:#38bdf8;color:#f8fafc}}
.meta{{display:block;color:#94a3b8;font-size:11px;margin-top:2px}}
main{{flex:1;min-width:0;display:flex;flex-direction:column}}
.toolbar{{min-height:54px;border-bottom:1px solid #243044;background:#111827;padding:9px 14px;display:flex;align-items:center;gap:10px;flex-wrap:wrap}}
label{{font-size:12px;color:#94a3b8}}
input{{background:#0b1220;border:1px solid #334155;color:#e5e7eb;border-radius:6px;padding:6px 8px;font-size:13px}}
button{{background:#1f2937;border:1px solid #334155;color:#e5e7eb;border-radius:6px;padding:7px 12px;cursor:pointer;font-size:13px}}
button:hover{{background:#263244}}
.check{{display:flex;align-items:center;gap:6px;margin-left:auto}}
.check input{{width:16px;height:16px}}
#status{{color:#94a3b8;font-size:12px;padding:0 14px;height:28px;display:flex;align-items:center;border-bottom:1px solid #1f2937}}
#content{{flex:1;overflow:auto;margin:0;padding:14px 16px;background:#0b1020;color:#d1d5db;font:12px/1.55 Consolas,'Cascadia Mono',monospace;white-space:pre-wrap;word-break:break-word}}
@media (max-width:760px){{body{{flex-direction:column}}aside{{width:100%;height:230px}}.check{{margin-left:0}}}}
</style>
</head>
<body>
<aside>
  <header>Log Viewer</header>
  <div id=""files""></div>
</aside>
<main>
  <div class=""toolbar"">
    <label>起</label><input id=""from"" type=""datetime-local"">
    <label>迄</label><input id=""to"" type=""datetime-local"">
    <label>行數</label><input id=""tail"" type=""number"" min=""1"" max=""5000"" value=""500"" style=""width:86px"">
    <button onclick=""loadContent()"">查詢</button>
    <button onclick=""loadFiles()"">重新整理</button>
    <label class=""check""><input id=""auto"" type=""checkbox"" checked>即時</label>
  </div>
  <div id=""status"">尚未載入</div>
  <pre id=""content""></pre>
</main>
<script>
const BASE = '{basePath}';
let selectedFile = null;
let timer = null;

function formatBytes(bytes) {{
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
  return (bytes / 1024 / 1024).toFixed(1) + ' MB';
}}

function toIso(value) {{
  return value ? new Date(value).toISOString() : '';
}}

async function loadFiles() {{
  const response = await fetch(BASE + '/api/files');
  const files = await response.json();
  const list = document.getElementById('files');
  if (!files.length) {{
    list.innerHTML = '<div style=""padding:16px;color:#94a3b8;font-size:13px"">沒有 log 檔</div>';
    document.getElementById('content').textContent = '';
    document.getElementById('status').textContent = '沒有 log 檔';
    return;
  }}

  list.innerHTML = files.map(file => {{
    const active = file.name === selectedFile ? ' active' : '';
    const time = new Date(file.lastWriteTimeUtc).toLocaleString('zh-TW');
    return `<button class=""file${{active}}"" onclick=""selectFile('${{encodeURIComponent(file.name)}}')"">${{file.name}}<span class=""meta"">${{formatBytes(file.sizeBytes)}} · ${{time}}</span></button>`;
  }}).join('');

  if (!selectedFile || !files.some(file => file.name === selectedFile)) {{
    selectFile(encodeURIComponent(files[0].name));
  }}
}}

function selectFile(encodedName) {{
  selectedFile = decodeURIComponent(encodedName);
  document.querySelectorAll('.file').forEach(button => button.classList.remove('active'));
  for (const button of document.querySelectorAll('.file')) {{
    if (button.textContent.startsWith(selectedFile)) button.classList.add('active');
  }}
  loadContent();
}}

async function loadContent() {{
  if (!selectedFile) return;
  const params = new URLSearchParams();
  params.set('file', selectedFile);
  const from = toIso(document.getElementById('from').value);
  const to = toIso(document.getElementById('to').value);
  const tail = document.getElementById('tail').value;
  if (from) params.set('from', from);
  if (to) params.set('to', to);
  if (tail) params.set('tailLines', tail);

  const response = await fetch(BASE + '/api/content?' + params.toString());
  const data = await response.json();
  if (!response.ok) {{
    document.getElementById('content').textContent = data.error || '讀取失敗';
    document.getElementById('status').textContent = '讀取失敗';
    return;
  }}

  document.getElementById('content').textContent = data.content || '';
  document.getElementById('status').textContent = `${{data.fileName}} · ${{data.lineCount}} 行 · ${{new Date().toLocaleTimeString('zh-TW')}}`;
}}

function resetTimer() {{
  if (timer) clearInterval(timer);
  timer = setInterval(() => {{
    if (document.getElementById('auto').checked) loadContent();
  }}, 5000);
}}

document.getElementById('auto').addEventListener('change', resetTimer);
loadFiles();
resetTimer();
</script>
</body>
</html>";
        }
    }
}
