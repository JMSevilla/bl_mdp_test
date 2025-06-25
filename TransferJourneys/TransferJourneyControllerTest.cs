using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Aws;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.Edms;
using WTW.MdpService.Infrastructure.EmailConfirmation;
using WTW.MdpService.Infrastructure.JobScheduler;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.MemberDb.Documents;
using WTW.MdpService.Infrastructure.MemberDb.IfaReferrals;
using WTW.MdpService.Infrastructure.PdfGenerator;
using WTW.MdpService.Infrastructure.Redis;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.MdpService.Test.Domain.Members;
using WTW.MdpService.TransferJourneys;
using WTW.Web.Errors;
using WTW.Web.Serialization;
using static LanguageExt.Prelude;

namespace WTW.MdpService.Test.TransferJourneys;

public class TransferJourneyControllerTest
{
    private readonly TransferJourneyController _sut;
    private readonly Mock<ICalculationsClient> _calculationsClientMock;
    private readonly Mock<ITransferJourneyRepository> _transferJourneyRepositoryMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IMdpUnitOfWork> _mdpDbUnitOfWorkMock;
    private readonly Mock<IMemberDbUnitOfWork> _uowMock;
    private readonly Mock<IContentClient> _contentClientMock;
    private readonly Mock<IEdmsClient> _edmsClientMock;
    private readonly Mock<IEmailConfirmationSmtpClient> _smtpClientMock;
    private readonly Mock<ILogger<TransferJourneyController>> _logger;
    private readonly Mock<IRetirementPostIndexEventRepository> _postIndexEventsRepositoryMock;
    private readonly Mock<IJobSchedulerClient> _jobSchedulerClientMock;
    private readonly Mock<ICalculationsRedisCache> _calculationsRedisCacheMock;
    private readonly Mock<IDocumentsRepository> _documentsRepositoryMock;
    private readonly Mock<IAwsClient> _awsClientMock;
    private readonly Mock<ITransferCalculationRepository> _transferCalculationRepositoryMock;
    private readonly Mock<ICalculationHistoryRepository> _calculationHistoryRepositoryMock;
    private readonly Mock<IIfaConfigurationRepository> _ifaConfigurationRepositoryMock;
    private readonly Mock<IIfaReferralRepository> _ifaReferralRepositoryMock;
    private readonly MemberIfaReferral _memberIfaReferral;
    private readonly Mock<IPdfGenerator> _pdfGeneratorMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;

    public TransferJourneyControllerTest()
    {
        _calculationsClientMock = new Mock<ICalculationsClient>();
        _transferJourneyRepositoryMock = new Mock<ITransferJourneyRepository>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _mdpDbUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _uowMock = new Mock<IMemberDbUnitOfWork>();
        _contentClientMock = new Mock<IContentClient>();
        _edmsClientMock = new Mock<IEdmsClient>();
        _smtpClientMock = new Mock<IEmailConfirmationSmtpClient>();
        _logger = new Mock<ILogger<TransferJourneyController>>();
        _postIndexEventsRepositoryMock = new Mock<IRetirementPostIndexEventRepository>();
        _jobSchedulerClientMock = new Mock<IJobSchedulerClient>();
        _calculationsRedisCacheMock = new Mock<ICalculationsRedisCache>();
        _awsClientMock = new Mock<IAwsClient>();
        _transferCalculationRepositoryMock = new Mock<ITransferCalculationRepository>();
        _calculationHistoryRepositoryMock = new Mock<ICalculationHistoryRepository>();
        _ifaConfigurationRepositoryMock = new Mock<IIfaConfigurationRepository>();
        _ifaReferralRepositoryMock = new Mock<IIfaReferralRepository>();
        _memberIfaReferral = new MemberIfaReferral(_ifaReferralRepositoryMock.Object, _transferJourneyRepositoryMock.Object);
        _pdfGeneratorMock = new Mock<IPdfGenerator>();
        _calculationsParserMock = new Mock<ICalculationsParser>();

        _sut = new TransferJourneyController(
            _calculationsClientMock.Object,
            _transferJourneyRepositoryMock.Object,
            _memberRepositoryMock.Object,
            _mdpDbUnitOfWorkMock.Object,
            _uowMock.Object,
            _contentClientMock.Object,
            _edmsClientMock.Object,
            _smtpClientMock.Object,
            _logger.Object,
            _postIndexEventsRepositoryMock.Object,
            _jobSchedulerClientMock.Object,
            _calculationsRedisCacheMock.Object,
            _awsClientMock.Object,
            _transferCalculationRepositoryMock.Object,
            new JobSchedulerConfiguration("dev"),
            _calculationHistoryRepositoryMock.Object,
            _ifaConfigurationRepositoryMock.Object,
            _memberIfaReferral,
            _pdfGeneratorMock.Object,
            _calculationsParserMock.Object);

        SetupControllerContext();
    }

