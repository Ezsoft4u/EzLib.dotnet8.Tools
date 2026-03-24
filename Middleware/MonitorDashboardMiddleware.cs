using EzLib.Models;
using EzLib.Services;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace EzLib.Middleware
{
    public class MonitorDashboardMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MonitorCollectorSettings _settings;
        private readonly IMonitorStorageService _storage;

        private static readonly JsonSerializerOptions _jsonOut = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public MonitorDashboardMiddleware(
            RequestDelegate next,
            MonitorCollectorSettings settings,
            IMonitorStorageService storage)
        {
            _next = next;
            _settings = settings;
            _storage = storage;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";
            var basePath = _settings.DashboardPath.TrimEnd('/');

            if (!path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // API Key 驗證
            if (_settings.RequireDashboardKey)
            {
                var key = context.Request.Headers["X-Monitor-Key"].FirstOrDefault();
                if (key != _settings.DashboardKey)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
            }

            var subPath = path.Substring(basePath.Length).TrimStart('/');

            if (string.IsNullOrEmpty(subPath) || subPath == "index.html")
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(BuildDashboardHtml(basePath), Encoding.UTF8);
                return;
            }

            if (subPath.Equals("api/hosts", StringComparison.OrdinalIgnoreCase))
            {
                await HandleHostsApiAsync(context);
                return;
            }

            if (subPath.StartsWith("api/snapshots", StringComparison.OrdinalIgnoreCase))
            {
                await HandleSnapshotsApiAsync(context);
                return;
            }

            if (subPath.StartsWith("api/export", StringComparison.OrdinalIgnoreCase))
            {
                await HandleExportApiAsync(context);
                return;
            }

            await _next(context);
        }

        private async Task HandleHostsApiAsync(HttpContext context)
        {
            try
            {
                var hosts = await _storage.GetHostNamesAsync();
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(hosts, _jsonOut));
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync($"{{\"error\":\"{ex.Message}\"}}");
            }
        }

        private async Task HandleSnapshotsApiAsync(HttpContext context)
        {
            try
            {
                var (host, from, to) = ParseQueryParams(context);
                var snapshots = await _storage.GetSnapshotsAsync(host, from, to);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(snapshots, _jsonOut));
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync($"{{\"error\":\"{ex.Message}\"}}");
            }
        }

        private async Task HandleExportApiAsync(HttpContext context)
        {
            try
            {
                var (host, from, to) = ParseQueryParams(context);
                var snapshots = await _storage.GetSnapshotsAsync(host, from, to);
                var csv = BuildCsv(snapshots);
                context.Response.ContentType = "text/csv; charset=utf-8";
                var filename = $"monitor_{host ?? "all"}_{from:yyyyMMdd}_{to:yyyyMMdd}.csv";
                context.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{filename}\"";
                await context.Response.WriteAsync(csv, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync($"Error: {ex.Message}");
            }
        }

        private static (string? host, DateTime from, DateTime to) ParseQueryParams(HttpContext context)
        {
            var q = context.Request.Query;
            var host = q["host"].FirstOrDefault();
            var now = DateTime.UtcNow;
            var from = q["from"].FirstOrDefault() is string fs && DateTime.TryParse(fs, out var fv)
                ? fv.ToUniversalTime() : now.AddHours(-1);
            var to = q["to"].FirstOrDefault() is string ts && DateTime.TryParse(ts, out var tv)
                ? tv.ToUniversalTime() : now;
            return (string.IsNullOrEmpty(host) ? null : host, from, to);
        }

        private static string BuildCsv(List<MonitorSnapshotEntity> data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Id,HostName,CollectedAt,OverallStatus,CpuUsagePercent,WorkingSetMB," +
                          "PrivateMemoryMB,ThreadCount,UptimeSeconds,SystemRamTotalMB," +
                          "SystemRamUsedMB,SystemRamUsedPercent,GcTotalHeapMB");
            foreach (var r in data)
            {
                sb.AppendLine($"{r.Id},{Esc(r.HostName)},{r.CollectedAt:o},{Esc(r.OverallStatus)}," +
                              $"{r.CpuUsagePercent:F2},{r.WorkingSetMB:F2},{r.PrivateMemoryMB:F2}," +
                              $"{r.ThreadCount},{r.UptimeSeconds:F0},{r.SystemRamTotalMB:F2}," +
                              $"{r.SystemRamUsedMB:F2},{r.SystemRamUsedPercent:F2},{r.GcTotalHeapMB:F2}");
            }
            return sb.ToString();
        }

        private static string Esc(string s) =>
            s.Contains(',') || s.Contains('"') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;

        private string BuildDashboardHtml(string basePath)
        {
            return $@"<!DOCTYPE html>
<html lang=""zh-TW"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width,initial-scale=1"">
<title>Monitor Dashboard</title>
<script src=""https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js""></script>
<style>
*{{box-sizing:border-box;margin:0;padding:0}}
body{{font-family:'Segoe UI',Arial,sans-serif;background:#0f1117;color:#c9d1d9;display:flex;height:100vh;overflow:hidden}}
#sidebar{{width:220px;min-width:220px;background:#161b22;border-right:1px solid #30363d;display:flex;flex-direction:column;overflow-y:auto}}
#sidebar-header{{padding:16px;font-size:18px;font-weight:700;color:#58a6ff;border-bottom:1px solid #30363d;letter-spacing:.5px}}
#sidebar-header span{{font-size:11px;color:#8b949e;display:block;margin-top:2px;font-weight:400}}
#host-list{{padding:8px 0}}
.host-item{{padding:10px 16px;cursor:pointer;font-size:13px;border-left:3px solid transparent;transition:all .15s}}
.host-item:hover{{background:#1f2937;color:#fff}}
.host-item.active{{background:#1f2937;border-left-color:#58a6ff;color:#58a6ff;font-weight:600}}
#main{{flex:1;display:flex;flex-direction:column;overflow:hidden}}
#topbar{{background:#161b22;border-bottom:1px solid #30363d;padding:10px 20px;display:flex;align-items:center;gap:12px;flex-wrap:wrap}}
#topbar label{{font-size:12px;color:#8b949e}}
#topbar select,#topbar input{{background:#0d1117;border:1px solid #30363d;color:#c9d1d9;padding:5px 10px;border-radius:6px;font-size:13px}}
#topbar select:focus,#topbar input:focus{{outline:none;border-color:#58a6ff}}
#status-badge{{padding:4px 12px;border-radius:20px;font-size:12px;font-weight:600;margin-left:auto}}
.status-Healthy{{background:#1a3a2a;color:#3fb950}}.status-Degraded{{background:#3a2a1a;color:#d29922}}.status-Unhealthy{{background:#3a1a1a;color:#f85149}}
#refresh-btn,#export-btn{{background:#21262d;border:1px solid #30363d;color:#c9d1d9;padding:5px 14px;border-radius:6px;cursor:pointer;font-size:13px;transition:background .15s}}
#refresh-btn:hover,#export-btn:hover{{background:#30363d}}
#content{{flex:1;overflow-y:auto;padding:16px 20px}}
.kpi-row{{display:flex;gap:12px;margin-bottom:16px;flex-wrap:wrap}}
.kpi-card{{flex:1;min-width:150px;background:#161b22;border:1px solid #30363d;border-radius:8px;padding:14px 16px}}
.kpi-card .kpi-label{{font-size:11px;color:#8b949e;text-transform:uppercase;letter-spacing:.5px}}
.kpi-card .kpi-value{{font-size:28px;font-weight:700;color:#c9d1d9;margin-top:4px;line-height:1}}
.kpi-card .kpi-sub{{font-size:11px;color:#8b949e;margin-top:4px}}
.kpi-ok{{color:#3fb950!important}}.kpi-warn{{color:#d29922!important}}.kpi-err{{color:#f85149!important}}
.chart-grid{{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-bottom:16px}}
.chart-card{{background:#161b22;border:1px solid #30363d;border-radius:8px;padding:14px}}
.chart-card canvas{{width:100%!important}}
.chart-full{{grid-column:1/-1}}
.section-title{{font-size:13px;font-weight:600;color:#8b949e;margin-bottom:8px}}
table{{width:100%;border-collapse:collapse;font-size:13px}}
th{{padding:7px 12px;text-align:left;color:#8b949e;font-size:11px;text-transform:uppercase;border-bottom:1px solid #21262d}}
td{{padding:7px 12px;border-bottom:1px solid #21262d}}
tr:hover td{{background:#1c2128}}
.badge{{display:inline-block;padding:2px 8px;border-radius:10px;font-size:11px;font-weight:600}}
.badge-ok{{background:#1a3a2a;color:#3fb950}}.badge-warn{{background:#3a2a1a;color:#d29922}}.badge-err{{background:#3a1a1a;color:#f85149}}
.table-section{{background:#161b22;border:1px solid #30363d;border-radius:8px;padding:14px;margin-bottom:16px}}
#empty-msg{{text-align:center;color:#8b949e;padding:40px;font-size:15px}}
.custom-range{{display:none;gap:8px;align-items:center}}
.custom-range.show{{display:flex}}
</style>
</head>
<body>
<div id=""sidebar"">
  <div id=""sidebar-header"">📊 Monitor<span>EzLib Dashboard</span></div>
  <div id=""host-list""><div style=""padding:20px;color:#8b949e;font-size:12px"">載入中...</div></div>
</div>
<div id=""main"">
  <div id=""topbar"">
    <label>時間範圍</label>
    <select id=""range-select"" onchange=""onRangeChange()"">
      <option value=""1"">最近 1 小時</option>
      <option value=""6"">最近 6 小時</option>
      <option value=""24"" selected>最近 24 小時</option>
      <option value=""168"">最近 7 天</option>
      <option value=""custom"">自訂...</option>
    </select>
    <div class=""custom-range"" id=""custom-range"">
      <input type=""datetime-local"" id=""from-dt"">
      <span style=""color:#8b949e"">→</span>
      <input type=""datetime-local"" id=""to-dt"">
    </div>
    <button id=""refresh-btn"" onclick=""loadData()"">🔄 重新整理</button>
    <button id=""export-btn"" onclick=""exportCsv()"">⬇ 匯出 CSV</button>
    <div id=""status-badge"" class=""status-Healthy"">Healthy</div>
  </div>
  <div id=""content"">
    <div id=""empty-msg"">請從左側選擇主機</div>
  </div>
</div>
<script>
const BASE = '{basePath}';
let currentHost = null;
let cpuChart = null, ramChart = null, memChart = null;

async function loadHosts() {{
  try {{
    const r = await fetch(BASE + '/api/hosts');
    const hosts = await r.json();
    const list = document.getElementById('host-list');
    if (!hosts.length) {{ list.innerHTML='<div style=""padding:20px;color:#8b949e;font-size:12px"">尚無資料</div>'; return; }}
    list.innerHTML = hosts.map(h =>
      `<div class=""host-item"" onclick=""selectHost('${{h}}')"" id=""host-${{encodeURIComponent(h)}}"">🖥 ${{h}}</div>`
    ).join('');
    selectHost(hosts[0]);
  }} catch(e) {{ console.error(e); }}
}}

function selectHost(name) {{
  currentHost = name;
  document.querySelectorAll('.host-item').forEach(el => el.classList.remove('active'));
  const el = document.getElementById('host-' + encodeURIComponent(name));
  if(el) el.classList.add('active');
  loadData();
}}

function onRangeChange() {{
  const v = document.getElementById('range-select').value;
  const cr = document.getElementById('custom-range');
  if(v === 'custom') {{ cr.classList.add('show'); }} else {{ cr.classList.remove('show'); loadData(); }}
}}

function getTimeRange() {{
  const v = document.getElementById('range-select').value;
  const now = new Date();
  if(v === 'custom') {{
    const f = document.getElementById('from-dt').value;
    const t = document.getElementById('to-dt').value;
    return {{ from: f ? new Date(f).toISOString() : new Date(now - 3600000).toISOString(), to: t ? new Date(t).toISOString() : now.toISOString() }};
  }}
  return {{ from: new Date(now - parseInt(v)*3600000).toISOString(), to: now.toISOString() }};
}}

async function loadData() {{
  if(!currentHost) return;
  const {{from, to}} = getTimeRange();
  const url = BASE + '/api/snapshots?host=' + encodeURIComponent(currentHost) + '&from=' + encodeURIComponent(from) + '&to=' + encodeURIComponent(to);
  try {{
    const r = await fetch(url);
    const data = await r.json();
    renderDashboard(data);
  }} catch(e) {{ console.error(e); }}
}}
</script>
<script>
function exportCsv() {{
  if(!currentHost) return;
  const {{from, to}} = getTimeRange();
  const url = BASE + '/api/export?host=' + encodeURIComponent(currentHost) + '&from=' + encodeURIComponent(from) + '&to=' + encodeURIComponent(to) + '&format=csv';
  window.location.href = url;
}}

function renderDashboard(data) {{
  const content = document.getElementById('content');
  if(!data || !data.length) {{
    content.innerHTML = '<div id=""empty-msg"">此時間範圍內沒有資料</div>';
    return;
  }}
  const latest = data[data.length - 1];
  const badge = document.getElementById('status-badge');
  badge.textContent = latest.overallStatus;
  badge.className = 'status-badge status-' + latest.overallStatus;

  const labels = data.map(d => {{
    const dt = new Date(d.collectedAt);
    return dt.toLocaleTimeString('zh-TW', {{hour:'2-digit',minute:'2-digit'}});
  }});
  const cpuVals = data.map(d => +d.cpuUsagePercent.toFixed(2));
  const ramVals = data.map(d => +d.systemRamUsedPercent.toFixed(2));
  const memVals = data.map(d => +d.workingSetMB.toFixed(2));

  let disks = [], dbs = [], https_list = [];
  try {{ disks = latest.disksJson ? JSON.parse(latest.disksJson) : []; }} catch(e){{}}
  try {{ dbs = latest.databasesJson ? JSON.parse(latest.databasesJson) : []; }} catch(e){{}}
  try {{ https_list = latest.httpJson ? JSON.parse(latest.httpJson) : []; }} catch(e){{}}

  const cpuClass = latest.cpuUsagePercent > 80 ? 'kpi-err' : latest.cpuUsagePercent > 60 ? 'kpi-warn' : 'kpi-ok';
  const ramClass = latest.systemRamUsedPercent > 85 ? 'kpi-err' : latest.systemRamUsedPercent > 70 ? 'kpi-warn' : 'kpi-ok';
  const diskOk = disks.every(d => d.status === 'OK' || d.Status === 'OK');
  const dbOk = dbs.every(d => d.isAlive || d.IsAlive);
  const httpOk = https_list.every(h => h.isAlive || h.IsAlive);

  content.innerHTML = `
<div class=""kpi-row"">
  <div class=""kpi-card""><div class=""kpi-label"">CPU 使用率</div><div class=""kpi-value ${{cpuClass}}"">${{latest.cpuUsagePercent.toFixed(1)}}%</div><div class=""kpi-sub"">Process</div></div>
  <div class=""kpi-card""><div class=""kpi-label"">RAM 使用率</div><div class=""kpi-value ${{ramClass}}"">${{latest.systemRamUsedPercent.toFixed(1)}}%</div><div class=""kpi-sub"">${{latest.systemRamUsedMB.toFixed(0)}} / ${{latest.systemRamTotalMB.toFixed(0)}} MB</div></div>
  <div class=""kpi-card""><div class=""kpi-label"">Process 記憶體</div><div class=""kpi-value"">${{latest.workingSetMB.toFixed(1)}} MB</div><div class=""kpi-sub"">Working Set</div></div>
  <div class=""kpi-card""><div class=""kpi-label"">Thread 數</div><div class=""kpi-value"">${{latest.threadCount}}</div><div class=""kpi-sub"">GC Heap: ${{latest.gcTotalHeapMB.toFixed(1)}} MB</div></div>
  <div class=""kpi-card""><div class=""kpi-label"">Disk 狀態</div><div class=""kpi-value ${{diskOk?'kpi-ok':'kpi-err'}}"">${{diskOk?'OK':'⚠ 警告'}}</div><div class=""kpi-sub"">${{disks.length}} 個磁碟</div></div>
  <div class=""kpi-card""><div class=""kpi-label"">DB 狀態</div><div class=""kpi-value ${{dbOk?'kpi-ok':'kpi-err'}}"">${{dbOk?'OK':'⚠ 異常'}}</div><div class=""kpi-sub"">${{dbs.length}} 個連線</div></div>
  <div class=""kpi-card""><div class=""kpi-label"">HTTP 端點</div><div class=""kpi-value ${{httpOk?'kpi-ok':'kpi-err'}}"">${{httpOk?'OK':'⚠ 異常'}}</div><div class=""kpi-sub"">${{https_list.length}} 個端點</div></div>
</div>
<div class=""chart-grid"">
  <div class=""chart-card""><div class=""section-title"">CPU 使用率 (%)</div><canvas id=""cpuChart"" height=""160""></canvas></div>
  <div class=""chart-card""><div class=""section-title"">系統 RAM 使用率 (%)</div><canvas id=""ramChart"" height=""160""></canvas></div>
  <div class=""chart-card chart-full""><div class=""section-title"">Process 記憶體 (MB)</div><canvas id=""memChart"" height=""100""></canvas></div>
</div>
${{disks.length ? renderDisksTable(disks) : ''}}
${{dbs.length ? renderDbTable(dbs) : ''}}
${{https_list.length ? renderHttpTable(https_list) : ''}}
`;
  drawChart('cpuChart', labels, cpuVals, 'CPU %', '#58a6ff', 100);
  drawChart('ramChart', labels, ramVals, 'RAM %', '#3fb950', 100);
  drawChart('memChart', labels, memVals, 'MB', '#d29922', null);
}}
</script>
<script>
function drawChart(id, labels, data, label, color, maxY) {{
  const ctx = document.getElementById(id);
  if(!ctx) return;
  const existing = Chart.getChart(ctx);
  if(existing) existing.destroy();
  new Chart(ctx, {{
    type: 'line',
    data: {{
      labels,
      datasets: [{{ label, data, borderColor: color, backgroundColor: color + '22',
        borderWidth: 2, pointRadius: data.length > 60 ? 0 : 2, fill: true, tension: 0.3 }}]
    }},
    options: {{
      responsive: true, maintainAspectRatio: true,
      scales: {{
        x: {{ ticks: {{ color: '#8b949e', maxTicksLimit: 8, font: {{size:10}} }}, grid: {{ color: '#21262d' }} }},
        y: {{ ticks: {{ color: '#8b949e', font: {{size:10}} }}, grid: {{ color: '#21262d' }},
              min: 0, max: maxY || undefined }}
      }},
      plugins: {{ legend: {{ labels: {{ color: '#c9d1d9', font: {{size:12}} }} }} }}
    }}
  }});
}}

function renderDisksTable(disks) {{
  const rows = disks.map(d => {{
    const status = d.status || d.Status || 'OK';
    const badgeCls = status==='OK'?'badge-ok':status==='Warning'?'badge-warn':'badge-err';
    return `<tr>
      <td>${{d.drive||d.Drive||''}}</td>
      <td>${{d.driveType||d.DriveType||''}}</td>
      <td>${{(d.totalGB||d.TotalGB||0).toFixed(1)}} GB</td>
      <td>${{(d.usedGB||d.UsedGB||0).toFixed(1)}} GB</td>
      <td>${{(d.freeGB||d.FreeGB||0).toFixed(1)}} GB</td>
      <td>${{(d.usedPercent||d.UsedPercent||0).toFixed(1)}}%</td>
      <td><span class=""badge ${{badgeCls}}"">${{status}}</span></td>
    </tr>`;
  }}).join('');
  return `<div class=""table-section""><div class=""section-title"">磁碟狀態</div>
<table><thead><tr><th>磁碟</th><th>類型</th><th>總計</th><th>已用</th><th>剩餘</th><th>使用%</th><th>狀態</th></tr></thead>
<tbody>${{rows}}</tbody></table></div>`;
}}

function renderDbTable(dbs) {{
  const rows = dbs.map(d => {{
    const alive = d.isAlive||d.IsAlive;
    const badgeCls = alive?'badge-ok':'badge-err';
    return `<tr>
      <td>${{d.name||d.Name||''}}</td>
      <td><span class=""badge ${{badgeCls}}"">${{alive?'連線正常':'連線失敗'}}</span></td>
      <td>${{d.responseMs||d.ResponseMs||0}} ms</td>
      <td>${{d.isCached||d.IsCached?'是':'否'}}</td>
      <td style=""color:#f85149"">${{d.errorMessage||d.ErrorMessage||''}}</td>
    </tr>`;
  }}).join('');
  return `<div class=""table-section""><div class=""section-title"">資料庫健康狀態</div>
<table><thead><tr><th>名稱</th><th>狀態</th><th>回應時間</th><th>快取</th><th>錯誤</th></tr></thead>
<tbody>${{rows}}</tbody></table></div>`;
}}

function renderHttpTable(https_list) {{
  const rows = https_list.map(h => {{
    const alive = h.isAlive||h.IsAlive;
    const badgeCls = alive?'badge-ok':'badge-err';
    return `<tr>
      <td>${{h.name||h.Name||''}}</td>
      <td style=""color:#8b949e;font-size:12px"">${{h.url||h.Url||''}}</td>
      <td><span class=""badge ${{badgeCls}}"">${{alive?'正常':'異常'}}</span></td>
      <td>${{h.statusCode||h.StatusCode||'-'}}</td>
      <td>${{h.responseMs||h.ResponseMs||0}} ms</td>
      <td style=""color:#f85149"">${{h.errorMessage||h.ErrorMessage||''}}</td>
    </tr>`;
  }}).join('');
  return `<div class=""table-section""><div class=""section-title"">HTTP 端點狀態</div>
<table><thead><tr><th>名稱</th><th>URL</th><th>狀態</th><th>HTTP Code</th><th>回應時間</th><th>錯誤</th></tr></thead>
<tbody>${{rows}}</tbody></table></div>`;
}}

loadHosts();
setInterval(loadData, 60000);
</script>
</body>
</html>";
        }
    }
}
