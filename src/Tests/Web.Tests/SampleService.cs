using System.Globalization;
using System.Threading.Tasks;
using Tests.Common;

namespace Web.Tests
{
  public class SampleService : ISampleService
  {
    public void ExecuteSample(bool includeStack, bool includeProps, bool includeCallerInfo)
    {
      using var factory = new FluentdTargetLoggerFactory();
      var logger = factory
        .WithEmitStackTraceWhenAvailable(includeStack)
        .WithIncludeAllProperties(includeProps)
        .WithIncludeCallerInfo(includeCallerInfo)
        .CreateLogger();
      logger.Properties.Add("test", "test");
      logger.Info(CultureInfo.CurrentCulture, "Hello World {sample}!", "sample");
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
        logger.Info(CultureInfo.CurrentCulture, "Hello World {sample}!", "sample");
      });
    }
  }
}
