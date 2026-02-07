using System;
using System.IO;
using Xunit.Abstractions;

namespace Backend.Tests.Common;

public class TestLogger
{
    private readonly ITestOutputHelper _output;
    private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_results.log");
    private static readonly object FileLock = new object();

    static TestLogger()
    {
        // Clear log file at start of test run
        if (File.Exists(LogFilePath))
        {
            File.Delete(LogFilePath);
        }
    }

    public TestLogger(ITestOutputHelper output)
    {
        _output = output;
    }

    public void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] {message}";

        // Log to XUnit Output (Console)
        _output.WriteLine(logMessage);

        // Log to File
        lock (FileLock)
        {
            File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
        }
    }

    public static string GetLogPath() => LogFilePath;
}
