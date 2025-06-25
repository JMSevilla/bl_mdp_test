using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.DcRetirement;
using WTW.MdpService.DcRetirement.Services;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.Investment;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Retirement;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.Web.Errors;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.DcRetirement;

public class DcRetirementControllerTest
{
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<ICalculationsRepository> _calculationsRepositoryMock;
    private readonly Mock<IInvestmentServiceClient> _investmentServiceClientMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<IRetirementDatesService> _retirementDatesServiceMock;
    private readonly Mock<ILogger<DcRetirementController>> _loggerMock;
    private readonly Mock<IDcRetirementService> _dcRetirementServiceMock;
    private readonly DcRetirementController _sut;

    public DcRetirementControllerTest()
    {
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _calculationsRepositoryMock = new Mock<ICalculationsRepository>();
        _investmentServiceClientMock = new Mock<IInvestmentServiceClient>();
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _retirementDatesServiceMock = new Mock<IRetirementDatesService>();
        _loggerMock = new Mock<ILogger<DcRetirementController>>();
        _dcRetirementServiceMock = new Mock<IDcRetirementService>();
        _sut = new DcRetirementController(_memberRepositoryMock.Object,
            _calculationsRepositoryMock.Object,
            _investmentServiceClientMock.Object,
            _calculationsParserMock.Object,
            _dcRetirementServiceMock.Object,
            _retirementDatesServiceMock.Object,
            _loggerMock.Object);

        _sut.SetupControllerContext();
    }

    public async Task GetDcProjectedBalancesReturns400_WhenMemberIsNotDc()
    {
        _memberRepositoryMock
             .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(new MemberBuilder().Build());

        var result = await _sut.GetDcProjectedBalances();

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    public async Task GetDcProjectedBalancesReturns404_WhenCalculationDoesNotExist()
    {
        _memberRepositoryMock
             .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(new MemberBuilder().SchemeType("DC").Build());

        var result = await _sut.GetDcProjectedBalances();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task GetDcProjectedBalancesReturns200_WhenDataIsValid()
    {
        _memberRepositoryMock
             .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(new MemberBuilder().SchemeType("DC").Build());

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Calculation(It.IsAny<string>(), It.IsAny<string>(), TestData.RetiremntDatesAgesJson, TestData.RetirementJsonV2, It.IsAny<DateTime>(), It.IsAny<DateTimeOffset>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<DateTime?>()));

        _investmentServiceClientMock
            .Setup(x => x.GetInvestmentForecast(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new InvestmentForecastResponse { Ages = new List<InvestmentForecastByAgeResponse> { new InvestmentForecastByAgeResponse { Age = 65, AssetValue = 1999.99M } } });

        _calculationsParserMock
                .Setup(x => x.GetRetirementDatesAges(It.IsAny<string>()))
                .Returns(new RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options())));

        _retirementDatesServiceMock
            .Setup(x => x.GetAgeLines(It.IsAny<PersonalDetails>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>()))
            .Returns(new List<(int, bool)> { (65, true) });

        var result = await _sut.GetDcProjectedBalances();

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as DcRetirementProjectedBalancesResponse;
        response.ProjectedBalances.First().Age.Should().Be("65*");
        response.ProjectedBalances.First().AssetValue.Should().Be(1999.99M);
    }

    public async Task GetDcProjectedBalancesReturns500_When()
    {
        _memberRepositoryMock
             .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(new MemberBuilder().SchemeType("DC").Build());

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Calculation(It.IsAny<string>(), It.IsAny<string>(), TestData.RetiremntDatesAgesJson, TestData.RetirementJsonV2, It.IsAny<DateTime>(), It.IsAny<DateTimeOffset>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<DateTime?>()));

        _retirementDatesServiceMock
           .Setup(x => x.GetAgeLines(It.IsAny<PersonalDetails>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>()))
           .Returns(new List<(int, bool)> { (65, true) });

