using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Threading.Tasks;
using Tests.Common;

namespace Web.Tests
{
  public class SampleService : ISampleService
  {
    private readonly ILogger<SampleService> _log;

    public SampleService(ILogger<SampleService> log)
    {
      _log = log;
    }

    public void ExecuteSample(bool includeStack, bool includeProps, bool includeCallerInfo)
    {
      using var factory = new FluentdTargetLoggerFactory();
      var logger = factory
        .WithEmitStackTraceWhenAvailable(includeStack)
        .WithIncludeAllProperties(includeProps)
        .WithIncludeCallerInfo(includeCallerInfo)
        .CreateLogger();
      logger.Properties.Add("test", "test");
      logger.Info(CultureInfo.CurrentCulture, "Hello World Sample {sample}!", "sample");
    }

    public Task ExecuteSampleAsync(bool includeStack, bool includeProps, bool includeCallerInfo)
    {
      return Task.Run(() =>
      {
        using var factory = new FluentdTargetLoggerFactory();
        var logger = factory
          .WithEmitStackTraceWhenAvailable(includeStack)
          .WithIncludeAllProperties(includeProps)
          .WithIncludeCallerInfo(includeCallerInfo)
          .CreateLogger();
        logger.Properties.Add("test", "test");
        logger.Info(CultureInfo.CurrentCulture, "Hello World Sample Async {sample}!", "sample");
      });
    }

    public void ExecuteLogSample()
    {
      _log.LogInformation("Hello World Log Sample {sample}!", "sample");
    }

    public Task ExecuteLogSampleAsync()
    {
      return Task.Run(() =>
      {
        _log.LogInformation("Hello World Log Sample Async {sample}!", "sample");
      });
    }
  }
}
