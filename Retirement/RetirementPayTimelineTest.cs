using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Retirement;
using WTW.TestCommon.FixieConfig;

namespace WTW.MdpService.Test.Retirement;

public class RetirementPayTimelineTest
{
    [Input("1335", "", "23")]
    [Input("", "1436", "5")]
    [Input("3132", "2140", "18")]
    public void ReturnsCorrectPayday( string category, string scheme, string expectedResult)
    {
        var sut = new RetirementPayTimeline(Timelines(), category, scheme, new LoggerFactory());

        var date = sut.PensionPayDay();

        date.Should().Be(expectedResult);
    }

    [Input("1335", "", "+")]
    [Input("", "1436", "-")]
    [Input("3132", "2140", null)]
    public void ReturnsCorrectPayDayIndicator(string category, string scheme, string expectedResult)
    {
        var sut = new RetirementPayTimeline(Timelines(), category, scheme, new LoggerFactory());

        var indicator = sut.PensionPayDayIndicator();

        indicator.Should().Be(expectedResult);
    }

    private static List<TenantRetirementTimeline> Timelines()
    {
        return new List<TenantRetirementTimeline>()
        {
            new TenantRetirementTimeline(1, "RETFIRSTPAYMADE","RT", "HBS", "*",
                "1300,1302,1303,1304,1325,1326,1330,1332,1333,1334,1335,1336,1340,1342,1343,1344,1345,1346", "+,0,+,0,0,23,+"),
            new TenantRetirementTimeline(2, "RETFIRSTPAYMADE","RT", "HBS",
                "1400,1402,1403,1404,1425,1426,1435,1436,1445,1446", "*", "+,0,+,0,0,5,-"),
            new TenantRetirementTimeline(1, "RETFIRSTPAYMADE","RT", "HBS",
                "2100,2102,2103,2125,2126,2135,2136,2140,2142,2143,2145,2146,2200,2202,2203,2225,2226",
                "3100,3102,3103,3125,3126,3130,3132,3133,3135,3136,3140,3142,3143,3145,3146", "+,0,+,0,0,18"),
        };
    }
}