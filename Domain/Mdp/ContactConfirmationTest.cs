using System;
using FluentAssertions;
using LanguageExt.Common;
using WTW.MdpService.Domain;
using WTW.MdpService.Domain.Mdp;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Mdp;

public class ContactConfirmationTest
{
    public void CreateEmailContactConfirmation()
    {
        var email = Email.Create("test@test.com").Right();
        var utcNow = DateTimeOffset.UtcNow;
        var maximumConfirmationAttemptCount = 5;

        var sut = ContactConfirmation.CreateForEmail("RBS", "0304442", "123456", email, utcNow.AddMinutes(5), utcNow, maximumConfirmationAttemptCount);

        sut.Should().NotBeNull();
        sut.ContactType.Should().Be(ContactType.EmailAddress);
        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferenceNumber.Should().Be("0304442");
        sut.Token.Should().Be("123456");
        sut.Contact.Should().Be(email);
        sut.CreatedAt.Should().Be(utcNow);
        sut.ExpiresAt.Should().Be(utcNow.AddMinutes(5));
        sut.ValidatedAt.Should().BeNull();
        sut.MaximumConfirmationAttemptCount.Should().Be(5);
        sut.FailedConfirmationAttemptCount.Should().Be(0);
    }

    public void ThrowsIfExpirationIsEarlierThanCreationDate_WhenCreatingEmailConfirmation()
    {
        var email = Email.Create("test@test.com").Right();
        var utcNow = DateTimeOffset.UtcNow;
        var maximumConfirmationAttemptCount = 5;

        var action = () => ContactConfirmation.CreateForEmail("RBS", "0304442", "123456", email, utcNow.AddMinutes(-5), utcNow, maximumConfirmationAttemptCount);

        action.Should().Throw<InvalidOperationException>();
    }

    public void CanValidateEmailContactConfirmationWithValidToken()
    {
        var maximumConfirmationAttemptCount = 5;
        var utcNow = DateTimeOffset.UtcNow;
        var email = Email.Create("test@test.com").Right();
        var creationDate = DateTimeOffset.UtcNow.AddMinutes(-2);
        var sut = ContactConfirmation.CreateForEmail("RBS", "0304442", "123456", email, creationDate.AddMinutes(5), creationDate, maximumConfirmationAttemptCount);

        var result = sut.MarkValidated("123456", utcNow, false);

        sut.Should().NotBeNull();
        result.HasValue.Should().BeFalse();
        sut.ValidatedAt.Should().Be(utcNow);
    }

    public void CanValidateEmailContactConfirmationWithInvalidToken_WhenOtpIsDisabled()
    {
        var maximumConfirmationAttemptCount = 5;
        var utcNow = DateTimeOffset.UtcNow;
        var email = Email.Create("test@test.com").Right();
        var creationDate = DateTimeOffset.UtcNow.AddMinutes(-2);
        var sut = ContactConfirmation.CreateForEmail("RBS", "0304442", "123456", email, creationDate.AddMinutes(5), creationDate, maximumConfirmationAttemptCount);

        var result = sut.MarkValidated("654321", utcNow, true);

        sut.Should().NotBeNull();
        result.HasValue.Should().BeFalse();
        sut.ValidatedAt.Should().Be(utcNow);
    }

    public void ReturnsErrorIfInvalidTokenIsProvided()
    {
        var maximumConfirmationAttemptCount = 5;
        var email = Email.Create("test@test.com").Right();
        var utcNow = DateTimeOffset.UtcNow;
        var creationDate = DateTimeOffset.UtcNow.AddMinutes(-2);
        var sut = ContactConfirmation.CreateForEmail("RBS", "0304442", "123456", email, creationDate.AddMinutes(5), creationDate, maximumConfirmationAttemptCount);

        var result = sut.MarkValidated("654321", utcNow, false);

        sut.Should().NotBeNull();
        result.HasValue.Should().BeTrue();
        result.Value.Message.Should().Be("CONFIRMATION_TOKEN_INVALID_ERROR");
        sut.ValidatedAt.Should().BeNull();
        sut.FailedConfirmationAttemptCount.Should().Be(1);
    }

    public void ReturnsErrorIfAttemptingToValidateTheSameTokenMoreThanOnce()
    {
        var maximumeConfirmationAttemptCount = 5;
        var email = Email.Create("test@test.com").Right();
        var utcNow = DateTimeOffset.UtcNow;
        var creationDate = DateTimeOffset.UtcNow.AddMinutes(-2);
        var sut = ContactConfirmation.CreateForEmail("RBS", "0304442", "123456", email, creationDate.AddMinutes(5), creationDate, maximumeConfirmationAttemptCount);
        sut.MarkValidated("123456", utcNow, false);

        var result = sut.MarkValidated("123456", utcNow, false);

        sut.Should().NotBeNull();
        result.HasValue.Should().BeTrue();
        result.Value.Message.Should().Be("CONFIRMATION_TOKEN_INVALID_ERROR");
        sut.ValidatedAt.Should().NotBeNull();
    }

