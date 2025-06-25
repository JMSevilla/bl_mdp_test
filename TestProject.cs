using Fixie;
using WTW.TestCommon.FixieConfig;

namespace WTW.MdpService.Test
{
    public class TestProject : ITestProject
    {
        public void Configure(TestConfiguration configuration, TestEnvironment environment)
        {
            configuration.Conventions.Add<TestDiscovery, TestExecution>();
        }
    }
}