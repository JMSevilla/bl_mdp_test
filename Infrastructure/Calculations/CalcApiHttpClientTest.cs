using System;
using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using WTW.MdpService.Infrastructure.Calculations;

namespace WTW.MdpService.Test.Infrastructure.Calculations;

public class CalcApiHttpClientTest
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IHostEnvironment> _hostEnvironmentMock;
    private readonly HttpClient _client;
    private readonly HttpClient _transferCalculationClient;
    private readonly CalcApiHttpClient _calcApiHttpClient;

    public CalcApiHttpClientTest()
    {
        _configurationMock = new Mock<IConfiguration>();
        _hostEnvironmentMock = new Mock<IHostEnvironment>();
        _client = new HttpClient();
        _transferCalculationClient = new HttpClient();
        _transferCalculationClient.BaseAddress = new Uri("https://testurl-transfer.awstas.net/");
        _calcApiHttpClient = new CalcApiHttpClient(_client, _transferCalculationClient, _configurationMock.Object, _hostEnvironmentMock.Object);
    }

    public void Client_SetsBaseAddressCorrectly()
    {
        var environmentName = "Development";
        var businessGroup = "TestGroup";

        _hostEnvironmentMock.Setup(h => h.EnvironmentName).Returns(environmentName);
        _configurationMock.Setup(c => c[$"Tenants:{environmentName}:{businessGroup}"]).Returns("testurl");

        var client = _calcApiHttpClient.Client(businessGroup);

        client.BaseAddress.Should().Be(new Uri("https://testurl.awstas.net/"));
    }

    public void TransferClient_SetsBaseAddressCorrectly()
    {
        var environmentName = "Development";
        var businessGroup = "TestGroup";

        _hostEnvironmentMock.Setup(h => h.EnvironmentName).Returns(environmentName);
        _configurationMock.Setup(c => c[$"Tenants:{environmentName}:{businessGroup}"]).Returns("testurl-transfer");

        var transferClient = _calcApiHttpClient.TransferClient(businessGroup);

        transferClient.BaseAddress.Should().Be(new Uri("https://testurl-transfer.awstas.net/"));
    }
}