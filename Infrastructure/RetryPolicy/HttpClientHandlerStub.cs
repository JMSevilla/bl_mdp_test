using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WTW.MdpService.Test.Infrastructure.RetryPolicy
{
    public class HttpClientHandlerStub : DelegatingHandler
    {
        private readonly Exception _exception;
        private readonly TimeSpan _delay;
        public int Attempts { get; private set; }

        public HttpClientHandlerStub(HttpStatusCode statusCode)
        {
            _exception = new HttpRequestException($"Response status code does not indicate success: {(int)statusCode} ({statusCode}).");
        }

        public HttpClientHandlerStub(Exception exception)
        {
            _exception = exception;
        }

        public HttpClientHandlerStub(TimeSpan delay)
        {
            _delay = delay;
        }

        public async Task<HttpResponseMessage> SendPublicAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await SendAsync(request, cancellationToken);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Attempts++;
            if (_delay != TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken);
            }
            if (_exception != null)
            {
                throw _exception;
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
