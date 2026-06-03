using System.Text;
using System.Text.RegularExpressions;
using EzLib.Models;

namespace EzLib.Services
{
    /// <summary>
    /// 從檔案系統讀取宿主專案 log 的服務。
    /// </summary>
    public class FileLogViewerService : ILogViewerService
    {
        private static readonly Regex TimestampRegex = new(
            @"^(?<timestamp>\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:\s?(?:Z|[+-]\d{2}:?\d{2}))?)",
            RegexOptions.Compiled);

        private readonly LogViewerSettings _settings;
        private readonly string _logDirectory;

        public FileLogViewerService(LogViewerSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logDirectory = ResolveLogDirectory(settings.LogDirectory);
        }

        /// <summary>
        /// 列出宿主專案設定目錄中的 log 檔，依最後修改時間新到舊排序。
        /// </summary>
        public Task<IReadOnlyList<LogViewerFile>> ListLogFilesAsync(CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(_logDirectory))
                return Task.FromResult<IReadOnlyList<LogViewerFile>>(Array.Empty<LogViewerFile>());

            var pattern = string.IsNullOrWhiteSpace(_settings.FileSearchPattern)
                ? "*"
                : _settings.FileSearchPattern;

            var files = Directory
                .EnumerateFiles(_logDirectory, pattern, SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ThenBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
                .Select(file => new LogViewerFile
                {
                    Name = file.Name,
                    SizeBytes = file.Length,
                    LastWriteTimeUtc = file.LastWriteTimeUtc
                })
                .ToList();

            return Task.FromResult<IReadOnlyList<LogViewerFile>>(files);
        }

        /// <summary>
        /// 讀取指定 log 檔，並用每筆 log 開頭的時間戳套用起訖篩選。
        /// </summary>
        public async Task<LogReadResult> ReadLogFileAsync(LogReadRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var path = ResolveLogFilePath(request.FileName);
            if (!File.Exists(path))
                throw new FileNotFoundException("Log file was not found.", request.FileName);

            var lines = new List<string>();
            var currentEntry = new List<string>();
            DateTimeOffset? currentTimestamp = null;

            await using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync(cancellationToken) ?? string.Empty;
                if (TryParseTimestamp(line, out var timestamp))
                {
                    FlushEntry(lines, currentEntry, currentTimestamp, request.From, request.To);
                    currentEntry.Clear();
                    currentTimestamp = timestamp;
                }

                currentEntry.Add(line);
            }

            FlushEntry(lines, currentEntry, currentTimestamp, request.From, request.To);

            var limitedLines = ApplyTailLimit(lines, request.TailLines);
            return new LogReadResult
            {
                FileName = Path.GetFileName(path),
                Content = string.Join(Environment.NewLine, limitedLines),
                LineCount = limitedLines.Count
            };
        }

        private static string ResolveLogDirectory(string configuredDirectory)
        {
            var directory = string.IsNullOrWhiteSpace(configuredDirectory)
                ? "logs"
                : configuredDirectory;

            return Path.IsPathRooted(directory)
                ? Path.GetFullPath(directory)
                : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), directory));
        }

        private string ResolveLogFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new InvalidOperationException("Log file name is required.");

            if (fileName.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0 ||
                fileName != Path.GetFileName(fileName))
            {
                throw new InvalidOperationException("Log file name cannot contain a path.");
            }

            var fullPath = Path.GetFullPath(Path.Combine(_logDirectory, fileName));
            var root = _logDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Log file must be inside the configured log directory.");

            return fullPath;
        }

        private static bool TryParseTimestamp(string line, out DateTimeOffset timestamp)
        {
            var match = TimestampRegex.Match(line);
            if (!match.Success)
            {
                timestamp = default;
                return false;
            }

            var raw = match.Groups["timestamp"].Value.Replace("T", " ", StringComparison.Ordinal);
            return DateTimeOffset.TryParse(raw, out timestamp);
        }

        private static void FlushEntry(
            List<string> output,
            List<string> entry,
            DateTimeOffset? timestamp,
            DateTimeOffset? from,
            DateTimeOffset? to)
        {
            if (entry.Count == 0)
                return;

            var hasRange = from.HasValue || to.HasValue;
            var include = timestamp.HasValue
                ? (!from.HasValue || timestamp.Value >= from.Value) &&
                  (!to.HasValue || timestamp.Value <= to.Value)
                : !hasRange;

            if (include)
                output.AddRange(entry);
        }

        private List<string> ApplyTailLimit(List<string> lines, int? requestedTailLines)
        {
            var maxLines = _settings.MaxTailLines <= 0 ? 5000 : _settings.MaxTailLines;
            var defaultLines = _settings.DefaultTailLines <= 0 ? 500 : _settings.DefaultTailLines;
            var tailLines = Math.Clamp(requestedTailLines ?? defaultLines, 1, maxLines);

            return lines.Count <= tailLines
                ? lines
                : lines.Skip(lines.Count - tailLines).ToList();
        }
    }
}
