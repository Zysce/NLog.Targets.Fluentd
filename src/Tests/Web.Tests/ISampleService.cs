using System.Threading.Tasks;

namespace Web.Tests
{
  public interface ISampleService
  {
    void ExecuteSample(bool includeStack, bool includeProps, bool includeCallerInfo);
    Task ExecuteSampleAsync(bool includeStack, bool includeProps, bool includeCallerInfo);
  }
}