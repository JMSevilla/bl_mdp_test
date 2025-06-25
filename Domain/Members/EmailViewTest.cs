using FluentAssertions;
using WTW.MdpService.Domain;
using WTW.MdpService.Domain.Members;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Members;

public class EmailViewTest
{
    public void CanCreateEmailView()
    {
        var email = Email.Create("test@wtw.com").Right();
        var sut = new EmailView(email);

        sut.Email.Should().Be(Email.Create("test@wtw.com").Right());
    }
}