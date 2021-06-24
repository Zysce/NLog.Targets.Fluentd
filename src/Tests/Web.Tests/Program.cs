using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Targets;
using NLog.Web;
using System;

namespace Web.Tests
{
  class Program
  {
    public static void Main(string[] args)
    {
      var logger = NLogBuilder.ConfigureNLog(CreateNLogConfig()).GetCurrentClassLogger();

      try
      {
        logger.Debug("init main");
        CreateHostBuilder(args).Build().Run();
      }
      catch (Exception exception)
      {
        //NLog: catch setup errors
        logger.Error(exception, "Stopped program because of exception");
        throw;
      }
      finally
      {
        // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
        NLog.LogManager.Shutdown();
      }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
              webBuilder.UseStartup<Startup>();
            })
            .ConfigureLogging((HostBuilderContext c, ILoggingBuilder l) =>
            {
              l
                  .ClearProviders()
                  .SetMinimumLevel(LogLevel.Trace);
            })
            .UseNLog(new NLogAspNetCoreOptions
            {
              IncludeScopes = true
            });

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "for test purpose")]
    private static LoggingConfiguration CreateNLogConfig()
    {
      var config = new LoggingConfiguration();
      config.AddTarget("Fluentd", new Fluentd
      {
        EmitStackTraceWhenAvailable = true,
        IncludeAllProperties = true,
        IncludeCallerInfo = true,
        Layout = new NLog.Layouts.SimpleLayout("${longdate}|${level}|${callsite}|${logger}|${message}")
      });
      config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Error, "Fluentd");

      return config;
    }
  }
}