    public async Task CreateIfaCase_ReturnsNoContent_WhenAllOperationsSucceed()
    {
        var now = DateTime.UtcNow;
        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        var journey = new TransferJourneyBuilder()
            .CurrentPageKey("current_page_test")
            .NextPageKey("next_page_test")
            .BuildWithSteps();

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        var transferCalculation = new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), TestData.TransferQuoteJson, now);

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        _jobSchedulerClientMock
            .Setup(x => x.Login())
            .ReturnsAsync(Right<Error, LoginResponse>(new LoginResponse()));

        _calculationsClientMock
            .Setup(x => x.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => TryAsync(async () =>
            {
                return new TypeResponse { Type = "TestType" };
            }));

        var orderRequest = OrderRequest.CreateOrderRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 1);
        _jobSchedulerClientMock
            .Setup(x => x.CreateOrder(orderRequest, "testToken"))
            .ReturnsAsync(Right<Error, Unit>(Unit.Default));

        var orderStatusRequest = OrderRequest.OrderStatusRequest(It.IsAny<string>(), It.IsAny<string>());
        _jobSchedulerClientMock
            .Setup(x => x.CheckOrderStatus(orderStatusRequest, "testToken"))
            .ReturnsAsync(Right<Error, OrderStatusResponse>(new OrderStatusResponse()));

        _jobSchedulerClientMock
            .Setup(x => x.Logout("testToken"))
            .ReturnsAsync(Right<Error, Unit>(Unit.Default));

        _contentClientMock
           .Setup(x => x.FindTemplate("transfer_completion_email", It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });
        _contentClientMock
            .Setup(x => x.FindTemplate("transfer_lv_email", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });

        _ifaConfigurationRepositoryMock
            .Setup(x => x.FindEmail(It.IsAny<string>(), It.IsAny<string>(), "LV"))
            .ReturnsAsync(Option<string>.Some("company@test.com"));

        _smtpClientMock
            .Setup(x => x.Send(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _ifaReferralRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<IfaReferral>.Some(new IfaReferral(It.IsAny<string>(), It.IsAny<string>(), now, It.IsAny<string>(), It.IsAny<string>())));

        var datesResponse = new RetirementDatesAgesResponse()
        {
            HasLockedInTransferQuote = true,
            LockedInTransferQuoteSeqno = 1,
        };
        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => datesResponse);

        _calculationsParserMock
                .Setup(x => x.GetTransferQuote(It.IsAny<string>()))
                .Returns(new TransferQuote(JsonSerializer.Deserialize<TransferQuoteDto>(TestData.TransferQuoteJson, SerialiationBuilder.Options())));

        var request = new SendIfaEmailsRequest
        {
            ContentAccessKey = "testContentAccessKey",
        };
        var result = await _sut.SendIfaEmails(request);

        result.Should().BeOfType<NoContentResult>();
    }

    public async Task CreateIfaCase_ReturnsBadRequest_If_LockedInTransferQuoteSeqno_Is_Null()
    {
        var now = DateTime.UtcNow;
        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        var journey = new TransferJourneyBuilder()
            .CurrentPageKey("current_page_test")
            .NextPageKey("next_page_test")
            .BuildWithSteps();

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        var transferCalculation = new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), TestData.TransferQuoteJson, now);

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        _jobSchedulerClientMock
            .Setup(x => x.Login())
            .ReturnsAsync(Right<Error, LoginResponse>(new LoginResponse()));

        _calculationsClientMock
            .Setup(x => x.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => TryAsync(async () =>
            {
                return new TypeResponse { Type = "TestType" };
            }));

        var datesResponse = new RetirementDatesAgesResponse()
        {
            HasLockedInTransferQuote = true,
            LockedInTransferQuoteSeqno = null,
        };
        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => datesResponse);

        var request = new SendIfaEmailsRequest
        {
            ContentAccessKey = "testContentAccessKey",
        };
        var result = await _sut.SendIfaEmails(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        var errorResponse = ((BadRequestObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Message.Should().Be("Seqno does not have value");
    }

    public async Task CreateIfaCase_ReturnsBadRequest_If_CalcApi_ReterimentDatesAge_Failed()
    {
        var now = DateTime.UtcNow;
        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        var journey = new TransferJourneyBuilder()
            .CurrentPageKey("current_page_test")
            .NextPageKey("next_page_test")
            .BuildWithSteps();

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        var transferCalculation = new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), TestData.TransferQuoteJson, now);

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        _jobSchedulerClientMock
            .Setup(x => x.Login())
            .ReturnsAsync(Right<Error, LoginResponse>(new LoginResponse()));

        _calculationsClientMock
            .Setup(x => x.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => TryAsync(async () =>
            {
                return new TypeResponse { Type = "TestType" };
            }));


        var request = new SendIfaEmailsRequest
        {
            ContentAccessKey = "testContentAccessKey",
        };
        var result = await _sut.SendIfaEmails(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        var errorResponse = ((BadRequestObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Message.Should().Be("Failed to receive transfer quote information");
    }

    private void SetupControllerContext()
    {
        var id = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("business_group", "TestBusinessGroup"),
            new Claim("reference_number", "TestReferenceNumber"),
        };

        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }
}
