using System.Globalization;
using System.IO.Compression;

namespace Reach.Module.SoapSearch.Discovery;

internal sealed record FileEntry(string FilePath, bool IsZip);

// Adapted from Serara's WinFileEnumerator and ZipFileSearcher.GetSortedEntries.
// Time bounds select candidate files, not individual log events.
internal sealed class WinFileEnumerator
{
    private readonly SoapSearchOptions _options;

    public WinFileEnumerator(SoapSearchOptions options)
    {
        _options = options;
    }

    public List<FileEntry> GetFiles(
        string rootDirectoryPath,
        DateTimeOffset searchStartTime,
        DateTimeOffset searchEndTime,
        List<SoapSearchError> errors,
        CancellationToken cancellationToken = default
    )
    {
        var result = new List<FileEntry>();
        var directories = new Stack<string>();
        directories.Push(rootDirectoryPath);
        var enumerationOptions = new EnumerationOptions
        {
            IgnoreInaccessible = false,
            AttributesToSkip =
                FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
        };

        while (directories.TryPop(out var directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var logs = new List<FileInfo>();
            try
            {
                foreach (
                    var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos(
                        "*",
                        enumerationOptions
                    )
                )
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (entry is DirectoryInfo child)
                    {
                        directories.Push(child.FullName);
                    }
                    else if (entry is FileInfo file)
                    {
                        if (file.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase))
                        {
                            logs.Add(file);
                        }
                        else if (
                            file.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)
                            && DateTimeOffset.TryParseExact(
                                Path.GetFileNameWithoutExtension(file.Name),
                                "dd.MM.yyyy",
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out var date
                            )
                            && searchStartTime.Date <= date.Date
                            && date.Date <= searchEndTime.Date
                        )
                        {
                            result.Add(new(file.FullName, true));
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add(new(directory, ex.Message));
            }

            foreach (var group in logs.GroupBy(x => GroupName(x.Name)))
            {
                var sorted = group.OrderBy(x => x.LastWriteTime).ToList();
                var creation = DateTimeOffset.MinValue;
                foreach (var file in sorted)
                {
                    if (searchStartTime <= file.LastWriteTime && creation <= searchEndTime)
                    {
                        result.Add(new(file.FullName, false));
                    }

                    creation = new DateTimeOffset(file.LastWriteTime).AddSeconds(
                        -_options.CreationTimeBufferInSeconds
                    );
                }
            }
        }

        return result;
    }

    public List<ZipArchiveEntry> GetZipEntries(
        ZipArchive archive,
        DateTimeOffset searchStartTime,
        DateTimeOffset searchEndTime,
        CancellationToken cancellationToken = default
    )
    {
        var groups = new Dictionary<string, List<ZipArchiveEntry>>();
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = entry.FullName.Replace('\\', '/');
            if (path.EndsWith('/'))
            {
                continue;
            }

            var key = Path.Combine(
                Path.GetDirectoryName(path) ?? string.Empty,
                GroupName(Path.GetFileName(path))
            );
            if (!groups.TryGetValue(key, out var group))
            {
                group = [];
                groups.Add(key, group);
            }

            group.Add(entry);
        }

        var result = new List<ZipArchiveEntry>();
        foreach (var group in groups.Values)
        {
            var creation = DateTimeOffset.MinValue;
            foreach (var entry in group.OrderBy(x => x.LastWriteTime))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (
                    Path.GetExtension(entry.FullName)
                        .Equals(".log", StringComparison.OrdinalIgnoreCase)
                    && searchStartTime <= entry.LastWriteTime
                    && creation <= searchEndTime
                )
                {
                    result.Add(entry);
                }

                creation = entry.LastWriteTime.AddSeconds(-_options.ZipCreationTimeBufferInSeconds);
            }
        }

        return result;
    }

    private static string GroupName(string name)
    {
        var dot = name.IndexOf('.');
        return dot < 0 ? name : name[..dot];
    }
}