        _calculationsParserMock
               .Setup(x => x.GetRetirementDatesAges(It.IsAny<string>()))
               .Returns(new RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options())));

        _investmentServiceClientMock
            .Setup(x => x.GetInvestmentForecast(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(Option<InvestmentForecastResponse>.None);

        var result = await _sut.GetDcProjectedBalances();

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    public async Task GetDcStrategies200_WhenDataIsValid()
    {
        var request = new InvestmentStrategiesRequest
        {
            SchemeCode = "0022",
            Category = "1117"
        };

        var investmentResponse = new DcSpendingResponse<StrategyContributionTypeResponse>
        {
            ContributionTypes = new List<StrategyContributionTypeResponse>
            {
                new ()
                {
                    ContributionType = "LAVC",
                    Strategies = new List<StrategyResponse>
                    {
                        new ()
                        {
                            Code = "LIFECASH",
                            Name = "Medium Risk Cash"
                        }
                    }
                }
            }
        };

        _investmentServiceClientMock
            .Setup(x => x.GetTargetSchemeMappings(It.IsAny<string>(), request.SchemeCode, request.Category))
            .ReturnsAsync(Option<TargetSchemeMappingResponse>.Some(new TargetSchemeMappingResponse { ContributionType = "LAVC", SchemeCode = "9000" }));

        _investmentServiceClientMock
           .Setup(x => x.GetInvestmentStrategies(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(investmentResponse);

        var result = await _sut.GetDcStrategies(request);

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as DcSpendingResponse<StrategyContributionTypeResponse>;
        response.ContributionTypes.Should().HaveCount(1);
        response.ContributionTypes[0].ContributionType.Should().Be("LAVC");
        response.ContributionTypes[0].Strategies.Should().HaveCount(1);
        response.ContributionTypes[0].Strategies[0].Code.Should().Be("LIFECASH");
    }

    [Input("LIFE", "LifeSight Equity", 0.4105)]
    [Input("LSBO", "LifeSight Bonds", null)]
    public async Task GetDcFunds200_WhenDataIsValid(string code, string name, double? annualMemberFee)
    {
        var request = new InvestmentFundsRequest
        {
            SchemeCode = "0022",
            Category = "1117"
        };

        var investmentResponse = new DcSpendingResponse<FundContributionTypeResponse>
        {
            ContributionTypes = new List<FundContributionTypeResponse>
            {
                new ()
                {
                    ContributionType = "SPEN",
                    Funds = new List<FundResponse>
                    {
                        new ()
                        {
                            Code = code,
                            Name = name,
                            AnnualMemberFee = annualMemberFee != null ? (decimal)annualMemberFee : null,
                        }
                    }
                }
            }
        };

        _investmentServiceClientMock
            .Setup(x => x.GetTargetSchemeMappings(It.IsAny<string>(), request.SchemeCode, request.Category))
            .ReturnsAsync(Option<TargetSchemeMappingResponse>.Some(new TargetSchemeMappingResponse { ContributionType = "SPEN", SchemeCode = "9000" }));

        _investmentServiceClientMock
           .Setup(x => x.GetInvestmentFunds(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(investmentResponse);

        var result = await _sut.GetDcFunds(request);

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as DcSpendingResponse<FundContributionTypeResponse>;
        response.ContributionTypes.Should().HaveCount(1);
        response.ContributionTypes[0].ContributionType.Should().Be("SPEN");
        response.ContributionTypes[0].Funds.Should().HaveCount(1);
        response.ContributionTypes[0].Funds[0].Code.Should().Be(code);
    }

    public async Task ResetQuoteReturns400_WhenNonDcMember()
    {
        _dcRetirementServiceMock
            .Setup(x => x.ResetQuote(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Error.New("error"));

        var result = await _sut.ResetQuote();

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    public async Task ResetQuoteReturns204_WhenNonDcMember()
    {
        Error? error = null;
        _dcRetirementServiceMock
            .Setup(x => x.ResetQuote(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(error);

        var result = await _sut.ResetQuote();

        result.Should().BeOfType<NoContentResult>();
    }

    public async Task GetLifeSightRetirementDateAgeReturns200_WhenDataIsValid()
    {
        _investmentServiceClientMock
            .Setup(x => x.GetInvestmentForecastAge(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new InvestmentForecastAgeResponse { RetirementDate = "2024-11-11", RetirementAge = 59 });

        var result = await _sut.GetLifeSightRetirementDateAge();

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as LifeSighRetirementDateAgeResponse;
        response.DcRetirementDate.Should().Be("2024-11-11");
        response.DcRetirementAge.Should().Be(59);
    }

    public async Task GetLifeSightRetirementDateAgeReturns404_WhenInvestmentForecastAgeReturnsNone()
    {
        _investmentServiceClientMock
            .Setup(x => x.GetInvestmentForecastAge(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<InvestmentForecastAgeResponse>.None);

        var result = await _sut.GetLifeSightRetirementDateAge();

        result.Should().BeOfType<NotFoundObjectResult>();
        var response = ((NotFoundObjectResult)result).Value as ApiError;
        response.Errors[0].Message.Should().Be("Not found");
        response.Errors[0].Code.Should().BeNull();
    }
}