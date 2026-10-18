using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Reach.Module.SoapSearch.Discovery;
using Reach.Module.SoapSearch.Extraction;

namespace Reach.Module.SoapSearch;

internal sealed class SoapSearcher
{
    private readonly IOptions<SoapSearchOptions> _options;
    private readonly SoapLogExtractor _extractor;

    public SoapSearcher(IOptions<SoapSearchOptions> options, SoapLogExtractor extractor)
    {
        _options = options;
        _extractor = extractor;
    }

    public async Task<SoapSearchResult> Search(
        SoapSearchRequest request,
        CancellationToken cancellationToken = default
    )
    {
        request.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        var settings = _options.Value;
        var errors = new List<SoapSearchError>();
        List<WinApplicationMetadata>? applications = null;
        try
        {
            applications = await CachedMetadataLoader
                .Load(settings.MetadataFilePath, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new(
                null,
                null,
                SoapSearchOutcome.IncompleteSearch,
                [new(settings.MetadataFilePath, ex.Message)]
            );
        }

        var metadata =
            applications.FirstOrDefault(x =>
                x.Name.Equals(request.Application, StringComparison.OrdinalIgnoreCase)
            )
            ?? throw new ArgumentException(
                $"Application '{request.Application}' was not found in the metadata file."
            );
        if (
            request.Servers.Any(server =>
                !metadata.Servers.Contains(server, StringComparer.OrdinalIgnoreCase)
            )
        )
        {
            throw new ArgumentException("Selected servers must belong to the application.");
        }

        var paths = new WinApplicationPathProvider(settings);
        var enumerator = new WinFileEnumerator(settings);
        foreach (var server in request.Servers)
        {
            var roots = new List<string>();
            if (request.Strategy != SoapSearchStrategy.Archive)
            {
                if (string.IsNullOrWhiteSpace(settings.LiveServerBasePath))
                {
                    errors.Add(new(server, "LiveServerBasePath is not configured."));
                }
                else
                {
                    roots.Add(paths.GetLiveServerLogPath(metadata, server));
                }
            }

            if (request.Strategy != SoapSearchStrategy.Live)
            {
                if (
                    string.IsNullOrWhiteSpace(settings.ArchiveServerBasePath)
                    || string.IsNullOrWhiteSpace(metadata.ArchiveDirectoryName)
                )
                {
                    errors.Add(
                        new(
                            server,
                            "Archive root or application archive directory is not configured."
                        )
                    );
                }
                else
                {
                    roots.Add(paths.GetArchiveServerLogPath(metadata, server));
                }
            }

            foreach (var root in roots)
            {
                foreach (
                    var file in enumerator.GetFiles(
                        root,
                        request.StartTime,
                        request.EndTime,
                        errors,
                        cancellationToken
                    )
                )
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var result = file.IsZip
                            ? await SearchZip(file.FilePath, request, enumerator, cancellationToken)
                                .ConfigureAwait(false)
                            : await SearchFile(file.FilePath, request.Siid, cancellationToken)
                                .ConfigureAwait(false);
                        errors.AddRange(result.Errors);
                        if (result.Request is not null)
                        {
                            return result with { Errors = errors };
                        }
                    }
                    catch (Exception ex)
                        when (ex
                                is IOException
                                    or UnauthorizedAccessException
                                    or InvalidDataException
                        )
                    {
                        errors.Add(new(file.FilePath, ex.Message));
                    }
                }
            }
        }

        return new(
            null,
            null,
            errors.Count > 0 ? SoapSearchOutcome.IncompleteSearch : SoapSearchOutcome.NotFound,
            errors
        );
    }

    public async Task<SoapSearchResult> SearchFile(
        string path,
        string siid,
        CancellationToken cancellationToken = default
    )
    {
        var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan
        );
        await using var streamLifetime = stream.ConfigureAwait(false);
        using var reader = new StreamReader(stream, leaveOpen: true);
        return await _extractor
            .Extract(reader, siid, path, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<SoapSearchResult> SearchZip(
        string path,
        SoapSearchRequest request,
        WinFileEnumerator enumerator,
        CancellationToken cancellationToken
    )
    {
        var archive = await ZipFile.OpenReadAsync(path, cancellationToken).ConfigureAwait(false);
        await using var archiveLifetime = archive.ConfigureAwait(false);
        var errors = new List<SoapSearchError>();
        foreach (
            var entry in enumerator.GetZipEntries(
                archive,
                request.StartTime,
                request.EndTime,
                cancellationToken
            )
        )
        {
            var source = $"{path}!{entry.FullName}";
            try
            {
                var stream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
                await using var streamLifetime = stream.ConfigureAwait(false);
                using var reader = new StreamReader(stream, leaveOpen: true);
                var result = await _extractor
                    .Extract(reader, request.Siid, source, cancellationToken)
                    .ConfigureAwait(false);
                errors.AddRange(result.Errors);
                if (result.Request is not null)
                {
                    return result with { Errors = errors };
                }
            }
            catch (Exception ex)
                when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                errors.Add(new(source, ex.Message));
            }
        }

        return new(
            null,
            null,
            errors.Count > 0 ? SoapSearchOutcome.IncompleteSearch : SoapSearchOutcome.NotFound,
            errors
        );
    }
}
