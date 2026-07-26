using System.Runtime.InteropServices;

namespace Reach.Infra.NativeConsole;

public interface IConsoleAttacher
{
    void EnsureAttached();
}

public sealed partial class ConsoleAttacher : IConsoleAttacher
{
    private const uint AttachParentProcess = unchecked((uint)-1);

    public void EnsureAttached()
    {
        if (!OperatingSystem.IsWindows() || GetConsoleWindow() != IntPtr.Zero)
        {
            return;
        }

        if (!AttachConsole(AttachParentProcess) && !AllocConsole())
        {
            throw new InvalidOperationException("Unable to attach or allocate a console.");
        }

        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        Console.SetError(Console.Out);
        Console.SetIn(new StreamReader(Console.OpenStandardInput()));
    }

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(uint dwProcessId);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsole();

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GetConsoleWindow();
}
