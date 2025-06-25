using System;
using FluentAssertions;
using WTW.MdpService.BereavementContactsConfirmation;
using WTW.MdpService.Domain;
using WTW.MdpService.Domain.Bereavement;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Bereavement;

public class BereavementContactConfirmationTest
{
    public void CanCreateBereavementContactConfirmation()
    {
        var refNo = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var sut = BereavementContactConfirmation.CreateForEmail("RBS", refNo, "123456", Email.Create("test@tes.com").Right(), now.AddDays(20), now, 10);

        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferenceNumber.Should().Be(refNo.ToString());
        sut.Contact.Should().Be(EmailSecurity.Hash("test@tes.com"));
        sut.Token.Should().Be("123456");
        sut.CreatedAt.Should().Be(now);
        sut.ExpiresAt.Should().Be(now.AddDays(20));
        sut.FailedConfirmationAttemptCount.Should().Be(0);
        sut.MaximumConfirmationAttemptCount.Should().Be(10);
    }

    public void MarkValidatedReturnsError_WhenOtpIsEnabledAndTokenIsInvalid()
    {
        var refNo = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var sut = BereavementContactConfirmation.CreateForEmail("RBS", refNo, "123456", Email.Create("test@tes.com").Right(), now.AddDays(20), now.AddMinutes(-3), 10);

        var result = sut.MarkValidated("654321", now, false);

        result.HasValue.Should().BeTrue();
        result.Value.Message.Should().Be("CONFIRMATION_TOKEN_INVALID_ERROR");
        sut.FailedConfirmationAttemptCount.Should().Be(1);
    }

    public void MarkValidatedReturnsError_WhenOtpIsEnabledAndConfirmationIsexpired()
    {
        var refNo = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var sut = BereavementContactConfirmation.CreateForEmail("RBS", refNo, "123456", Email.Create("test@tes.com").Right(), now.AddMinutes(-2), now.AddMinutes(-3), 10);

        var result = sut.MarkValidated("123456", now, false);

        result.HasValue.Should().BeTrue();
        result.Value.Message.Should().Be("CONFIRMATION_TOKEN_INVALID_ERROR");
        sut.FailedConfirmationAttemptCount.Should().Be(1);
    }

    public void MarkValidatedReturnsError_WhenOtpIsEnabledAndMaxAttempCountReachedWithinLastAtemp()
    {
        var refNo = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var sut = BereavementContactConfirmation.CreateForEmail("RBS", refNo, "123456", Email.Create("test@tes.com").Right(), now.AddDays(20), now.AddMinutes(-3), 2);

        sut.MarkValidated("654321", now, false);
        var result = sut.MarkValidated("654321", now, false);

        result.HasValue.Should().BeTrue();
        result.Value.Message.Should().Be("CONFIRMATION_TOKEN_EXPIRED_ERROR");
        sut.FailedConfirmationAttemptCount.Should().Be(2);
    }

    public void MarkValidatedReturnsError_WhenOtpIsEnabledAndMaxAttempCountReachedBeforeLastAttemp()
    {
        var refNo = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var sut = BereavementContactConfirmation.CreateForEmail("RBS", refNo, "123456", Email.Create("test@tes.com").Right(), now.AddDays(20), now.AddMinutes(-3), 2);

        sut.MarkValidated("654321", now, false);
        sut.MarkValidated("654321", now, false);
        var result = sut.MarkValidated("654321", now, false);

        result.HasValue.Should().BeTrue();
        result.Value.Message.Should().Be("CONFIRMATION_TOKEN_EXPIRED_ERROR");
        sut.FailedConfirmationAttemptCount.Should().Be(2);
    }

    public void MarkValidated_WhenOtpIsEnabledAndTokenIsValid()
    {
        var refNo = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var sut = BereavementContactConfirmation.CreateForEmail("RBS", refNo, "123456", Email.Create("test@tes.com").Right(), now.AddDays(20), now.AddMinutes(-3), 10);

        var result = sut.MarkValidated("123456", now, false);

        result.HasValue.Should().BeFalse();
        sut.ValidatedAt.Should().Be(now);
    }

    public void MarkValidated_WhenOtpIsDisabledAndTokenIsInvalid()
    {
        var refNo = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var sut = BereavementContactConfirmation.CreateForEmail("RBS", refNo, "123456", Email.Create("test@tes.com").Right(), now.AddDays(20), now.AddMinutes(-3), 10);

        var result = sut.MarkValidated("654321", now, true);

        result.HasValue.Should().BeFalse();
        sut.ValidatedAt.Should().Be(now);
    }
}
