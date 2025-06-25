using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.TestCommon.Helpers;
using WTW.Web.LanguageExt;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Infrastructure.Calculations;

public class CalculationsClientTest
{
    private readonly Mock<HttpMessageHandler> _mockClientHttpMessageHandler;
    private readonly Mock<HttpMessageHandler> _mockTransferClientHttpMessageHandler;
    private readonly HttpClient _client;
    private readonly HttpClient _transferCalculationClient;
    private readonly Mock<IConfiguration> _configuration;
    private readonly Mock<IHostEnvironment> _hostEnvironment;
    private readonly Mock<ILogger<CalculationsClient>> _loggerMock;
    private readonly CalculationsClient _sut;
    protected Mock<IOptionsSnapshot<CalculationServiceOptions>> _optionsMock;
    public CalculationsClientTest()
    {
        _mockClientHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockTransferClientHttpMessageHandler = new Mock<HttpMessageHandler>();
        _client = new HttpClient(_mockClientHttpMessageHandler.Object);
        _transferCalculationClient = new HttpClient(_mockTransferClientHttpMessageHandler.Object);
        _configuration = new Mock<IConfiguration>();
        _hostEnvironment = new Mock<IHostEnvironment>();
        _loggerMock = new();
        _optionsMock = new Mock<IOptionsSnapshot<CalculationServiceOptions>>();

        var environmentName = "Development";
        var businessGroup = "ABC";

        _hostEnvironment.Setup(h => h.EnvironmentName).Returns(environmentName);
        _configuration.Setup(c => c[$"Tenants:{environmentName}:{businessGroup}"]).Returns("testurl");


        _optionsMock.Setup(m => m.Value)
        .Returns(new CalculationServiceOptions
        {
            BaseUrl = "https://calcapi-dev.awstas.net/",
            CacheExpiresInMs = 10,
            TimeOutInSeconds = 30000,
            GuaranteedQuotesEnabledFor = new List<string>() { "RBS", "ABC" },
            GetGuaranteedQuotesApiPath = "bgroups/{0}/members/{1}/quotations?guaranteeDateFrom={2}&guaranteeDateTo={3}&event={4}&status={5}&pageNumber={6}&pageSize={7}"
        });

        var calcApiHttpClient = new CalcApiHttpClient(_client, _transferCalculationClient, _configuration.Object, _hostEnvironment.Object);
        _sut = new CalculationsClient(_client, _transferCalculationClient, _configuration.Object, _hostEnvironment.Object, _loggerMock.Object, _optionsMock.Object);
    }

