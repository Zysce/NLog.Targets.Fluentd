using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Tests.Common;

namespace Web.Tests.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class TestController : ControllerBase
  {
    private readonly ISampleService service;

    public TestController(ISampleService service)
    {
      this.service = service;
    }

    [HttpGet("nlog")]
    public IActionResult Get()
    {
      using (var factory = new FluentdTargetLoggerFactory())
      {
        var logger = factory
          .WithEmitStackTraceWhenAvailable()
          .WithIncludeAllProperties()
          .WithIncludeCallerInfo()
          .CreateLogger();
        logger.Properties.Add("test", "test");
        logger.Info("Hello World!");
      }

      return Ok();
    }

    [HttpGet("nlog/ioc")]
    public IActionResult GetFromIoc(bool includeStack = true, bool includeProps = true, bool includeCallerInfo = true)
    {
      service.ExecuteSample(includeStack, includeProps, includeCallerInfo);

      return Ok();
    }

    [HttpGet("nlog/ioc/async")]
    public async Task<IActionResult> GetFromIocAsync(bool includeStack = true, bool includeProps = true, bool includeCallerInfo = true)
    {
      await service.ExecuteSampleAsync(includeStack, includeProps, includeCallerInfo).ConfigureAwait(false);

      return Ok();
    }

    [HttpGet("log")]
    public IActionResult TestFromLogger()
    {
      service.ExecuteLogSample();

      return Ok();
    }

    [HttpGet("log/async")]
    public async Task<IActionResult> TestFromLoggerAsync()
    {
      await service.ExecuteLogSampleAsync().ConfigureAwait(false);

      return Ok();
    }
  }
}