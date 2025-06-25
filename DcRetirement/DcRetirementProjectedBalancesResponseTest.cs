using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using WTW.MdpService.DcRetirement;
using WTW.MdpService.Infrastructure.Investment;

namespace WTW.MdpService.Test.DcRetirement;

public class DcRetirementProjectedBalancesResponseTest
{
    public void SetsProjectedBalancesCorrectly()
    {
        var sut = new DcRetirementProjectedBalancesResponse(
            new InvestmentForecastResponse
            {
                Ages = new List<InvestmentForecastByAgeResponse>
                {
                    new InvestmentForecastByAgeResponse { Age = 62, AssetValue = null , TaxFreeCash = null } ,
                    new InvestmentForecastByAgeResponse { Age = 65, AssetValue = 1999.99M } ,
                    new InvestmentForecastByAgeResponse { Age = 66, AssetValue = 2999.99M } ,
                }
            },
            new List<(int, bool)> { (62, false), (65, false), (66, true) });

        sut.ProjectedBalances.Should().ContainSingle(x => x.Age == "62");
        sut.ProjectedBalances.Should().ContainSingle(x => x.Age == "65");
        sut.ProjectedBalances.Should().ContainSingle(x => x.Age == "66*");
        sut.ProjectedBalances.Single(x => x.Age == "62").AssetValue.Should().BeNull();
        sut.ProjectedBalances.Single(x => x.Age == "65").AssetValue.Should().Be(1999.99M);
        sut.ProjectedBalances.Single(x => x.Age == "66*").AssetValue.Should().Be(2999.99M);
    }
}