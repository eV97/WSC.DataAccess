using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace WSC.DataAccess.Configuration;

/// <summary>
/// Configuration helper for iBatis logging setup
/// </summary>
public static class IBatisLoggingConfiguration
{
    /// <summary>
    /// Configures Serilog for iBatis with file logging to log/iBatis directory
    /// </summary>
    /// <param name="logDirectory">Directory for log files (default: log/iBatis)</param>
    /// <param name="minimumLevel">Minimum log level (default: Information)</param>
    /// <returns>Configured ILoggerFactory</returns>
    public static ILoggerFactory ConfigureIBatisLogging(
        string? logDirectory = null,
        LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        logDirectory ??= Path.Combine("log", "iBatis");

        // Ensure log directory exists
        Directory.CreateDirectory(logDirectory);

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logDirectory, "ibatis-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30,
                shared: true)
            .WriteTo.File(
                path: Path.Combine(logDirectory, "ibatis-errors-.log"),
                restrictedToMinimumLevel: LogEventLevel.Warning,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 90,
                shared: true)
            .CreateLogger();

        return LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(Log.Logger, dispose: true);
        });
    }

    /// <summary>
    /// Adds iBatis logging to existing logger factory builder
    /// </summary>
    public static ILoggingBuilder AddIBatisLogging(
        this ILoggingBuilder builder,
        string? logDirectory = null,
        LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        logDirectory ??= Path.Combine("log", "iBatis");

        // Ensure log directory exists
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logDirectory, "ibatis-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30,
                shared: true)
            .WriteTo.File(
                path: Path.Combine(logDirectory, "ibatis-errors-.log"),
                restrictedToMinimumLevel: LogEventLevel.Warning,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 90,
                shared: true)
            .CreateLogger();

        builder.AddSerilog(Log.Logger, dispose: true);

        return builder;
    }
}
