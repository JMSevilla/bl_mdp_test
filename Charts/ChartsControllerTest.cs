using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Charts;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Charts;
using WTW.MdpService.Infrastructure.Investment;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Test.Domain.Members;

namespace WTW.MdpService.Test.Charts;

public class ChartsControllerTest
{
    private readonly Mock<IChartsTemporaryClient> _chartsTemporaryClientMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IInvestmentServiceClient> _investmentServiceClientMock;
    private readonly Mock<ILogger<ChartsController>> _loggerMock;
    private readonly ChartsController _sut;

    public ChartsControllerTest()
    {
        _chartsTemporaryClientMock = new Mock<IChartsTemporaryClient>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _investmentServiceClientMock = new Mock<IInvestmentServiceClient>();
        _loggerMock = new Mock<ILogger<ChartsController>>();
        _sut = new ChartsController(
            _chartsTemporaryClientMock.Object,
            _investmentServiceClientMock.Object,
            _memberRepositoryMock.Object,
            _loggerMock.Object);

        SetupControllerContext();
    }

    public async Task InvestmentChart_ShouldReturnOk()
    {
        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        var investmentResponse = new InvestmentInternalBalanceResponse
        {
            Currency = "GBP",
            TotalPaidIn = 1000,
            TotalValue = 2000,
            Contributions = new List<InvestmentContributionResponse>()
            {
                new ()
                {
                    Code = "1",
                    Name = "Test",
                    PaidIn = 1000,
                    Value = 2000
                }
            },
            Funds = new List<InvestmentFundResponse>()
            {
                new InvestmentFundResponse
                {
                    Code = "1",
                    Name = "Test",
                    Value = 2000
                }
            }
        };

        _investmentServiceClientMock
            .Setup(x => x.GetInternalBalance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<InvestmentInternalBalanceResponse>.Some(investmentResponse));

        var result = await _sut.InvestmentChart(3);

        result.Should().BeOfType<OkObjectResult>();
    }

    public async Task InvestmentChart_ShouldReturnNotFound()
    {
        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.None);

        var result = await _sut.InvestmentChart(3);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task InvestmentChart_ShouldReturnNoContent()
    {
        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        var investmentResponse = new InvestmentInternalBalanceResponse
        {
            Currency = "GBP",
            TotalPaidIn = 1000,
            TotalValue = 2000,
            Contributions = new List<InvestmentContributionResponse>()
            {
                new ()
                {
                    Code = "1",
                    Name = "Test",
                    PaidIn = 1000,
                    Value = 2000
                }
            },
            Funds = new List<InvestmentFundResponse>()
        };

        _investmentServiceClientMock
            .Setup(x => x.GetInternalBalance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<InvestmentInternalBalanceResponse>.Some(investmentResponse));

        var result = await _sut.InvestmentChart(3);

        result.Should().BeOfType<NoContentResult>();
    }

    public async Task TotalPaidInChart_ShouldReturnOk()
    {
        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        var investmentResponse = new InvestmentInternalBalanceResponse
        {
            Currency = "GBP",
            TotalPaidIn = 1000,
            TotalValue = 2000,
            Contributions = new List<InvestmentContributionResponse>()
            {
                new ()
                {
                    Code = "1",
                    Name = "Test",
                    PaidIn = 1000,
                    Value = 2000
                }
            },
            Funds = new List<InvestmentFundResponse>()
            {
                new InvestmentFundResponse
                {
                    Code = "1",
                    Name = "Test",
                    Value = 2000
                }
            }
        };

        _investmentServiceClientMock
            .Setup(x => x.GetInternalBalance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<InvestmentInternalBalanceResponse>.Some(investmentResponse));

        var result = await _sut.TotalPaidInChart();

        result.Should().BeOfType<OkObjectResult>();
    }

    public async Task ContributionsCountChart_ShouldReturnOk()
    {
        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        var investmentResponse = new InvestmentInternalBalanceResponse
        {
            Currency = "GBP",
            TotalPaidIn = 1000,
            TotalValue = 2000,
            Contributions = new List<InvestmentContributionResponse>()
            {
                new ()
                {
                    Code = "1",
                    Name = "Test",
                    PaidIn = 1000,
                    Value = 2000
                }
            },
            Funds = new List<InvestmentFundResponse>()
            {
                new InvestmentFundResponse
                {
                    Code = "1",
                    Name = "Test",
                    Value = 2000
                }
            }
        };

        _investmentServiceClientMock
            .Setup(x => x.GetInternalBalance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<InvestmentInternalBalanceResponse>.Some(investmentResponse));

        var result = await _sut.ContributionsCountChart();

        result.Should().BeOfType<OkObjectResult>();
    }

    public async Task PortfolioPerformanceChart_ShouldReturnOk()
    {
        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        var investmentResponse = new InvestmentInternalBalanceResponse
        {
            Currency = "GBP",
            TotalPaidIn = 1000,
            TotalValue = 2000,
            Contributions = new List<InvestmentContributionResponse>()
            {
                new ()
                {
                    Code = "1",
                    Name = "Test",
                    PaidIn = 1000,
                    Value = 2000
                }
            },
            Funds = new List<InvestmentFundResponse>()
            {
                new InvestmentFundResponse
                {
                    Code = "1",
                    Name = "Test",
                    Value = 2000
                }
            }
        };

        _investmentServiceClientMock
            .Setup(x => x.GetInternalBalance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<InvestmentInternalBalanceResponse>.Some(investmentResponse));

        var result = await _sut.PortfolioPerformanceChart();

        result.Should().BeOfType<OkObjectResult>();
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
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }
}