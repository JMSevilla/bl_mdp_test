using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Consumers;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.EmailConfirmation;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.PdfGenerator;
using WTW.MdpService.Infrastructure.Templates.SingleAuth;
using WTW.MdpService.SingleAuth.Services;
using WTW.MdpService.Test.Domain.Members;
using WTW.MessageBroker.Common;
using WTW.MessageBroker.Contracts.Mdp;
using WTW.TestCommon;
using WTW.TestCommon.Helpers;

namespace WTW.MdpService.Test.Infrastructure.Consumers;

public class SendEmailConsumerTest
{
    protected readonly SendEmailConsumer _sut;
    protected readonly Mock<ILogger<SendEmailConsumer>> _loggerMock;
    protected readonly Mock<IContentClient> _mockContentClient;
    protected readonly Mock<IEmailConfirmationSmtpClient> _mockEmailClient;
    protected readonly Mock<IRegistrationEmailTemplate> _mockRegistrationEmailTemplate;
    protected readonly Mock<IMemberRepository> _mockMemberRepo;
    protected readonly Mock<ISingleAuthService> _mockSingleAuthService;
    protected readonly Mock<IPdfGenerator> _mockPdfGenerator;
    protected readonly Mock<IWtwPublisher> _mockWtwPublisher;
    protected readonly Mock<ConsumeContext<EmailNotification>> _mockContext;

    public SendEmailConsumerTest()
    {
        _loggerMock = new Mock<ILogger<SendEmailConsumer>>();
        _mockContentClient = new Mock<IContentClient>();
        _mockEmailClient = new Mock<IEmailConfirmationSmtpClient>();
        _mockRegistrationEmailTemplate = new Mock<IRegistrationEmailTemplate>();
        _mockMemberRepo = new Mock<IMemberRepository>();
        _mockSingleAuthService = new Mock<ISingleAuthService>();
        _mockPdfGenerator = new Mock<IPdfGenerator>();
        _mockWtwPublisher = new Mock<IWtwPublisher>();
        _mockContext = new Mock<ConsumeContext<EmailNotification>>();
        _mockContext.SetupGet(x => x.CorrelationId).Returns(Guid.NewGuid());
        _mockContext.SetupGet(x => x.Message).Returns(new EmailNotification()
        {
            Bgroup = TestData.Bgroup,
            EventType = MdpEvent.SingleAuthRegistration,
            Refno = TestData.RefNo,
            ContentAccessKey = "testKey",
            MemberAuthGuid = Guid.NewGuid(),
            TemplateName = "TestTemplate"
        });
        _sut = new SendEmailConsumer(_loggerMock.Object, _mockContentClient.Object, _mockEmailClient.Object,
                                   _mockRegistrationEmailTemplate.Object, _mockMemberRepo.Object, _mockSingleAuthService.Object,
                                   _mockPdfGenerator.Object, _mockWtwPublisher.Object);
    }
}

public class SendEmailConsumerValidTest : SendEmailConsumerTest
{
    public async Task WhenSendEmailConsumerCalled_ThenSendMailAndExpectedLoggingOccurred()
    {
        _mockSingleAuthService.Setup(x => x.GetLinkedRecord(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new List<(string bGroup, string RefNo)>());
        _mockMemberRepo.Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Build());
        _mockContentClient.Setup(x => x.FindTemplate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TemplateResponse()
            {
                HtmlBody = "Test html",
            });
        _mockRegistrationEmailTemplate.Setup(x => x.RenderHtml(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync("Test Html");
        _mockPdfGenerator.Setup(x => x.Generate(It.IsAny<string>())).ReturnsAsync(TestData.PdfDataBase64);

        await _sut.Consume(_mockContext.Object);

        _loggerMock.VerifyLogging($"{nameof(SendEmailConsumer)} Execution started", LogLevel.Information);
        _loggerMock.VerifyLogging($"{nameof(SendEmailConsumer.SendRegistrationEmail)} Email sent", LogLevel.Information);
        _loggerMock.VerifyLogging($"{nameof(SendEmailConsumer)} Execution successful", LogLevel.Information);
    }
}

public class SendEmailConsumerInValidTest : SendEmailConsumerTest
{
    public async Task WhenEventTypeNotConfiguredForSendEmailConsumer_ThenDoNotProcessMessageExpectedLoggingOccurred()
    {
        _mockContext.SetupGet(x => x.Message).Returns(new EmailNotification()
        {
            Bgroup = TestData.Bgroup,
            EventType = MdpEvent.None,
            Refno = TestData.RefNo
        });

        await _sut.Consume(_mockContext.Object);

        _loggerMock.VerifyLogging($"{nameof(SendEmailConsumer)} Invalid event type - {MdpEvent.None}", LogLevel.Error);
    }

    public async Task WhenSendRegistrationEmailThrowsException_ThenLogErrorAndThrowException()
    {
        var error = "Member data not found";
        _mockSingleAuthService.Setup(x => x.GetLinkedRecord(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new List<(string bGroup, string RefNo)>());
        _mockMemberRepo.Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<Member>.None);

        var act = () => _sut.Consume(_mockContext.Object);

        await act.Should().ThrowAsync<MdpConsumerException>()
            .WithMessage(error);

        _loggerMock.VerifyLogging($"{nameof(SendEmailConsumer)} Execution started", LogLevel.Information);
        _loggerMock.VerifyLogging($"{nameof(SendEmailConsumer)} {error}", LogLevel.Error);
    }

    public async Task WhenNoMemberDataFound_ThenLogErrorAndThrowException()
    {
        var error = "Member data not found";
        _mockSingleAuthService.Setup(x => x.GetLinkedRecord(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new List<(string bGroup, string RefNo)>());
        _mockMemberRepo.Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.None);


        var act = () => _sut.Consume(_mockContext.Object);

        await act.Should().ThrowAsync<MdpConsumerException>()
            .WithMessage(error);

        _loggerMock.VerifyLogging($"{nameof(SendEmailConsumer)} Execution started", LogLevel.Information);
        _loggerMock.VerifyLogging($"{nameof(SendEmailConsumer)} {error}", LogLevel.Error);
    }
}
