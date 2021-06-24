using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;

namespace Tests.Common
{
  public class FluentdTargetLoggerFactory : IDisposable
  {
    private readonly Fluentd _fluentdTarget;
    private bool disposedValue;

    public FluentdTargetLoggerFactory()
    {
      _fluentdTarget = new Fluentd
      {
        Layout = new NLog.Layouts.SimpleLayout("${longdate}|${level}|${callsite}|${logger}|${message}")
      };
    }

    public FluentdTargetLoggerFactory(string layout)
    {
      _fluentdTarget = new Fluentd
      {
        Layout = new NLog.Layouts.SimpleLayout(layout)
      };
    }

    public FluentdTargetLoggerFactory WithIncludeAllProperties(bool include = true)
    {
      _fluentdTarget.IncludeAllProperties = include;
      return this;
    }

    public FluentdTargetLoggerFactory WithEmitStackTraceWhenAvailable(bool emit = true)
    {
      _fluentdTarget.EmitStackTraceWhenAvailable = emit;
      return this;
    }

    public FluentdTargetLoggerFactory WithIncludeCallerInfo(bool include = true)
    {
      _fluentdTarget.IncludeCallerInfo = include;
      return this;
    }

    public FluentdTargetLoggerFactory WithExcludeProperties(ISet<string> excludedProperties)
    {
      _fluentdTarget.ExcludeProperties = excludedProperties;
      return this;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "for demo")]
    public Logger CreateLogger()
    {
      var config = CreateConfig();
      var loggerFactory = new LogFactory(config);
      var logger = loggerFactory.GetLogger("demo");
      return logger;
    }

    private LoggingConfiguration CreateConfig()
    {
      var config = new LoggingConfiguration();

      config.AddTarget("fluentd", _fluentdTarget);
      config.LoggingRules.Add(new LoggingRule("demo", LogLevel.Debug, _fluentdTarget));

      return config;
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          _fluentdTarget?.Dispose();
        }

        // TODO: free unmanaged resources (unmanaged objects) and override finalizer
        // TODO: set large fields to null
        disposedValue = true;
      }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~FluentdTargetLoggerFactory()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }
  }
}
