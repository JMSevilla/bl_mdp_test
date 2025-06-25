using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.Investment;
using WTW.MdpService.Infrastructure.Investment.AnnuityBroker;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Investment;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.Helpers;
using WTW.Web.Errors;
using WTW.Web.Serialization;
using static LanguageExt.Prelude;

namespace WTW.MdpService.Test.Investment;

public class InvestmentControllerTest
{
    private readonly InvestmentController _controller;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<ICalculationsRepository> _calculationsRepositoryMock;
    private readonly Mock<IInvestmentServiceClient> _investmentServiceClientMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<ILogger<InvestmentController>> _loggerMock;
    private readonly Mock<IInvestmentQuoteService> _invesmentQuoteServiceMock;

    public InvestmentControllerTest()
    {
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _calculationsRepositoryMock = new Mock<ICalculationsRepository>();
        _investmentServiceClientMock = new Mock<IInvestmentServiceClient>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _loggerMock = new Mock<ILogger<InvestmentController>>();
        _invesmentQuoteServiceMock = new Mock<IInvestmentQuoteService>();

        _controller = new InvestmentController(
            _investmentServiceClientMock.Object,
            _memberRepositoryMock.Object,
            _calculationsRepositoryMock.Object,
            _calculationsParserMock.Object,
            _loggerMock.Object,
            _invesmentQuoteServiceMock.Object);

        SetupControllerContext();
    }

    public async Task GetInternalBalance_ReturnsOkObjectResult_WithValue()
    {
        var member = new MemberBuilder().SchemeType("DC").Build();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _investmentServiceClientMock
            .Setup(x => x.GetInternalBalance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<InvestmentInternalBalanceResponse>.Some(new InvestmentInternalBalanceResponse { TotalValue = 1000 }));

        var result = await _controller.GetInternalBalance();
        result.Should().BeOfType(typeof(OkObjectResult));
        var response = ((OkObjectResult)result).Value as InternalBalanceResponse;
        response.DcBalance.Should().Be(1000);
    }

    public async Task GetInternalBalance_ReturnsOkObjectResult_WithoutValue()
    {
        var member = new MemberBuilder().SchemeType("DC").Build();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _investmentServiceClientMock
            .Setup(x => x.GetInternalBalance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<InvestmentInternalBalanceResponse>.None);

        var result = await _controller.GetInternalBalance();
        result.Should().BeOfType(typeof(OkObjectResult));
        var response = ((OkObjectResult)result).Value as InternalBalanceResponse;
        response.DcBalance.Should().BeNull();
    }

    public async Task GetInternalBalance_ReturnsNotFound_WhenMemberDoesNotExists()
    {
        var errorMessage = "Member not found.";
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.None);

