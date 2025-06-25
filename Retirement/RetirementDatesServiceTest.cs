using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Retirement;
using WTW.MdpService.RetirementJourneys;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.FixieConfig;

namespace WTW.MdpService.Test.Retirement;

public class RetirementDatesServiceTest
{
    private readonly Mock<IJourneysRepository> _journeyRepositoryMock;
    private readonly RetirementDatesService _sut;

    public RetirementDatesServiceTest()
    {
        _journeyRepositoryMock = new Mock<IJourneysRepository>();
        _sut = new RetirementDatesService(_journeyRepositoryMock.Object, new RetirementJourneyConfiguration(90));
    }

    private static RetirementDatesAgesDto _retirementDatesAgesDto = new RetirementDatesAgesDto
    {
        EarliestRetirementDate = new DateTime(2027, 3, 5),
        NormalRetirementDate = new DateTime(2032, 3, 31),
        LatestRetirementDate = new DateTime(2047, 3, 5),
        NormalMinimumPensionAge = 60,
        EarliestRetirementAge = 55,
        LatestRetirementAge = 75
    };

    public void ReturnsCorrectTimeToNormalRetirementDateIsoDurationString()
    {
        var result = _sut.GetFormattedTimeUntilNormalRetirement(new MemberBuilder().Build(),
            new RetirementDatesAges(_retirementDatesAgesDto),
            new DateTimeOffset(new DateTime(2023, 8, 11)));

        result.Should().Be("8Y7M2W6D");
    }

    [Input("2022-1-14", "0Y0M0W0D")]
    [Input("2024-1-14", "0Y5M0W3D")]
    [Input(null, "0Y0M0W0D")]
    public void ReturnsCorrectTimeToTargetRetirementDateIsoDurationString(string targetRetirementDate, string expectedResult)
    {
        _retirementDatesAgesDto.TargetRetirementDate = targetRetirementDate == null ? null : new DateTimeOffset(DateTime.Parse(targetRetirementDate));
        var result = _sut.GetFormattedTimeUntilTargetRetirement(new RetirementDatesAges(_retirementDatesAgesDto), new DateTimeOffset(new DateTime(2023, 8, 11)));

        result.Should().Be(expectedResult);
    }

    [Input("DB", "2024-01-11")]
    [Input("DC", "2023-11-09")]
    [Input("DC", null)]
    public async Task GetRetirementApplicationExpiryDate_ReturnsCorrectValue(string scheme, string expectedResult)
    {
        var now = new DateTimeOffset(new DateTime(2023, 10, 13));
        var calculation = new CalculationBuilder()
         .EffectiveRetirementDate(now.AddDays(140).Date)
         .CurrentDate(now)
         .BuildV1();

        if (expectedResult != null)
            _journeyRepositoryMock
                .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, null, now, now.AddDays(27)));

        var result = await _sut.GetRetirementApplicationExpiryDate(calculation, new MemberBuilder().SchemeType(scheme).Build(), now);

        (result?.Date).Should().Be((expectedResult != null ? DateTimeOffset.Parse(expectedResult).Date : null));
    }

    [Input(60, "58-59-60")]
    [Input(70, "63-64-65-66-67-68-69-70")]
    [Input(56, "")]
    public void GetCorrectDcRetirementAgeLines(int targetRetirementAge, string expectedValue)
    {
        var dateOfBirth = new DateTimeOffset(new DateTime(1966, 10, 10));
        var now = new DateTimeOffset(new DateTime(2023, 10, 10));
        var member = new MemberBuilder()
            .DateOfBirth(dateOfBirth)
            .Build();

        var ageLines = _sut.GetAgeLines(
            member.PersonalDetails,
            targetRetirementAge,
            now);

        string.Join("-", ageLines.Select(x => x.Age)).Should().Be(expectedValue);
        if (ageLines.Any())
        {
            ageLines.Select(x => x.IsTargetRetirementAge).TakeLast(1).Single().Should().BeTrue();
            ageLines.Select(x => x.IsTargetRetirementAge).SkipLast(1).All(x => x == true).Should().BeFalse();
        }
    }
}