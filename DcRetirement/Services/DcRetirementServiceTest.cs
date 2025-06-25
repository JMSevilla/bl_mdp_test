using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.DcRetirement.Services;
using WTW.MdpService.DcRetirement;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.Investment;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WTW.MdpService.Domain.Bereavement;
using WTW.MdpService.Test.Domain.Members;
using FluentAssertions;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using System;
using WTW.MdpService.Domain.Mdp.Calculations;

namespace WTW.MdpService.Test.DcRetirement.Services;

public class DcRetirementServiceTest
{
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<ICalculationsRepository> _calculationsRepositoryMock;
    private readonly Mock<IMdpUnitOfWork> _mdpUnitOfWorkMock;
    private readonly Mock<ILogger<DcRetirementService>> _loggerMock;
    private readonly DcRetirementService _sut;

    public DcRetirementServiceTest()
    {
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _calculationsRepositoryMock = new Mock<ICalculationsRepository>();
        _mdpUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _loggerMock = new Mock<ILogger<DcRetirementService>>();
        _sut = new DcRetirementService(_memberRepositoryMock.Object,
            _calculationsRepositoryMock.Object,
            _mdpUnitOfWorkMock.Object,
            _loggerMock.Object);
    }

    public async Task ResetQuoteReturnsError()
    {
        var member = new MemberBuilder().SchemeType("DB").Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(member);

        var result = await _sut.ResetQuote(It.IsAny<string>(), It.IsAny<string>());

        result.Value.Message.Should().Be("Method supports only DC members");
    }

    public async Task ResetQuoteReturnsNoError()
    {
        var now = DateTimeOffset.UtcNow;
        var member = new MemberBuilder().SchemeType("DC").Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(member);

        var calculation = new CalculationBuilder()
           .EffectiveRetirementDate(now.AddDays(60).Date)
           .CurrentDate(now)
           .BuildV2();

        _calculationsRepositoryMock
          .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
          .ReturnsAsync(calculation);

        var result = await _sut.ResetQuote(It.IsAny<string>(), It.IsAny<string>());

        result.HasValue.Should().BeFalse();
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }
}
