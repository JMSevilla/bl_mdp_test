using FluentAssertions;
using WTW.MdpService.ContactsConfirmation;

namespace WTW.MdpService.Test.ContactsConfirmation;

public class RandomNumberTest
{
    public void GeneratesRandomSixDigitsNummericStringToken()
    {
        var sut = RandomNumber.Get();

        sut.Length.Should().Be(6);
        int.TryParse(sut, out var _).Should().BeTrue();
    }
}