using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WTW.MdpService.BereavementContactsConfirmation;
using WTW.MdpService.BereavementJourneys;
using WTW.MdpService.ContactsConfirmation;
using WTW.MdpService.Domain;
using WTW.MdpService.Domain.Bereavement;
using WTW.MdpService.Infrastructure.BereavementDb;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.EmailConfirmation;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.BereavementContactsConfirmation;

public class BereavementContactsConfirmationControllerTest
{
    private readonly BereavementJourneyConfiguration _bereavementJourneyConfiguration;
    private readonly Mock<IContentClient> _contentClientMock;
    private readonly Mock<IEmailConfirmationSmtpClient> _emailConfirmationSmtpClientMock;
    private readonly Mock<IBereavementUnitOfWork> _bereavementUnitOfWorkMock;
    private readonly OtpSettings _otpSettings;
    private readonly Mock<IBereavementContactConfirmationRepository> _bereavementContactConfirmationRepositoryMock;
    private readonly BereavementContactsConfirmationController _sut;

    public BereavementContactsConfirmationControllerTest()
    {
        _bereavementJourneyConfiguration = new BereavementJourneyConfiguration(1, 1, 1, 1, 1, 1);
        _contentClientMock = new Mock<IContentClient>();
        _emailConfirmationSmtpClientMock = new Mock<IEmailConfirmationSmtpClient>();
        _bereavementUnitOfWorkMock = new Mock<IBereavementUnitOfWork>();
        _otpSettings = new OtpSettings(false);
        _bereavementContactConfirmationRepositoryMock = new Mock<IBereavementContactConfirmationRepository>();

        _sut = new BereavementContactsConfirmationController(
            _bereavementJourneyConfiguration,
            _contentClientMock.Object,
            _emailConfirmationSmtpClientMock.Object,
            _bereavementUnitOfWorkMock.Object,
            _otpSettings,
            _bereavementContactConfirmationRepositoryMock.Object);
        SetupControllerContext();
    }

    [Input("inva;in_email_address_gmail.com", false, typeof(BadRequestObjectResult))]
    [Input("test@gmail.com", true, typeof(BadRequestObjectResult))]
    [Input("test@gmail.com", false, typeof(OkObjectResult))]
    public async Task SendConfirmationEmailReturnsCorrectHttpStatusCodeAndCorrectData(string emailAddress, bool lockedConfirmationExists, Type expectedType)
    {
        var now = DateTime.UtcNow;
        var email = Email.Create(emailAddress);
        if (email.IsRight && lockedConfirmationExists)
        {
            var confirmation = BereavementContactConfirmation.CreateForEmail("RBS", Guid.NewGuid(), "123456", email.Right(), now.AddMinutes(1), now, 1);
            _bereavementContactConfirmationRepositoryMock.Setup(x => x.FindLocked(It.IsAny<Email>())).ReturnsAsync(confirmation);
        }

        _contentClientMock
            .Setup(x => x.FindTemplate("email_validation_message", "{}"))
            .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });

        var result = await _sut.SendConfirmationEmail(new BereavementContactsConfirmationRequest
        {
            EmailAddress = emailAddress,
            ContentAccessKey = "{}"
        });


        result.Should().BeOfType(expectedType);

        if (expectedType == typeof(OkObjectResult))
        {
            var response = (result as OkObjectResult).Value as BereavementContactsConfirmationResponse;
            response.TokenExpirationDate.Value.Should().BeAfter(now);
        }
    }

    [Input(false, "123456", typeof(NotFoundObjectResult))]
    [Input(true, "123456", typeof(NoContentResult))]
    [Input(true, "123457", typeof(BadRequestObjectResult))]
    public async Task ConfirmEmailReturnsCorrectHttpStatusCodeAndCorrectData(bool confirmationExists, string token, Type expectedType)
    {
        var now = DateTimeOffset.UtcNow;
        if (confirmationExists)
            _bereavementContactConfirmationRepositoryMock
                    .Setup(x => x.FindLastEmailConfirmation(It.IsAny<string>(), It.IsAny<Guid>()))
                    .ReturnsAsync(BereavementContactConfirmation.CreateForEmail("RBS", Guid.NewGuid(), token, Email.Create("test@tes.com").Right(), now.AddDays(20), now, 10));

        var result = await _sut.ConfirmEmail(new EmailConfirmationRequest { EmailConfirmationToken = "123456" });

        result.Should().BeOfType(expectedType);
    }

    private void SetupControllerContext()
    {
        var id = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("business_group", "TestBusinessGroup"),
            new Claim("bereavement_reference_number", Guid.NewGuid().ToString()),
        };

        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }
}