    public void CreateMobilePhoneContactConfirmation()
    {
        var maximumConfirmationAttemptCount = 5;
        var mobilePhone = Phone.Create("447544111111").Right();
        var utcNow = DateTimeOffset.UtcNow;

        var sut = ContactConfirmation.CreateForMobile("RBS", "0304442", "123456", mobilePhone, utcNow.AddMinutes(5), utcNow, maximumConfirmationAttemptCount);

        sut.Should().NotBeNull();
        sut.ContactType.Should().Be(ContactType.MobilePhoneNumber);
        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferenceNumber.Should().Be("0304442");
        sut.Token.Should().Be("123456");
        sut.Contact.Should().Be(mobilePhone);
        sut.CreatedAt.Should().Be(utcNow);
        sut.ExpiresAt.Should().Be(utcNow.AddMinutes(5));
        sut.ValidatedAt.Should().BeNull();
        sut.MaximumConfirmationAttemptCount.Should().Be(5);
        sut.FailedConfirmationAttemptCount.Should().Be(0);
    }

    public void ThrowsIfExpirationIsEarlierThanCreationDate_WhenCreatingMobilePhoneConfirmation()
    {
        var maximumConfirmationAttemptCount = 5;
        var mobilePhone = Phone.Create("447544111111").Right();
        var utcNow = DateTimeOffset.UtcNow;

        var action = () => ContactConfirmation.CreateForMobile("RBS", "0304442", "123456", mobilePhone, utcNow.AddMinutes(-5), utcNow, maximumConfirmationAttemptCount);

        action.Should().Throw<InvalidOperationException>();
    }

    public void CanValidateMobilePhoneContactConfirmationWithValidToken()
    {
        var maximumConfirmationAttemptCount = 5;
        var utcNow = DateTimeOffset.UtcNow;
        var mobilePhone = Phone.Create("447544111111").Right();
        var creationDate = DateTimeOffset.UtcNow.AddMinutes(-2);
        var sut = ContactConfirmation.CreateForMobile("RBS", "0304442", "123456", mobilePhone, creationDate.AddMinutes(5), creationDate, maximumConfirmationAttemptCount);

        var result = sut.MarkValidated("123456", utcNow, false);

        sut.Should().NotBeNull();
        result.HasValue.Should().BeFalse();
        sut.ValidatedAt.Should().HaveValue();
    }

    public void CanValidateMobilePhoneContactConfirmationWithInvalidToken_WhenOtpIsDisabled()
    {
        var maximumConfirmationAttemptCount = 5;
        var utcNow = DateTimeOffset.UtcNow;
        var mobilePhone = Phone.Create("447544111111").Right();
        var creationDate = DateTimeOffset.UtcNow.AddMinutes(-2);
        var sut = ContactConfirmation.CreateForMobile("RBS", "0304442", "123456", mobilePhone, creationDate.AddMinutes(5), creationDate, maximumConfirmationAttemptCount);

        var result = sut.MarkValidated("654321", utcNow, true);

        sut.Should().NotBeNull();
        result.HasValue.Should().BeFalse();
        sut.ValidatedAt.Should().HaveValue();
    }

    public void ReturnsErrorIfTokenValidationFailsMoreThanMaximumAllowedFailureCount()
    {
        var maximumConfirmationAttemptCount = 5;
        var utcNow = DateTimeOffset.UtcNow;
        var mobilePhone = Phone.Create("447544111111").Right();
        var creationDate = DateTimeOffset.UtcNow.AddMinutes(-2);
        var sut = ContactConfirmation.CreateForMobile("RBS", "0304442", "123456", mobilePhone, creationDate.AddMinutes(5), creationDate, maximumConfirmationAttemptCount);
        Error? error = null;

        for (int i = 0; i < maximumConfirmationAttemptCount + 1; i++)
        {
            error = sut.MarkValidated("invalidToken", utcNow, false);
        }

        sut.Should().NotBeNull();
        error.Value.Message.Should().Be("CONFIRMATION_TOKEN_EXPIRED_ERROR");
        error.HasValue.Should().BeTrue();
        sut.FailedConfirmationAttemptCount.Should().Be(5);
    }
}