    public async Task RetirementDatesAges_ReturnsResponse_OnSuccess()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"lockedInTransferQuoteFileId\":123456, " +
                "\"retirementAges\":{\"normalMinimumPensionAge\":20.00, \"target\":\"20Y12M21D\",\"targetDerivedInteger\":\"20Y12M22D\"}," +
                "\"retirementDates\":{\"normalMinimumPensionDate\":\"2026-12-12\"}}")
            });
        var referenceNumber = "123";
        var businessGroup = "ABC";

        var result = await _sut.RetirementDatesAges(referenceNumber, businessGroup).Try();

        result.IsSuccess.Should().BeTrue();
        result.Value().LockedInTransferQuoteFileId.Should().Be(123456);
    }

    public async Task RetirementDatesAges_ReturnsError_OnFailure()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            });
        var referenceNumber = "123";
        var businessGroup = "ABC";

        var result = await _sut.RetirementDatesAges(referenceNumber, businessGroup).Try();

        result.IsSuccess.Should().BeFalse();
    }

    public async Task RetirementCalculation_ReturnsResponse_OnSuccess()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"calctype\":\"PV\"}")
            });

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("&disableTransferCalc=True&effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(TestData.RetirementResponseJson)
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculation(referenceNumber, businessGroup, date);

        result.IsRight.Should().BeTrue();
        result.Right().EventType.Should().Be("PV");
        result.Right().RetirementResponse.Results.Mdp.DateOfBirth.Should().Be(DateTime.Parse("1962-11-07"));
    }

    public async Task RetirementCalculation_ReturnsError_WhenCalcApiReturnsAnyFatalErrors()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"calctype\":\"PV\"}")
            });

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("&disableTransferCalc=True&effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"errors\":{\"fatals\":[\"fatal-1\",\"fatal-2\"]}}")
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculation(referenceNumber, businessGroup, date);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("fatal-1, fatal-2");
    }

    public async Task RetirementCalculation_ReturnsError_WhenExceptionIsThrown()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new ArgumentException("test-message"));

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculation(referenceNumber, businessGroup, date);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("Result<T> failed");
    }

    public async Task RetirementCalculationOverload_ReturnsResponse_OnSuccess()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"calctype\":\"PV\"}")
            });

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("disableTransferCalc=True&effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(TestData.RetirementResponseJson)
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculation(referenceNumber, businessGroup, date, 20M);

        result.IsRight.Should().BeTrue();
        result.Right().EventType.Should().Be("PV");
        result.Right().RetirementResponse.Results.Mdp.DateOfBirth.Should().Be(DateTime.Parse("1962-11-07"));
    }

    public async Task RetirementCalculationOverload_ReturnsError_WhenCalcApiReturnsAnyFatalErrors()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"calctype\":\"PV\"}")
            });

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("disableTransferCalc=True&effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"errors\":{\"fatals\":[\"fatal-1\",\"fatal-2\"]}}")
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculation(referenceNumber, businessGroup, date, 20M);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("fatal-1, fatal-2");
    }

    public async Task RetirementCalculationOverload_ReturnsError_WhenExceptionIsThrown()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new ArgumentException("test-message"));

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculation(referenceNumber, businessGroup, date, 20M);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("Result<T> failed");
    }

    public async Task RetirementCalculationV2WithLock_ReturnsResponse_OnSuccess()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("disableTransferCalc=True&effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(TestData.RetirementV2ResponseJson)
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculationV2WithLock(referenceNumber, businessGroup, "PV", date, 20M);

        result.IsRight.Should().BeTrue();
        result.Right().Results.Mdp.DateOfBirth.Should().Be(DateTime.Parse("1962-11-07"));
    }

    public async Task RetirementCalculationV2WithLock_ReturnsError_WhenCalcApiReturnsAnyFatalErrors()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("disableTransferCalc=True&effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"errors\":{\"fatals\":[\"fatal-1\",\"fatal-2\"]}}")
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculationV2WithLock(referenceNumber, businessGroup, "PV", date, 20M);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("fatal-1, fatal-2");
    }

    public async Task RetirementCalculationV2WithLock_ReturnsError_WhenExceptionIsThrown()
    {
        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculationV2WithLock(referenceNumber, businessGroup, "PV", date, 20M);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("Handler did not return a response message.");
    }

    public async Task HardQuote_ReturnsResponse_OnSuccess()
    {
        _mockClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/transfer")),
               ItExpr.IsAny<CancellationToken>())
           .Returns((HttpRequestMessage request, CancellationToken cancellationToken) =>
            {
                var stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"calctype\":\"PV\"}"));

                return Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StreamContent(stream)
                });
            });

        _mockTransferClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("engineWriteResults")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent(TestData.TransferQuoteResponseJson)
           });

        _mockTransferClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("letters/default")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new TransferPackResponse { LetterURI = "test-LetterURI", LetterURL = "test-LetterURL" }, SerialiationBuilder.Options()))
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.HardQuote(businessGroup, referenceNumber);

        result.IsRight.Should().BeTrue();
        result.Right().Should().Be("test-LetterURI");
    }

    public async Task HardQuote_ReturnsError_WhenCalcApiReturnsAnyFatalErrors()
    {
        _mockClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/transfer")),
               ItExpr.IsAny<CancellationToken>())
           .Returns((HttpRequestMessage request, CancellationToken cancellationToken) =>
           {
               var stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"calctype\":\"PV\"}"));

               return Task.FromResult(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StreamContent(stream)
               });
           });

        _mockTransferClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("engineWriteResults")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent("{\"errors\":{\"fatals\":[\"fatal-1\",\"fatal-2\"]}}")
           });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.HardQuote(businessGroup, referenceNumber);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("fatal-1, fatal-2");
    }

    public async Task HardQuote_ReturnsError_WhenExceptionIsThrown()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new ArgumentException());

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.HardQuote(businessGroup, referenceNumber);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("Result<T> failed");
    }

    public async Task RetirementQuote_ReturnsResponse_OnSuccess()
    {
        _mockClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent("{\"calctype\":\"PV\"}")
           });

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("disableTransferCalc=True&effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(TestData.RetirementV2ResponseJson)
            });

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("letters/default")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"letterURI\":\"test-LetterURI\",\"letterURL\":\"test-LetterURL\"}")
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        Either<Error, (string, int)> result = await _sut.RetirementQuote(businessGroup, referenceNumber, date);

        result.IsRight.Should().BeTrue();
        result.Right().Item1.Should().Be("test-LetterURI");
    }

    public async Task RetirementQuote_ReturnsError_WhenCalcApiReturnsAnyFatalErrors()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"calctype\":\"PV\"}")
            });

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("disableTransferCalc=True&effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"errors\":{\"fatals\":[\"fatal-1\",\"fatal-2\"]}}")
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementQuote(businessGroup, referenceNumber, date);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("fatal-1, fatal-2");
    }

    public async Task RetirementQuote_ReturnsError_WhenExceptionIsThrown()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
             .ThrowsAsync(new ArgumentException());

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementQuote(businessGroup, referenceNumber, date);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("Result<T> failed");
    }

    public async Task TransferCalculation_ReturnsResponse_OnSuccess()
    {
        _mockClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/transfer")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent("{\"calctype\":\"PV\"}")
           });

        _mockTransferClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("engineWriteResults")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent(TestData.TransferQuoteResponseJson)
           });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.TransferCalculation(businessGroup, referenceNumber);

        result.IsRight.Should().BeTrue();
        result.Right().Results.Mdp.TotalPensionAtDOL.Should().Be(739.0M);
    }

    public async Task TransferCalculation_ReturnsError_WhenCalcApiReturnsAnyFatalErrors()
    {
        _mockClientHttpMessageHandler.Protected()
          .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/transfer")),
              ItExpr.IsAny<CancellationToken>())
          .ReturnsAsync(new HttpResponseMessage
          {
              StatusCode = HttpStatusCode.OK,
              Content = new StringContent("{\"calctype\":\"PV\"}")
          });

        _mockTransferClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("engineWriteResults")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent("{\"errors\":{\"fatals\":[\"fatal-1\",\"fatal-2\"]}}")
           });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.TransferCalculation(businessGroup, referenceNumber);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("fatal-1, fatal-2");
    }

    public async Task TransferCalculation_ReturnsError_WhenExceptionIsThrown()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new ArgumentException());

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.TransferCalculation(businessGroup, referenceNumber);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("Result<T> failed");
    }

    public async Task PensionTranches_ReturnsResponse_OnSuccess()
    {
        _mockClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/partialTransfer")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent("{\"calctype\":\"PV\"}")
           });

        _mockClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("calculations/PV?requestedTransferValue=20")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent(GetPartialTransferResponseJson())
           });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.PensionTranches(businessGroup, referenceNumber, 20);

        result.IsRight.Should().BeTrue();
        AssertPartialTransferResponseMdpResponse(result.Right());
    }

    private void AssertPartialTransferResponseMdpResponse(PartialTransferResponse.MdpResponse mdpResponse)
    {
        mdpResponse.PensionTranchesResidual.Total.Should().Be(1);
        mdpResponse.PensionTranchesResidual.Gmp.Should().Be(2);
        mdpResponse.PensionTranchesResidual.Post97.Should().Be(3);
        mdpResponse.PensionTranchesResidual.Pre97Excess.Should().Be(4);

        mdpResponse.PensionTranchesFull.Total.Should().Be(5);
        mdpResponse.PensionTranchesFull.Pre88Gmp.Should().Be(2);
        mdpResponse.PensionTranchesFull.Post88Gmp.Should().Be(1);
        mdpResponse.PensionTranchesFull.Pre97Excess.Should().Be(3);
        mdpResponse.PensionTranchesFull.Post97.Should().Be(4);

        mdpResponse.TransferValuesFull.Total.Should().Be(1);
        mdpResponse.TransferValuesFull.Gmp.Should().Be(2);
        mdpResponse.TransferValuesFull.Post97.Should().Be(5);
        mdpResponse.TransferValuesFull.NonGuaranteed.Should().Be(3);
        mdpResponse.TransferValuesFull.Pre97Excess.Should().Be(4);

        mdpResponse.TransferValuesPartial.Total.Should().Be(1);
        mdpResponse.TransferValuesPartial.Gmp.Should().Be(2);
        mdpResponse.TransferValuesPartial.Post97.Should().Be(5);
        mdpResponse.TransferValuesPartial.Pre97Excess.Should().Be(4);
    }

    private static string GetPartialTransferResponseJson()
    {
        var partialTransferResponse = new PartialTransferResponse
        {
            Errors = new ErrorsResponse
            {
                Fatals = new List<string>(),
                Warnings = new List<string>(),
            },
            Results = new PartialTransferResponse.ResultsResponse
            {
                Mdp = new PartialTransferResponse.MdpResponse
                {
                    PensionTranchesResidual = new PartialTransferResponse.PensionTranchesResidualResponse { Total = 1, Gmp = 2, Post97 = 3, Pre97Excess = 4 },
                    PensionTranchesFull = new PartialTransferResponse.PensionTranchesFull { Pre88Gmp = 2, Post88Gmp = 1, Pre97Excess = 3, Post97 = 4, Total = 5 },
                    TransferValuesFull = new PartialTransferResponse.TransferValuesFullResponse { Total = 1, Gmp = 2, NonGuaranteed = 3, Pre97Excess = 4, Post97 = 5 },
                    TransferValuesPartial = new PartialTransferResponse.TransferValuesPartialResponse { Total = 1, Gmp = 2, Pre97Excess = 4, Post97 = 5 },
                }
            }
        };

        return JsonSerializer.Serialize(partialTransferResponse, SerialiationBuilder.Options());
    }

    public async Task PensionTranches_ReturnsError_WhenCalcApiReturnsAnyFatalErrors()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/partialTransfer")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"calctype\":\"PV\"}")
            });

        _mockClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("calculations/PV?requestedTransferValue=20")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent("{\"errors\":{\"fatals\":[\"fatal-1\",\"fatal-2\"]}}")
           });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.PensionTranches(businessGroup, referenceNumber, 20);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("fatal-1, fatal-2");
    }

    public async Task PensionTranches_ReturnsError_WhenExceptionIsThrown()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new ArgumentException());

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.PensionTranches(businessGroup, referenceNumber, 20);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("Result<T> failed");
    }

    public async Task TransferValues_ReturnsResponse_OnSuccess()
    {
        _mockClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/partialTransfer")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent("{\"calctype\":\"PV\"}")
           });

        _mockClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("calculations/PV?requestedResidualPension=20")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent(GetPartialTransferResponseJson())
           });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.TransferValues(businessGroup, referenceNumber, 20);

        result.IsRight.Should().BeTrue();
        AssertPartialTransferResponseMdpResponse(result.Right());
    }

    public async Task TransferValues_ReturnsError_WhenCalcApiReturnsAnyFatalErrors()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/partialTransfer")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"calctype\":\"PV\"}")
            });

        _mockClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("calculations/PV?requestedResidualPension=20")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent("{\"errors\":{\"fatals\":[\"fatal-1\",\"fatal-2\"]}}")
           });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.TransferValues(businessGroup, referenceNumber, 20);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("fatal-1, fatal-2");
    }

    public async Task TransferValues_ReturnsError_WhenExceptionIsThrown()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new ArgumentException());

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.TransferValues(businessGroup, referenceNumber, 20);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("Result<T> failed");
    }

    public async Task PartialTransferValues_ReturnsResponse_OnSuccess()
    {
        _mockClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/partialTransfer")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent("{\"calctype\":\"PV\"}")
           });

        _mockClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("calculations/PV?requestedTransferValue=20&requestedResidualPension=30")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent(GetPartialTransferResponseJson())
           });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.PartialTransferValues(businessGroup, referenceNumber, 20, 30);

        result.IsRight.Should().BeTrue();
        AssertPartialTransferResponseMdpResponse(result.Right());
    }

    public async Task PartialTransferValues_ReturnsError_WhenCalcApiReturnsAnyFatalErrors()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/partialTransfer")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"calctype\":\"PV\"}")
            });

        _mockClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("calculations/PV?requestedTransferValue=20&requestedResidualPension=30")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent("{\"errors\":{\"fatals\":[\"fatal-1\",\"fatal-2\"]}}")
           });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.PartialTransferValues(businessGroup, referenceNumber, 20, 30);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("fatal-1, fatal-2");
    }

    public async Task PartialTransferValues_ReturnsError_WhenExceptionIsThrown()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new ArgumentException());

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.PartialTransferValues(businessGroup, referenceNumber, 20, 30);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("Result<T> failed");
    }

    public async Task TransferEventType_ReturnsResponse_OnSuccess()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"calctype\":\"PO\"}")
            });
        var referenceNumber = "123";
        var businessGroup = "ABC";

        var result = await _sut.TransferEventType(businessGroup, referenceNumber).Try();

        result.IsSuccess.Should().BeTrue();
        result.Value().Type.Should().Be("PO");
    }

    public async Task TransferEventType_ReturnsError_OnFailure()
    {
        var referenceNumber = "123";
        var businessGroup = "ABC";

        var result = await _sut.TransferEventType(referenceNumber, businessGroup).Try();

        result.IsSuccess.Should().BeFalse();
    }

    public async Task RetirementCalculationV2_ReturnsResponse_OnSuccess()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"calctype\":\"PV\"}")
            });

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("&disableTransferCalc=True&effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(TestData.RetirementV2ResponseJson)
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculationV2(referenceNumber, businessGroup, date, false);

        result.IsRight.Should().BeTrue();
        result.Right().EventType.Should().Be("PV");
        result.Right().RetirementResponseV2.Results.Mdp.DateOfBirth.Should().Be(DateTime.Parse("1962-11-07"));
    }

    public async Task RetirementCalculationV2_ReturnsError_WhenCalcApiReturnsAnyFatalErrors()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"calctype\":\"PV\"}")
            });

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("&disableTransferCalc=True&effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"errors\":{\"fatals\":[\"fatal-1\",\"fatal-2\"]}}")
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculationV2(referenceNumber, businessGroup, date, false);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("fatal-1, fatal-2");
    }

    public async Task RetirementCalculationV2_ReturnsError_WhenExceptionIsThrown()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new ArgumentException("test-message"));

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculationV2(referenceNumber, businessGroup, date, false);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("Result<T> failed");
    }

    public async Task RetirementCalculationV2WithFactorDate_ReturnsResponse_OnSuccess()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"calctype\":\"PV\"}")
            });

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("factorDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(TestData.RetirementV2ResponseJson)
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculationV2(referenceNumber, businessGroup, date, 20, DateTime.UtcNow);

        result.IsRight.Should().BeTrue();
        result.Right().EventType.Should().Be("PV");
        result.Right().RetirementResponseV2.Results.Mdp.DateOfBirth.Should().Be(DateTime.Parse("1962-11-07"));
    }

    public async Task RetirementCalculationV2WithLumpSum_ReturnsResponse_OnSuccess()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"calctype\":\"PV\"}")
            });

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("disableTransferCalc=True&effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(TestData.RetirementV2ResponseJson)
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculationV2(referenceNumber, businessGroup, date, 20);

        result.IsRight.Should().BeTrue();
        result.Right().EventType.Should().Be("PV");
        result.Right().RetirementResponseV2.Results.Mdp.DateOfBirth.Should().Be(DateTime.Parse("1962-11-07"));
    }

    public async Task RetirementCalculationV2WithLumpSum_ReturnsError_WhenCalcApiReturnsAnyFatalErrors()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/retirement?effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"calctype\":\"PV\"}")
            });

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("disableTransferCalc=True&effectiveDate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"errors\":{\"fatals\":[\"fatal-1\",\"fatal-2\"]}}")
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculationV2(referenceNumber, businessGroup, date, 20);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("fatal-1, fatal-2");
    }

    public async Task RetirementCalculationV2WithLumpSum_ReturnsError_WhenExceptionIsThrown()
    {
        var referenceNumber = "123";
        var businessGroup = "ABC";
        var date = DateTime.UtcNow;

        var result = await _sut.RetirementCalculationV2(referenceNumber, businessGroup, date, 20);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("Result<T> failed");
    }
    public async Task RateOfReturn_ReturnsResponse_OnSuccess()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("calculations/RR?")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(TestData.RetirementV2ResponseJson)
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var startDate = DateTimeOffset.UtcNow.AddYears(-1);
        var effectiveDate = DateTimeOffset.UtcNow;

        var result = await _sut.RateOfReturn(businessGroup, referenceNumber, startDate, effectiveDate);

        result.IsRight.Should().BeTrue();
        result.Right().Results.Mdp.DateOfBirth.Should().Be(DateTime.Parse("1962-11-07"));
    }

    public async Task RateOfReturn_ReturnsError_WhenCalcApiReturnsAnyFatalErrors()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("calculations/RR?")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"errors\":{\"fatals\":[\"fatal-1\",\"fatal-2\"]}}")
            });

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var startDate = DateTimeOffset.UtcNow.AddYears(-1);
        var effectiveDate = DateTimeOffset.UtcNow;

        var result = await _sut.RateOfReturn(businessGroup, referenceNumber, startDate, effectiveDate);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("fatal-1, fatal-2");
    }

    public async Task RateOfReturn_ReturnsError_WhenExceptionIsThrown()
    {
        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("calculations/RR?")),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new ArgumentException("test-message"));

        var referenceNumber = "123";
        var businessGroup = "ABC";
        var startDate = DateTimeOffset.UtcNow.AddYears(-1);
        var effectiveDate = DateTimeOffset.UtcNow;

        var result = await _sut.RateOfReturn(businessGroup, referenceNumber, startDate, effectiveDate);

        _loggerMock.VerifyLogging("test-message", LogLevel.Error, Times.Once());
        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("test-message");
    }
    public async Task GetGuaranteedTransfer_WhenNoLockedInTransferQuote_ReturnsResponse_OnSuccess()
    {
        var businessGroup = "ABC";
        var referenceNumber = "123";
        var retirementDateAgesUri = $"https://testurl.awstas.net/bgroups/{businessGroup}/members/{referenceNumber}";
        var expectedResponse = "test-LetterURI";

        _mockClientHttpMessageHandler.Protected()
          .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/transfer")),
              ItExpr.IsAny<CancellationToken>())
          .ReturnsAsync(new HttpResponseMessage
          {
              StatusCode = HttpStatusCode.OK,
              Content = new StringContent("{\"calctype\":\"PO\"}")
          });

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Equals(retirementDateAgesUri)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new RetirementDatesAgesResponse { HasLockedInTransferQuote = false }, SerialiationBuilder.Options()))
            });

        _mockTransferClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("engineWriteResults")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent(TestData.TransferQuoteResponseJson)
           });

        _mockTransferClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("letters/default")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new TransferPackResponse { LetterURI = "test-LetterURI", LetterURL = "test-LetterURL" }, SerialiationBuilder.Options()))
            });

        var result = await _sut.GetGuaranteedTransfer(businessGroup, referenceNumber);

        result.IsRight.Should().BeTrue();
        result.IfRight(response => response.Should().Be(expectedResponse));
    }

    public async Task GetGuaranteedTransfer_WhenNoLockedInTransferQuote_ReturnsResponse_Fatal()
    {
        var businessGroup = "ABC";
        var referenceNumber = "123";
        var retirementDateAgesUri = $"https://testurl.awstas.net/bgroups/{businessGroup}/members/{referenceNumber}";

        _mockClientHttpMessageHandler.Protected()
          .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/events/transfer")),
              ItExpr.IsAny<CancellationToken>())
          .ReturnsAsync(new HttpResponseMessage
          {
              StatusCode = HttpStatusCode.OK,
              Content = new StringContent("{\"calctype\":\"PO\"}")
          });

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Equals(retirementDateAgesUri)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new RetirementDatesAgesResponse { HasLockedInTransferQuote = false }, SerialiationBuilder.Options()))
            });

        _mockTransferClientHttpMessageHandler.Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("engineWriteResults")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent("{\"errors\":{\"fatals\":[\"fatal-1\",\"fatal-2\"]}}")
           });

        var result = await _sut.GetGuaranteedTransfer(businessGroup, referenceNumber);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("fatal-1, fatal-2");
    }

    public async Task GetGuaranteedTransfer_ReturnsError_WhenExceptionIsThrown()
    {
        var businessGroup = "ABC";
        var referenceNumber = "123";
        var exceptionMessage = "Result<T> failed";

        _mockClientHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException(exceptionMessage));

        var result = await _sut.GetGuaranteedTransfer(businessGroup, referenceNumber);

        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Message.Should().Be(exceptionMessage));
    }

    public async Task GetGuaranteedQuotes_ReturnsResponse_OnSuccess()
    {
        var _stubGetGuaranteedQuoteClientRequest = new GetGuaranteedQuoteClientRequest
        {
            Bgroup = "ABC",
            RefNo = "1234567",
            Event = "retirement",
            GuaranteeDateFrom = DateTimeOffset.UtcNow,
            GuaranteeDateTo = DateTimeOffset.UtcNow,
            PageNumber = 1,
            PageSize = 1,
            QuotationStatus = "GUARANTEED"
        };

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(TestData.GuaranteedQuoteResponse)
            });

        var result = await _sut.GetGuaranteedQuotes(_stubGetGuaranteedQuoteClientRequest);

        result.IsRight.Should().BeTrue();
        result.IfRight(result => result.Should().BeOfType<GetGuaranteedQuoteResponse>());
    }

    public async Task GetGuaranteedQuotesCalledWithNullQuotationStatus_ReturnsResponse_OnSuccess()
    {
        var _stubGetGuaranteedQuoteClientRequest = new GetGuaranteedQuoteClientRequest
        {
            Bgroup = "ABC",
            RefNo = "1234567",
            Event = "retirement",
            GuaranteeDateFrom = DateTimeOffset.UtcNow,
            GuaranteeDateTo = DateTimeOffset.UtcNow,
            PageNumber = 1,
            PageSize = 1
        };

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(TestData.GuaranteedQuoteResponse)
            });

        var result = await _sut.GetGuaranteedQuotes(_stubGetGuaranteedQuoteClientRequest);

        result.IsRight.Should().BeTrue();
        result.IfRight(result => result.Should().BeOfType<GetGuaranteedQuoteResponse>());
    }

    public async Task GetGuaranteedQuotes_ReturnsError_OnFailure()
    {
        var _stubGetGuaranteedQuoteClientRequest = new GetGuaranteedQuoteClientRequest
        {
            Bgroup = "ABC",
            RefNo = "1234567",
            Event = "retirement",
            GuaranteeDateFrom = DateTimeOffset.UtcNow,
            GuaranteeDateTo = DateTimeOffset.UtcNow,
            PageNumber = 1,
            PageSize = 1,
            QuotationStatus = "GUARANTEED"
        };

        _mockClientHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        var result = await _sut.GetGuaranteedQuotes(_stubGetGuaranteedQuoteClientRequest);

        result.IsLeft.Should().BeTrue();
        result.IfLeft(result => result.Should().BeOfType<Error>());
    }
}


