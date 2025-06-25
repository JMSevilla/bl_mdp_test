using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Members;

public class NotificationSettingTest
{
    public void CreatesPostNotificationSetting()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = NotificationSetting.Create(true, true, true, 1, "POST", now);

        sut.IsRight.Should().BeTrue();
        sut.Right().Settings.Should().BeNull();
        sut.Right().OnlineCommunicationConsent.Should().Be("N");
        sut.Right().StartDate.Should().Be(now);
        sut.Right().BusinessGroup.Should().BeNull();
        sut.Right().ReferenceNumber.Should().BeNull();
    }

    public void CreatesEmailNotificationSetting()
    {
        var sut = NotificationSetting.Create(true, false, true, 1, "EMAIL", DateTimeOffset.UtcNow);

        sut.IsRight.Should().BeTrue();
        sut.Right().Settings.Should().Be("E");
        sut.Right().OnlineCommunicationConsent.Should().Be("Y");
    }

    public void CreatesSmsNotificationSetting()
    {
        var sut = NotificationSetting.Create(false, true, true, 1, "SMS", DateTimeOffset.UtcNow);

        sut.IsRight.Should().BeTrue();
        sut.Right().Settings.Should().Be("S");
        sut.Right().OnlineCommunicationConsent.Should().Be("Y");
    }

    public void CreatesDeselectedSmsNotificationSetting()
    {
        var sut = NotificationSetting.Create(true, false, false, 1, "SMS", DateTimeOffset.UtcNow);

        sut.IsRight.Should().BeTrue();
        sut.Right().Settings.Should().Be("E");
        sut.Right().Scheme.Should().Be("M");
        sut.Right().OnlineCommunicationConsent.Should().Be("Y");
    }

    public void CreatesDeselectedEmailNotificationSetting()
    {
        var sut = NotificationSetting.Create(false, true, false, 1, "EMAIL", DateTimeOffset.UtcNow);

        sut.IsRight.Should().BeTrue();
        sut.Right().Settings.Should().Be("S");
        sut.Right().OnlineCommunicationConsent.Should().Be("Y");
    }

    public void CreatesSmsAndEmailNotificationSetting()
    {
        var sut = NotificationSetting.Create(true, true, true, 1, "SMS", DateTimeOffset.UtcNow);

        sut.IsRight.Should().BeTrue();
        sut.Right().Settings.Should().Be("B");
        sut.Right().OnlineCommunicationConsent.Should().Be("Y");
    }

    public void ReturnsErrorWhenDisablingAllTypeOfNotifications()
    {
        var sut = NotificationSetting.Create(false, false, false, 1, "SMS", DateTimeOffset.UtcNow);

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("At least one of notification preference must be selected.");
    }

    [Input(true, true, true, "SMS", true, true, false)]
    [Input(false, true, true, "SMS", false, true, false)]
    [Input(true, true, true, "EMAIL", true, true, false)]
    [Input(true, false, true, "EMAIL", true, false, false)]
    [Input(true, true, true, "POST", false, false, true)]
    public void ReturnsNotificationsSettings(bool emailInput,
        bool smsInput,
        bool postInput,
        string typeToUpdateInput,
        bool emailExpectedResul,
        bool smsExpectedResul,
        bool postExpectedResul)
    {
        var sut = NotificationSetting.Create(emailInput, smsInput, postInput, 1, typeToUpdateInput, DateTimeOffset.UtcNow);

        var result = sut.Right().NotificationSettings();

        result.Email.Should().Be(emailExpectedResul);
        result.Sms.Should().Be(smsExpectedResul);
        result.Post.Should().Be(postExpectedResul);
    }

    public void ClosesNotificationsSetting()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = NotificationSetting.Create(true, true, true, 1, "SMS", now);

        sut.Right().Close(now);

        sut.Right().EndDate.Should().Be(now.AddDays(-1));
    }
}