        var result = await _controller.GetInternalBalance();
        result.Should().BeOfType<NotFoundObjectResult>();
        var response = ((NotFoundObjectResult)result).Value as ApiError;
        response.Errors[0].Message.Should().Be(errorMessage);
    }

    public async Task GetInvestmentForecast_WhenCalled_ReturnsOkObjectResult()
    {
        var member = new MemberBuilder().SchemeType("DC").Build();
        var calc = new CalculationBuilder()
            .RetirementDatesAgesJson(TestData.RetiremntDatesAgesJson)
            .BuildV2();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _calculationsParserMock
                .Setup(x => x.GetRetirementDatesAges(It.IsAny<string>()))
                .Returns(new RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options())));
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calc);
        _investmentServiceClientMock
            .Setup(x => x.GetInvestmentForecast(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(Right<Error, InvestmentForecastResponse>(
        new InvestmentForecastResponse
        {
            Ages = new List<InvestmentForecastByAgeResponse>
            {
                new InvestmentForecastByAgeResponse
                {
                    Age = 60,
                    AssetValue = 2000,
                }
            }
        }));

        var result = await _controller.GetInvestmentForecast();
        result.Should().BeOfType(typeof(OkObjectResult));
        var response = ((OkObjectResult)result).Value as ForecastResponse;
        response.DcProjectedBalance.Should().Be(2000);
    }

    public async Task GetInvestmentForecast_ReturnsOkObjectResult_WithoutValue()
    {
        var member = new MemberBuilder().SchemeType("DC").Build();
        var calc = new CalculationBuilder()
            .RetirementDatesAgesJson(TestData.RetiremntDatesAgesJson)
            .BuildV2();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _calculationsParserMock
                .Setup(x => x.GetRetirementDatesAges(It.IsAny<string>()))
                .Returns(new RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options())));
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calc);
        _investmentServiceClientMock
            .Setup(x => x.GetInvestmentForecast(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(Right<Error, InvestmentForecastResponse>(new InvestmentForecastResponse()));

        var result = await _controller.GetInvestmentForecast();
        result.Should().BeOfType(typeof(OkObjectResult));
        var response = ((OkObjectResult)result).Value as ForecastResponse;
        response.DcProjectedBalance.Should().BeNull();
    }

    public async Task GetInvestmentForecast_ReturnsOkObjectResult_WithoutValue_WhenCalcualtionDoesNotExists()
    {
        var member = new MemberBuilder().SchemeType("DC").Build();
        var calc = new CalculationBuilder()
            .RetirementDatesAgesJson(TestData.RetiremntDatesAgesJson)
            .BuildV2();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<Calculation>.None);

        var result = await _controller.GetInvestmentForecast();
        result.Should().BeOfType(typeof(OkObjectResult));
        var response = ((OkObjectResult)result).Value as ForecastResponse;
        response.DcProjectedBalance.Should().BeNull();
    }

    public async Task GetInvestmentForecast_ReturnsNotFound_WhenMemberDoesNotExists()
    {
        var errorMessage = "Member not found.";
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.None);

        var result = await _controller.GetInvestmentForecast();
        result.Should().BeOfType<NotFoundObjectResult>();
        var response = ((NotFoundObjectResult)result).Value as ApiError;
        response.Errors[0].Message.Should().Be(errorMessage);
    }

    public async Task GetLatestContribution_ReturnsContributionsData()
    {
        _memberRepositoryMock
           .Setup(x => x.ExistsMember(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(true);

        _investmentServiceClientMock
            .Setup(x => x.GetLatestContribution(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new WTW.MdpService.Infrastructure.Investment.LatestContributionResponse
            {
                TotalValue = 383.97M,
                Currency = "GBP",
                PaymentDate = DateTime.Parse("2024-10-09T00:00:00"),
                ContributionsList = new List<MdpService.Infrastructure.Investment.Contribution>
                {
                    new MdpService.Infrastructure.Investment.Contribution{ Name = "Employee contribution", ContributionValue=153.59M, RegPayDate=DateTime.Parse("2024-10-09T00:00:00") },
                    new MdpService.Infrastructure.Investment.Contribution{ Name = "Employer contribution", ContributionValue=230.38M, RegPayDate=DateTime.Parse("2023-10-09T00:00:00") }
                }
            });

        var result = await _controller.GetLatestContribution();

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as WTW.MdpService.Investment.LatestContributionResponse;
        response.TotalValue.Should().Be(383.97M);
        response.Currency.Should().Be("GBP");
        response.PaymentDate.Value.Date.Should().Be(DateTime.Parse("2024-10-09T00:00:00").Date);
        response.Contributions[0].Label.Should().Be("Employee contribution");
        response.Contributions[0].Value.Should().Be(153.59M);
        response.Contributions[1].Label.Should().Be("Employer contribution");
        response.Contributions[1].Value.Should().Be(230.38M);
    }

    public async Task GetLatestContribution_Returns404_WhenMemberNotFound()
    {
        var result = await _controller.GetLatestContribution();

        result.Should().BeOfType<NotFoundObjectResult>();
        var response = ((NotFoundObjectResult)result).Value as ApiError;
        response.Errors[0].Message.Should().Be("Member not found.");
        _loggerMock.VerifyLogging("Member not found. Business Group: TestBusinessGroup. Reference Number: TestReferenceNumber.", LogLevel.Error, Times.Once());
    }

    public async Task GetLatestContribution_Returns400_WhenIncestmentServiceCallFailed()
    {
        _memberRepositoryMock
           .Setup(x => x.ExistsMember(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(true);

        _investmentServiceClientMock
            .Setup(x => x.GetLatestContribution(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<WTW.MdpService.Infrastructure.Investment.LatestContributionResponse>.None);

        var result = await _controller.GetLatestContribution();

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    public async Task GetLatestContribution_Returns204_WhenIncestmentServiceReturnsResponseWithNoContributions()
    {
        _memberRepositoryMock
           .Setup(x => x.ExistsMember(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(true);

        _investmentServiceClientMock
            .Setup(x => x.GetLatestContribution(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new WTW.MdpService.Infrastructure.Investment.LatestContributionResponse
            {
                TotalValue = 383.97M,
                Currency = "GBP",
                PaymentDate = DateTime.Parse("2024-10-09T00:00:00"),
                ContributionsList = new()
            });

        var result = await _controller.GetLatestContribution();

        result.Should().BeOfType<NoContentResult>();
        _loggerMock.VerifyLogging("Member has no contributions.", LogLevel.Information, Times.Once());
    }

    public async Task CreateAnnuityQuote_ReturnsNoContent_WhenQuoteIsCreated()
    {
        var calc = new CalculationBuilder()
            .RetirementDatesAgesJson(TestData.RetiremntDatesAgesJson)
            .BuildV2();

        _calculationsRepositoryMock
           .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(calc);

        _calculationsParserMock
             .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
             .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        _invesmentQuoteServiceMock
            .Setup(x => x.CreateAnnuityQuoteRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RetirementV2>()))
            .ReturnsAsync(Right<Error, InvestmentQuoteRequest>(new InvestmentQuoteRequest()));

        _investmentServiceClientMock
            .Setup(x => x.CreateAnnuityQuote(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<InvestmentQuoteRequest>()))
            .ReturnsAsync(Right<Error, Unit>(Unit.Default));


        var result = await _controller.CreateAnnuityQuote();

        result.Should().BeOfType<NoContentResult>();
    }

    public async Task CreateAnnuityQuote_ReturnsBadRequest_WhenCalculationNotFound()
    {
        _calculationsRepositoryMock
           .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(Option<Calculation>.None);

        var result = await _controller.CreateAnnuityQuote();

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    public async Task CreateAnnuityQuote_ReturnsBadRequest_WhenMemberInformationNotFound()
    {
        var calc = new CalculationBuilder()
            .RetirementDatesAgesJson(TestData.RetiremntDatesAgesJson)
            .BuildV2();

        _calculationsRepositoryMock
           .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(calc);

        _calculationsParserMock
             .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
             .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        _invesmentQuoteServiceMock
            .Setup(x => x.CreateAnnuityQuoteRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RetirementV2>()))
            .ReturnsAsync(Left<Error, InvestmentQuoteRequest>(Error.New("Failed to get member details")));

        var result = await _controller.CreateAnnuityQuote();

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    public async Task CreateAnnuityQuote_ReturnsBadRequest_WhenQuoteCreationFails()
    {
        var calc = new CalculationBuilder()
            .RetirementDatesAgesJson(TestData.RetiremntDatesAgesJson)
            .BuildV2();

        _calculationsRepositoryMock
           .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(calc);

        _calculationsParserMock
             .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
             .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        _invesmentQuoteServiceMock
             .Setup(x => x.CreateAnnuityQuoteRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RetirementV2>()))
             .ReturnsAsync(Right<Error, InvestmentQuoteRequest>(new InvestmentQuoteRequest()));

        _investmentServiceClientMock
            .Setup(x => x.CreateAnnuityQuote(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<InvestmentQuoteRequest>()))
            .ReturnsAsync(Left<Error, Unit>(Error.New("Failed to create annuity quote.")));

        var result = await _controller.CreateAnnuityQuote();

        var response = ((ObjectResult)result).Value as ProblemDetails;

        response.Status.Should().Be(500);
    }

    private void SetupControllerContext()
    {
        var id = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("business_group", "TestBusinessGroup"),
            new Claim("reference_number", "TestReferenceNumber"),
        };

        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }
}
