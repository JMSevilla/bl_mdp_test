using System.Text.Json;
using FluentAssertions;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Retirement;

public class RetirementServiceTest
{
    public void CanAddSelectedQuoteDetailsToDictionary()
    {
        var retirementResponse = JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options());
        var retirement = new RetirementV2Mapper().MapToDomain(retirementResponse, "PV");

        var sut = new RetirementService();
        var result = sut.GetSelectedQuoteDetails("fullPension", retirement);

        result.Count.Should().Be(7);
        result["fullPension_totalLTAUsedPerc"].Should().Be(14.72m);
        result["fullPension_totalPension"].Should().Be(32369.4m);
        result["fullPension_totalSpousePension"].Should().Be(12247.2m);
        result["fullPension_pensionTranches_post88GMP"].Should().Be(1604.2m);
        result["fullPension_pensionTranches_post97"].Should().Be(15249.7m);
        result["fullPension_pensionTranches_pre88GMP"].Should().Be(960.44m);
        result["fullPension_pensionTranches_pre97Excess"].Should().Be(14555.06m);
    }
}
