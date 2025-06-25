using System;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Moq;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Retirement;

namespace WTW.MdpService.Test.Retirement;

public class RateOfReturnServiceTest
{
    private readonly Mock<ICalculationsClient> _mockCalculationsClient;
    private readonly RateOfReturnService _rateOfReturnService;

    public RateOfReturnServiceTest()
    {
        _mockCalculationsClient = new Mock<ICalculationsClient>();
        _rateOfReturnService = new RateOfReturnService(_mockCalculationsClient.Object);
    }

    public async Task ReturnsError_WhenCalculationsClientReturnsError()
    {
        var error = Error.New("Some error");
        SetupMockClient(Prelude.Left<Error, RetirementResponseV2>(error));

        var result = await _rateOfReturnService.GetRateOfReturn("LIF", "0014400", DateTimeOffset.Now, DateTimeOffset.Now);

        result.IsLeft.Should().BeTrue();
        result.LeftToSeq().First().Should().Be(error);
    }

    public async Task ReturnsNone_WhenClosingBalanceZeroIsY()
    {
        var response = CreateResponse("Y", 0, 0);
        SetupMockClient(Prelude.Right<Error, RetirementResponseV2>(response));

        var result = await _rateOfReturnService.GetRateOfReturn("LIF", "0014400", DateTimeOffset.Now, DateTimeOffset.Now);

        result.IsRight.Should().BeTrue();
        result.RightToSeq().First().IsNone.Should().BeTrue();
    }

    public async Task ReturnsRateOfReturnResponse_WhenValidResponse()
    {
        var response = CreateResponse("N", 5.5m, 1000m);
        SetupMockClient(Prelude.Right<Error, RetirementResponseV2>(response));

        var result = await _rateOfReturnService.GetRateOfReturn("LIF", "0014400", DateTimeOffset.Now, DateTimeOffset.Now);

        result.IsRight.Should().BeTrue();
        var rateOfReturnResponse = result.RightToSeq().First().IfNoneUnsafe(() => null);
        rateOfReturnResponse.Should().NotBeNull();
        rateOfReturnResponse.personalRateOfReturn.Should().Be(5.5m);
        rateOfReturnResponse.changeInValue.Should().Be(1000m);
    }

    private void SetupMockClient(Either<Error, RetirementResponseV2> response)
    {
        _mockCalculationsClient
            .Setup(client => client.RateOfReturn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(response);
    }

    private RetirementResponseV2 CreateResponse(string closingBalanceZero, decimal personalRateOfReturn, decimal changeInValue)
    {
        return new RetirementResponseV2
        {
            Results = new ResultsResponse()
            {
                Mdp = new MdpResponseV2()
                {
                    RateOfReturn = new RateOfReturn()
                    {
                        ClosingBalanceZero = closingBalanceZero,
                        PersonalRateOfReturn = personalRateOfReturn,
                        ChangeInValue = changeInValue
                    }
                }
            }
        };
    }
}
