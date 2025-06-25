using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt.Common;
using Moq;
using Moq.Protected;
using WTW.MdpService.Infrastructure.JobScheduler;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.JobScheduler;

public class JobSchedulerClientTest
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly JobSchedulerClient _sut;
    private readonly string _userName = "testUser";
    private readonly string _password = "testPassword";

    public JobSchedulerClientTest()
    {
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new System.Uri("http://test.com/")
        };
        _sut = new JobSchedulerClient(_httpClient, _userName, _password);
    }

    public async Task CanLogin()
    {
        var loginResponse = new LoginResponse { AccessToken = "test-token" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(loginResponse))
        };
        _handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.ToString().Contains("/joc/api/security/login")),
              ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(response);

        var result = await _sut.Login();

        result.IsRight.Should().BeTrue();
        result.Right().Should().BeEquivalentTo(loginResponse);
    }

    public async Task LoginFails()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(JsonSerializer.Serialize(Error.New("Test-message")))
        };
        _handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.ToString().Contains("/joc/api/security/login")),
              ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(response);

        var result = await _sut.Login();

        result.IsRight.Should().BeFalse();
    }

    public async Task CanLogout()
    {
        var loginResponse = new LoginResponse { AccessToken = "test-token" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(loginResponse))
        };
        _handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.ToString().Contains("joc/api/security/logout")),
              ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(response);

        var result = await _sut.Logout("test-token");

        result.IsRight.Should().BeTrue();
    }

    public async Task LogoutFails()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(JsonSerializer.Serialize(Error.New("Test-message")))
        };
        _handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.ToString().Contains("joc/api/security/logout")),
              ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(response);

        var result = await _sut.Logout("test-token");

        result.IsRight.Should().BeFalse();
    }

    public async Task ChecksOrderStatus()
    {
        var orderStatusResponse = new OrderStatusResponse
        {
            DeliveryDate = System.DateTime.Now,
            History = new()
            {
                new()
                {
                   EndTime = System.DateTime.Now,
                   StartTime = System.DateTime.Now,
                   SurveyDate = System.DateTime.Now,
                   HistoryId = "1",
                   JobChain = "2",
                   JobschedulerId = "2",
                   Node = "2",
                   OrderId = "2",
                   Path = "2",
                   State = new()
                   {
                       Text = "2",
                       Severity = 2,
                   }
                }
            }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(orderStatusResponse))
        };
        _handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.ToString().Contains("joc/api/orders/history")),
              ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(response);

        var result = await _sut.CheckOrderStatus(OrderRequest.OrderStatusRequest(It.IsAny<string>(), It.IsAny<string>()), "test-token");

        result.IsRight.Should().BeTrue();
        result.Right().Should().BeEquivalentTo(orderStatusResponse);
    }

    public async Task CheckOrderStatusFails()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(JsonSerializer.Serialize(Error.New("Test-message")))
        };
        _handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.ToString().Contains("joc/api/orders/history")),
              ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(response);
        var request = OrderRequest.OrderStatusRequest(It.IsAny<string>(), It.IsAny<string>());
        request.Orders[0].AuditLog = new("Test");

        var result = await _sut.CheckOrderStatus(request, "test-token");

        result.IsRight.Should().BeFalse();
    }

    public async Task CreatesOrder()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };
        _handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.ToString().Contains("joc/api/orders/add")),
              ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(response);

        var result = await _sut.CreateOrder(OrderRequest.OrderStatusRequest(It.IsAny<string>(), It.IsAny<string>()), "test-token");

        result.IsRight.Should().BeTrue();
    }

    public async Task CreateOrderFails()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(JsonSerializer.Serialize(Error.New("Test-message")))
        };
        _handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.ToString().Contains("joc/api/orders/add")),
              ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(response);
        var request = OrderRequest.OrderStatusRequest(It.IsAny<string>(), It.IsAny<string>());

        var result = await _sut.CreateOrder(request, "test-token");

        result.IsRight.Should().BeFalse();
    }
}