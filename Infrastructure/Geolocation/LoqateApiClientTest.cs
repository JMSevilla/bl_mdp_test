using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using WTW.MdpService.Infrastructure.Geolocation;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.Geolocation;

public class LoqateApiClientTest
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<LoqateApiClient>> _loggerMock;
    private readonly LoqateApiConfiguration _configuration;
    private readonly LoqateApiClient _sut;

    public LoqateApiClientTest()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<LoqateApiClient>>();
        _configuration = new LoqateApiConfiguration { ApiKey = "test-api-key" };

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.loqate.com/")
        };

        _sut = new LoqateApiClient(httpClient, _configuration, _loggerMock.Object);
    }

    public async Task Find_ShouldReturnAddressSummaryResponse_WhenApiCallIsSuccessful()
    {
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new LocationApiAddressSummaryResponse
            {
                Items = new List<LocationApiAddressSummary>
                {
                    new()
                    {
                        Highlight = "1",
                        Id = "2",
                        Text = "3",
                        Type = "4",
                    }
                }
            }))
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.ToString().Contains("Capture/Interactive/Find/v1.1/json3.ws")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var result = await _sut.Find("text", "container", "en", "US");

        result.IsRight.Should().BeTrue();
        result.Right().IsSuccess.Should().BeTrue();
        result.Right().Items.Single().Highlight.Should().Be("1");
        result.Right().Items.Single().Id.Should().Be("2");
        result.Right().Items.Single().Text.Should().Be("3");
        result.Right().Items.Single().Type.Should().Be("4");
    }

    public async Task Find_ShouldReturnAddressSummaryResponse_WhenApiCallIsFaulted()
    {
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new LocationApiAddressSummaryResponse
            {
                Items = new List<LocationApiAddressSummary>
                {
                    new()
                    {
                        Error = "test-error",
                        Cause = "123",
                        Description = "321",
                    }
                }
            }))
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.ToString().Contains("Capture/Interactive/Find/v1.1/json3.ws")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var result = await _sut.Find("text", "container", "en", "US");

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("Bad request");
    }

    public async Task GetDetails_ShouldReturnSuccessfulResponse_WhenApiCallIsSuccessful()
    {
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new LocationApiAddressDetailsResponse
            {
                Items = new List<LocationApiAddressDetails>
                {
                    new()
                    {
                        Id = "2",
                        Type = "4",
                        City = "5",
                        Line1= "6",
                        Line2= "7",
                        Line3= "8",
                        Line4= "9",
                        Line5= "10",
                        PostalCode= "11",
                        CountryIso2 ="UK",
                    }
                }
            }))
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.ToString().Contains("Capture/Interactive/Retrieve/v1.2/json3.ws")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var result = await _sut.GetDetails("Test-id");

        result.IsRight.Should().BeTrue();
        result.Right().IsSuccess.Should().BeTrue();
        result.Right().Items.Single().Id.Should().Be("2");
        result.Right().Items.Single().Type.Should().Be("4");
        result.Right().Items.Single().City.Should().Be("5");
        result.Right().Items.Single().Line1.Should().Be("6");
        result.Right().Items.Single().Line2.Should().Be("7");
        result.Right().Items.Single().Line3.Should().Be("8");
        result.Right().Items.Single().Line4.Should().Be("9");
        result.Right().Items.Single().Line5.Should().Be("10");
        result.Right().Items.Single().PostalCode.Should().Be("11");
        result.Right().Items.Single().CountryIso2.Should().Be("UK");
    }

    public async Task GetDetails_ShouldReturnError_WhenApiCallIsFaulted()
    {
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new LocationApiAddressDetailsResponse
            {
                Items = new List<LocationApiAddressDetails>
                {
                    new()
                    {
                        Error = "test-error",
                        Cause = "123",
                        Description = "321",
                    }
                }
            }))
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.ToString().Contains("Capture/Interactive/Retrieve/v1.2/json3.ws")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var result = await _sut.GetDetails("test-id");

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("Bad request");
    }
}