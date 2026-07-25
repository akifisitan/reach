using System.Diagnostics;
using System.Text;

var repoRoot = RunGit("rev-parse --show-toplevel").Trim();
var files = RunGit("ls-files")
    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

var utf8Bom = new List<string>();
var utf8 = new List<string>();
var utf16Le = new List<string>();
var utf16Be = new List<string>();
var binary = new List<string>();
var empty = new List<string>();
var unreadable = new List<string>();

var strictUtf8 = new UTF8Encoding(
    encoderShouldEmitUTF8Identifier: false,
    throwOnInvalidBytes: true
);

foreach (var file in files)
{
    byte[] bytes;
    try
    {
        bytes = File.ReadAllBytes(Path.Combine(repoRoot, file));
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        unreadable.Add(file);
        continue;
    }

    if (bytes.Length == 0)
    {
        empty.Add(file);
        continue;
    }

    if (bytes is [0xEF, 0xBB, 0xBF, ..])
    {
        utf8Bom.Add(file);
        continue;
    }

    if (bytes is [0xFF, 0xFE, ..])
    {
        utf16Le.Add(file);
        continue;
    }

    if (bytes is [0xFE, 0xFF, ..])
    {
        utf16Be.Add(file);
        continue;
    }

    try
    {
        strictUtf8.GetString(bytes);
        utf8.Add(file);
    }
    catch (DecoderFallbackException)
    {
        binary.Add(file);
    }
}

Console.WriteLine($"Total tracked files: {files.Length}");
Console.WriteLine();
PrintSection("UTF-8 with BOM", utf8Bom);
PrintSection("UTF-8 without BOM", utf8);
PrintSection("UTF-16 LE with BOM", utf16Le);
PrintSection("UTF-16 BE with BOM", utf16Be);
PrintSection("Binary / not valid UTF-8", binary);
PrintSection("Empty", empty);
PrintSection("Unreadable", unreadable);

static void PrintSection(string title, List<string> files)
{
    Console.WriteLine($"=== {title} ({files.Count}) ===");
    foreach (var file in files)
    {
        Console.WriteLine(file);
    }

    Console.WriteLine();
}

static string RunGit(string arguments)
{
    using var process = Process.Start(
        new ProcessStartInfo("git", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        }
    )!;

    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(
            $"git {arguments} failed with exit code {process.ExitCode}: {process.StandardError.ReadToEnd()}"
        );
    }

    return output;
}
