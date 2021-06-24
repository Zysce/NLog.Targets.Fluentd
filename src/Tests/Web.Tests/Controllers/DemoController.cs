using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Tests.Common;

namespace Web.Tests.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class DemoController : ControllerBase
  {
    private readonly ISampleService service;

    public DemoController(ISampleService service)
    {
      this.service = service;
    }

    [HttpGet]
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

    [HttpGet("ioc")]
    public IActionResult GetFromIoc(bool includeStack = true, bool includeProps = true, bool includeCallerInfo = true)
    {
      service.ExecuteSample(includeStack, includeProps, includeCallerInfo);

      return Ok();
    }

    [HttpGet("iocasync")]
    public async Task<IActionResult> GetFromIocAsync(bool includeStack = true, bool includeProps = true, bool includeCallerInfo = true)
    {
      await service.ExecuteSampleAsync(includeStack, includeProps, includeCallerInfo).ConfigureAwait(false);

      return Ok();
    }
  }
}