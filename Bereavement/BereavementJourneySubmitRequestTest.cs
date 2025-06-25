using FluentAssertions;
using WTW.MdpService.BereavementJourneys;
using WTW.Web.Attributes;

namespace WTW.MdpService.Test.Bereavement;

public class BereavementJourneySubmitRequestTest
{
    public void ClassHasEscapeHtmlPropertiesAttribute()
    {
        var attribute = typeof(BereavementJourneySubmitRequest).GetCustomAttributes(typeof(EscapeHtmlPropertiesAttribute), false);
        attribute.Should().HaveCount(1).And.ContainItemsAssignableTo<EscapeHtmlPropertiesAttribute>();
    }
}