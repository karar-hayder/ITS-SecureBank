using System;
using System.IO;
using Xunit.Abstractions;

namespace Backend.Tests.Common;

public sealed class TestLogger
{
    private readonly ITestOutputHelper _output;
    private static readonly object FileLock = new();

    private static readonly string LogFilePath =
        Path.Combine(GetProjectRoot(), "test_results.log");

    public TestLogger(ITestOutputHelper output)
    {
        _output = output;
    }

    public void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] {message}";

        _output.WriteLine(logMessage);

        lock (FileLock)
        {
            File.AppendAllText(
                LogFilePath,
                logMessage + Environment.NewLine
            );
        }
    }

    public static string GetLogPath() => LogFilePath;

    private static string GetProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Length > 0)
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate project root (.csproj not found)."
        );
    }
}