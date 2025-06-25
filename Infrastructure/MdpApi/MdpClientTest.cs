using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using WTW.MdpService.Infrastructure.MdpApi;
using WTW.Web;

namespace WTW.MdpService.Test.Infrastructure.MdpApi;

public class MdpClientTest
{
    private readonly MdpClient _sut;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<MdpClient>> _loggerMock;
    private readonly Mock<IHttpContextAccessor> _httpMock;

    public MdpClientTest()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<MdpClient>>();
        _httpMock = new Mock<IHttpContextAccessor>();

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("CasesApi"))
            .Returns(new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://casework-api-st01a-dev.awstas.net")
            });

        _httpMock.SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext()
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(MdpConstants.MemberClaimNames.BusinessGroup, "WIF"),
                    new Claim(MdpConstants.MemberClaimNames.ReferenceNumber, "0070007"),
                    new Claim(ClaimTypes.NameIdentifier, "0070007")
            }))
            });

        _sut = new MdpClient(_httpClientFactoryMock.Object.CreateClient("CasesApi"), _loggerMock.Object, _httpMock.Object);
    }

    public async Task GetsMdpClientData_WhenResponseCodeIsOk()
    {
        var uris = new List<Uri> { new Uri("http://www.google.com") };
        var serializedObject = JsonSerializer.Serialize(new { Prop1 = "val1", Prop2 = "val2", Prop3 = "val3" });
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9."
                  + "eyJzdWIiOiJMSUYwMDcwMDA3MTk4MSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6Ik1lbWJlciIsImJ1c2luZXNzX2dyb3VwIjoiTElGIiwicmVmZXJlbmNlX251bWJlciI6IjAwNzAwMDciLCJtYWluX2J1c2luZXNzX2dyb3VwIjoiTElGIiwibWFpbl9yZWZlcmVuY2VfbnVtYmVyIjoiMDA3MDAwNyIsImJlcmVhdmVtZW50X3JlZmVyZW5jZV9udW1iZXIiOiI0NmMzMmZkYy01YzZmLTQ5ODctYWU5Ny04YzNmMTdjYjA1YjYiLCJ0b2tlbl9pZCI6IlJJT3l3UEJQRDJ6VFh1UUJnckpfXzhwUmxUNC4qQUFKVFNRQUNNREVBQWxOTEFCeHJPWE55YUVvMU1XTnJjMWxKS3pkSWF6SkJaWEV5VUM5M1VGazlBQVIwZVhCbEFBTkRWRk1BQWxNeEFBQS4qIiwiZXhwIjoxNzI5MDgwMjc2LCJpc3MiOiJ3dHctbWRwLWF1dGhlbnRpY2F0aW9uLXNlcnZpY2UtZGV2IiwiYXVkIjoid3R3LW1kcC1kZXYifQ."
                  + "4mA7hZG99qCcHoGjVBFGdfwaUWR5wq4HX60uDYxD8PI";
        var response = new HttpResponseMessage
        {
            Content = new StringContent(serializedObject),
            StatusCode = HttpStatusCode.OK
        };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                               "SendAsync",
                               ItExpr.IsAny<HttpRequestMessage>(),
                               ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(response);

        var result = await _sut.GetData(uris, ($"Bearer {token}", "dev2", "LIF"));

        result.Should().NotBeNull();
        JsonSerializer.Serialize(result).Should().Be(serializedObject);
    }

    public async Task GetsNoFromMdpClientData_WhenResponseCodeIsNotOk()
    {
        var uris = new List<Uri> { new Uri("http://www.google.com") };
        var serializedObject = JsonSerializer.Serialize(new { Prop1 = "val1", Prop2 = "val2", Prop3 = "val3" });
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9."
                  + "eyJzdWIiOiJMSUYwMDcwMDA3MTk4MSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6Ik1lbWJlciIsImJ1c2luZXNzX2dyb3VwIjoiTElGIiwicmVmZXJlbmNlX251bWJlciI6IjAwNzAwMDciLCJtYWluX2J1c2luZXNzX2dyb3VwIjoiTElGIiwibWFpbl9yZWZlcmVuY2VfbnVtYmVyIjoiMDA3MDAwNyIsImJlcmVhdmVtZW50X3JlZmVyZW5jZV9udW1iZXIiOiI0NmMzMmZkYy01YzZmLTQ5ODctYWU5Ny04YzNmMTdjYjA1YjYiLCJ0b2tlbl9pZCI6IlJJT3l3UEJQRDJ6VFh1UUJnckpfXzhwUmxUNC4qQUFKVFNRQUNNREVBQWxOTEFCeHJPWE55YUVvMU1XTnJjMWxKS3pkSWF6SkJaWEV5VUM5M1VGazlBQVIwZVhCbEFBTkRWRk1BQWxNeEFBQS4qIiwiZXhwIjoxNzI5MDgwMjc2LCJpc3MiOiJ3dHctbWRwLWF1dGhlbnRpY2F0aW9uLXNlcnZpY2UtZGV2IiwiYXVkIjoid3R3LW1kcC1kZXYifQ."
                  + "4mA7hZG99qCcHoGjVBFGdfwaUWR5wq4HX60uDYxD8PI";
        var response = new HttpResponseMessage
        {
            Content = new StringContent(serializedObject),
            StatusCode = HttpStatusCode.Unauthorized
        };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                               "SendAsync",
                               ItExpr.IsAny<HttpRequestMessage>(),
                               ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(response);

        Func<Task> act = async () => await _sut.GetData(uris, ($"Bearer {token}", "dev2", "LIF"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}