using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Polly.Timeout;
using WTW.MdpService.Infrastructure.RetryPolicy;

namespace WTW.MdpService.Test.Infrastructure.RetryPolicy
{
    public class GenericApiPoliciesTest
    {
        public async Task RetryPolicy_ShouldRetryOnTransientHttpError()
        {
            var config = new RetryPolicyOptions { GeneralRetryCount = 3, GeneralRetryDelay = 2 };
            var policy = GenericApiPolicies.RetryPolicy(config);
            var handler = new HttpClientHandlerStub(HttpStatusCode.ServiceUnavailable);
            var httpClient = new HttpClient(handler);

            Func<Task> action = async () => await policy.ExecuteAsync(() => handler.SendPublicAsync(new HttpRequestMessage(), CancellationToken.None));

            await action.Should().ThrowAsync<HttpRequestException>();
            handler.Attempts.Should().Be(config.GeneralRetryCount + 1);
        }

        public async Task RetryPolicy_ShouldRetryOnTimeoutRejectedException()
        {
            var config = new RetryPolicyOptions { GeneralRetryCount = 3, GeneralRetryDelay = 2 };
            var policy = GenericApiPolicies.RetryPolicy(config);
            var handler = new HttpClientHandlerStub(new TimeoutRejectedException());
            var httpClient = new HttpClient(handler);

            Func<Task> action = async () => await policy.ExecuteAsync(() => handler.SendPublicAsync(new HttpRequestMessage(), CancellationToken.None));

            await action.Should().ThrowAsync<TimeoutRejectedException>();
            handler.Attempts.Should().Be(config.GeneralRetryCount + 1);
        }
    }
}