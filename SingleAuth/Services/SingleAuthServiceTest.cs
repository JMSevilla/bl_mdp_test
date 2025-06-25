using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.DeloreanAuthentication;
using WTW.MdpService.Infrastructure.EpaService;
using WTW.MdpService.Infrastructure.MemberService;
using WTW.MdpService.SingleAuth.Services;
using WTW.MdpService.Test.Infrastructure.DeloreanAuthentication;
using WTW.MdpService.Test.Infrastructure.EpaService;
using WTW.MdpService.Test.Infrastructure.MemberService;
using WTW.MessageBroker.Common;
using WTW.MessageBroker.Contracts.Mdp;
using WTW.TestCommon;
using WTW.TestCommon.Helpers;
using WTW.Web;

namespace WTW.MdpService.Test.SingleAuth.Services;

public class SingleAuthServiceTest
{
    private readonly SingleAuthService _sut;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IDeloreanAuthenticationClient> _authClientMock;
    private readonly Mock<IMemberServiceClient> _mockMemberClient;
    private readonly Mock<ILogger<SingleAuthService>> _loggerMock;
    private readonly Mock<IWtwPublisher> _publisherMock;
    private readonly Mock<IContentService> _mockContentService;
    private readonly Mock<IEpaServiceClient> _mockEpaClient;

    public SingleAuthServiceTest()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _authClientMock = new Mock<IDeloreanAuthenticationClient>();
        _loggerMock = new Mock<ILogger<SingleAuthService>>();
        _mockMemberClient = new Mock<IMemberServiceClient>();
        _publisherMock = new Mock<IWtwPublisher>();
        _mockContentService = new Mock<IContentService>();
        _mockEpaClient = new Mock<IEpaServiceClient>();
        _sut = new SingleAuthService(_httpContextAccessorMock.Object, _loggerMock.Object,
                                    _authClientMock.Object, _mockMemberClient.Object, _publisherMock.Object,
                                    _mockContentService.Object, _mockEpaClient.Object);
    }

    public async Task WhenRegisterUserIsCalledWithValidData_ThenReturnTrueAndExpectedLoggingOccurred()
    {
        SetupValidRequest();
        _authClientMock.Setup(x => x.UpdateMember(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(GetMemberAccessClientResponseFactory.Create(TestData.Sub));

        var result = await _sut.RegisterUser(It.IsAny<string>());

        AssertSuccess(result, true);
        _loggerMock.VerifyLogging($"Member registration successful for sub {TestData.Sub} and {TestData.Sub}", LogLevel.Information, Times.Once());
    }

    public async Task WhenRegisterUserIsCalledAndBlockWelcomeEmailFalse_ThenQueueEmailEventAndAndExpectedLoggingOccurred()
    {
        SetupValidRequest();
        _authClientMock.Setup(x => x.UpdateMember(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(GetMemberAccessClientResponseFactory.Create(TestData.Sub));
        _mockContentService.Setup(x => x.FindTenant(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(ContentResponseFactory.CreateBlockEmail(false));

        var result = await _sut.RegisterUser(It.IsAny<string>());

        AssertSuccess(result, true);
        _publisherMock.Verify(x => x.Publish<EmailNotification>(It.IsAny<EmailNotification>(), It.IsAny<Guid>()), Times.Once);
        _loggerMock.VerifyLogging($"Message published to email queue for sub {TestData.Sub}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"Member registration successful for sub {TestData.Sub} and {TestData.Sub}", LogLevel.Information, Times.Once());
    }

    public async Task WhenRegisterUserIsCalledAndBlocRelatedRegistrationFalse_ThenRegisterMembersAndAndExpectedLoggingOccurred()
    {
        var matchingRefno = "23532";
        SetupValidRequest();
        _authClientMock.Setup(x => x.UpdateMember(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(GetMemberAccessClientResponseFactory.Create(TestData.Sub));
        _mockMemberClient.Setup(x => x.GetPersonalDetail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(GetMemberPersonalDetailClientResponseFactory.Create());
        _mockMemberClient.Setup(x => x.GetMemberMatchingRecords(It.IsAny<string>(), It.IsAny<GetMemberMatchingClientRequest>()))
            .ReturnsAsync(GetMatchingMemberClientResponseFactory.Create(matchingRefno));
        _authClientMock.Setup(x => x.CheckEligibility(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CheckEligibilityClientResponseFactory.Create(true));
        _mockContentService.Setup(x => x.FindTenant(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(ContentResponseFactory.CreateBlockRelatedRegistration(false));

        var result = await _sut.RegisterUser(It.IsAny<string>());

        AssertSuccess(result, true);
        _publisherMock.Verify(x => x.Publish<EmailNotification>(It.IsAny<EmailNotification>(), It.IsAny<Guid>()), Times.Never);
        _loggerMock.VerifyLogging($"Related member registered successfully - {matchingRefno}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"Member registration successful for sub {TestData.Sub} and {TestData.Sub}", LogLevel.Information, Times.Once());
    }

    public async Task WhenNoExternalIdClaimFoundDuringRegistration_ThenReturnErrorAndExpectedLoggingOccurred()
    {
        _httpContextAccessorMock.SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext());

        var result = await _sut.RegisterUser(It.IsAny<string>());

        AssertError(result, $"Claim {MdpConstants.MemberClaimNames.ExternalId} not found in request token");
        _loggerMock.VerifyLogging($"Claim {MdpConstants.MemberClaimNames.ExternalId} not found in request token", LogLevel.Error, Times.Once());
    }

    public async Task WhenNoSubClaimFoundDuringRegistration_ThenReturnErrorAndExpectedLoggingOccurred()
    {
        _httpContextAccessorMock.SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext()
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(MdpConstants.MemberClaimNames.ExternalId, TestData.Sub.ToString()) }))
            });

        var result = await _sut.RegisterUser(It.IsAny<string>());

        AssertError(result, $"Claim {MdpConstants.MemberClaimNames.Sub} not found in request token");
        _loggerMock.VerifyLogging($"Claim {MdpConstants.MemberClaimNames.Sub} not found in request token", LogLevel.Error, Times.Once());
    }

    public async Task WhenBgroupHeaderNotFoundDuringRegistration_ThenReturnErrorAndExpectedLoggingOccurred()
    {
        _httpContextAccessorMock.SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext()
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                    new Claim(MdpConstants.MemberClaimNames.ExternalId, TestData.Sub.ToString()),
                    new Claim(MdpConstants.MemberClaimNames.Sub, TestData.Sub.ToString())
                }))
            });

        var result = await _sut.RegisterUser(It.IsAny<string>());

        AssertError(result, $"Required header {MdpConstants.BusinessGroupHeaderName} not found");
        _loggerMock.VerifyLogging($"Required header {MdpConstants.BusinessGroupHeaderName} not found", LogLevel.Error, Times.Once());

    }

    public async Task WhenRegisterUserIsCalledAndBlockWelcomeEmailFalseButExceptionIsThrown_ThenLogWarningAndContinue()
    {
        SetupValidRequest();
        _authClientMock.Setup(x => x.UpdateMember(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
           .Returns(Task.CompletedTask);
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .Throws<DivideByZeroException>();

        var result = await _sut.RegisterUser(It.IsAny<string>());

        AssertSuccess(result, true);
        _loggerMock.VerifyLogging($"Error occurred in {nameof(SingleAuthService.QueueEventAndRegisterRelatedMember)}", LogLevel.Error, Times.Once());
        _loggerMock.VerifyLogging($"Member registration successful for sub {TestData.Sub} and {TestData.Sub}", LogLevel.Information, Times.Once());
    }

    public async Task WhenRegisterUserIsCalledAndBlocRelatedRegistrationFalseButRelatedMemberNotEligible_ThenDoNotRegisterMembersAndAndExpectedLoggingOccurred()
    {
        var matchingRefno = "67889";
        SetupValidRequest();
        _authClientMock.Setup(x => x.UpdateMember(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(GetMemberAccessClientResponseFactory.Create(TestData.Sub));
        _mockMemberClient.Setup(x => x.GetPersonalDetail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(GetMemberPersonalDetailClientResponseFactory.Create());
        _mockMemberClient.Setup(x => x.GetMemberMatchingRecords(It.IsAny<string>(), It.IsAny<GetMemberMatchingClientRequest>()))
            .ReturnsAsync(GetMatchingMemberClientResponseFactory.Create(matchingRefno));
        _authClientMock.Setup(x => x.CheckEligibility(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CheckEligibilityClientResponseFactory.Create(false));
        _mockContentService.Setup(x => x.FindTenant(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(ContentResponseFactory.CreateBlockRelatedRegistration(false));

        var result = await _sut.RegisterUser(It.IsAny<string>());

        AssertSuccess(result, true);
        _authClientMock.Verify(x => x.RegisterRelatedMember(It.IsAny<string>(), It.IsAny<string>(),
                               It.IsAny<List<RegisterRelatedMemberDeloreanClientRequest>>()), Times.Never);
        _loggerMock.VerifyLogging($"Skipping related member registration for refno - {matchingRefno} as not eligible", LogLevel.Warning, Times.Once());
        _loggerMock.VerifyLogging($"Member registration successful for sub {TestData.Sub} and {TestData.Sub}", LogLevel.Information, Times.Once());
    }

    public async Task WhenRegisterUserIsCalledAndBlocRelatedRegistrationFalseButNoMatchingRecord_ThenDoNotRegisterMembersAndAndExpectedLoggingOccurred()
    {
        SetupValidRequest();
        _authClientMock.Setup(x => x.UpdateMember(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(GetMemberAccessClientResponseFactory.Create(TestData.Sub));
        _mockMemberClient.Setup(x => x.GetPersonalDetail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(GetMemberPersonalDetailClientResponseFactory.Create());
        _mockMemberClient.Setup(x => x.GetMemberMatchingRecords(It.IsAny<string>(), GetMemberMatchingClientRequestFactory.Create()))
            .ReturnsAsync(Option<GetMatchingMemberClientResponse>.None);
        _mockContentService.Setup(x => x.FindTenant(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(ContentResponseFactory.CreateBlockRelatedRegistration(false));

        var result = await _sut.RegisterUser(It.IsAny<string>());

        AssertSuccess(result, true);
        _authClientMock.Verify(x => x.RegisterRelatedMember(It.IsAny<string>(), It.IsAny<string>(),
                               It.IsAny<List<RegisterRelatedMemberDeloreanClientRequest>>()), Times.Never);
        _loggerMock.VerifyLogging("No matching records found for registration", LogLevel.Warning, Times.Once());
        _loggerMock.VerifyLogging($"Member registration successful for sub {TestData.Sub} and {TestData.Sub}", LogLevel.Information, Times.Once());
    }

    public async Task WhenRegisterUserIsCalledAndBlocRelatedRegistrationFalseAndMatchingRecordFoundButNoPersonalDetailsFound_ThenDoNotRegisterMembersAndAndExpectedLoggingOccurred()
    {
        SetupValidRequest();
        _authClientMock.Setup(x => x.UpdateMember(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(GetMemberAccessClientResponseFactory.Create(TestData.Sub));
        _mockMemberClient.Setup(x => x.GetPersonalDetail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Option<GetMemberPersonalDetailClientResponse>.None);
        _mockMemberClient.Setup(x => x.GetMemberMatchingRecords(It.IsAny<string>(), GetMemberMatchingClientRequestFactory.Create()))
            .ReturnsAsync(GetMatchingMemberClientResponseFactory.Create(TestData.RefNo));
        _mockContentService.Setup(x => x.FindTenant(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(ContentResponseFactory.CreateBlockRelatedRegistration(false));

        var result = await _sut.RegisterUser(It.IsAny<string>());

        AssertSuccess(result, true);
        _authClientMock.Verify(x => x.RegisterRelatedMember(It.IsAny<string>(), It.IsAny<string>(),
                               It.IsAny<List<RegisterRelatedMemberDeloreanClientRequest>>()), Times.Never);
        _loggerMock.VerifyLogging("No personal details found for member matching registration", LogLevel.Warning, Times.Once());
        _loggerMock.VerifyLogging($"Member registration successful for sub {TestData.Sub} and {TestData.Sub}", LogLevel.Information, Times.Once());
    }


    public async Task WhenGetLoginDetailsCalledWithValidData_ThenReturnLinkedRecordDataAndExpectedLoggingOccurred()
    {
        SetupValidRequest();
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(GetMemberAccessClientResponseFactory.Create(TestData.Sub));
        _mockMemberClient.Setup(x => x.GetBcUkLinkedRecord(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetLinkedRecordClientResponseFactory.Create(TestData.Bgroup));
        _authClientMock.Setup(x => x.CheckEligibility(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CheckEligibilityClientResponseFactory.Create(true));

        var result = await _sut.GetLoginDetails();

        AssertSuccess(result, LinkedRecordServiceResultDataFactory.Create(TestData.Bgroup, TestData.RefNo));
        _loggerMock.VerifyLogging($"Member access record found -  {TestData.Bgroup} {TestData.RefNo} for {TestData.Sub}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"Linked record found - {TestData.Bgroup} {TestData.RefNo}", LogLevel.Information, Times.Once());
    }

    public async Task WhenNoTenantHeaderFoundDuringLogin_ThenReturnErrorAndExpectedLoggingOccurred()
    {
        _httpContextAccessorMock.SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext());

        var result = await _sut.GetLoginDetails();

        AssertError(result, $"Required header {MdpConstants.BusinessGroupHeaderName} not found");
        _loggerMock.VerifyLogging($"Required header {MdpConstants.BusinessGroupHeaderName} not found", LogLevel.Error, Times.Once());
    }


    public async Task WhenNoSubClaimFoundDuringLogin_ThenReturnErrorAndExpectedLoggingOccurred()
    {
        _httpContextAccessorMock.SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext()
            {
                Request = { Headers = { { MdpConstants.BusinessGroupHeaderName, TestData.Bgroup } } }
            });

        var result = await _sut.GetLoginDetails();

        AssertError(result, $"Claim {MdpConstants.MemberClaimNames.Sub} not found in request token");
        _loggerMock.VerifyLogging($"Claim {MdpConstants.MemberClaimNames.Sub} not found in request token", LogLevel.Error, Times.Once());
    }

    public async Task WhenNoEligibleRecordFoundDuringLogin_ThenReturnEmptyResponseAndExpectedLoggingOccurred()
    {
        SetupValidRequest();
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(GetMemberAccessClientResponseFactory.Create(TestData.Sub));
        _mockMemberClient.Setup(x => x.GetBcUkLinkedRecord(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetLinkedRecordClientResponseFactory.Create(TestData.Bgroup));
        _authClientMock.Setup(x => x.CheckEligibility(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CheckEligibilityClientResponseFactory.Create(false));

        var result = await _sut.GetLoginDetails();

        AssertSuccess(result, new List<LinkedRecordServiceResultData>());
        _loggerMock.VerifyLogging($"Removed linked record - {TestData.Bgroup} {TestData.RefNo} as eligible status is False", LogLevel.Warning, Times.AtLeast(1));
    }

    public async Task WhenLinkedRecordsDoesNotBelongToLoginTenant_ThenReturnEmptyResponse()
    {
        SetupValidRequest();
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(GetMemberAccessClientResponseFactory.Create(TestData.Sub));
        _mockMemberClient.Setup(x => x.GetBcUkLinkedRecord(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetLinkedRecordClientResponseFactory.Create("OtherBgroup"));
        _authClientMock.Setup(x => x.CheckEligibility(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CheckEligibilityClientResponseFactory.Create(false));

        var result = await _sut.GetLoginDetails();

        AssertSuccess(result, new List<LinkedRecordServiceResultData>());
    }

    public async Task WhenGetLoginDetailsCalledAndHasSomeNLStatusRecord_ThenReturnLinkedRecordDataExceptNLAndExpectedLoggingOccurred()
    {
        string bgroup = "RBS";
        string refno = "123455";
        SetupValidRequest();
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(GetMemberAccessClientResponseFactory.Create(TestData.Sub, refno, bgroup));
        _mockMemberClient.Setup(x => x.GetBcUkLinkedRecord(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetLinkedRecordClientResponseFactory.Create(TestData.Bgroup, TestData.RefNo));
        _authClientMock.Setup(x => x.CheckEligibility(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CheckEligibilityClientResponseFactory.Create(true));
        _mockMemberClient.Setup(x => x.GetMemberSummary(It.IsAny<string>(), refno)).
            ReturnsAsync(GetMemberSummaryClientResponseFactory.CreateNL(bgroup));

        var result = await _sut.GetLoginDetails();

        AssertSuccess(result, LinkedRecordServiceResultDataFactory.Create(bgroup, TestData.RefNo));
        _loggerMock.VerifyLogging($"Member access record found -  {bgroup} {refno} for {TestData.Sub}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"Linked record found - {TestData.Bgroup} {TestData.RefNo}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"Since {refno} status is NL , continue with next record", LogLevel.Warning, Times.Once());
    }

    public async Task WhenNoTenantHeaderFound_ThenReturnErrorAndExpectedLoggingOccurred()
    {
        _httpContextAccessorMock.SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext());

        var result = await _sut.GetLinkedRecordTableData();

        AssertError(result, $"Required header {MdpConstants.BusinessGroupHeaderName} not found");
        _loggerMock.VerifyLogging($"Required header {MdpConstants.BusinessGroupHeaderName} not found", LogLevel.Error, Times.Once());
    }

    public async Task WhenNoSubClaimFoundDuringGetLinkedMemberTableData_ThenReturnErrorAndExpectedLoggingOccurred()
    {
        _httpContextAccessorMock.SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext()
            {
                Request = { Headers = { { MdpConstants.BusinessGroupHeaderName, TestData.Bgroup } } }
            });

        var result = await _sut.GetLinkedRecordTableData();

        AssertError(result, $"Claim {MdpConstants.MemberClaimNames.Sub} not found in request token");
        _loggerMock.VerifyLogging($"Claim {MdpConstants.MemberClaimNames.Sub} not found in request token", LogLevel.Error, Times.Once());
    }


    public async Task WhenLinkedRecordFoundOutsideTenant_ThenSetOutsideTenantFlagToTrue()
    {
        SetupValidRequest();
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(GetMemberAccessClientResponseFactory.Create(TestData.Sub));
        _mockMemberClient.Setup(x => x.GetBcUkLinkedRecord(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetLinkedRecordClientResponseFactory.Create("OtherBgroup"));
        _authClientMock.Setup(x => x.CheckEligibility(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(CheckEligibilityClientResponseFactory.Create(true));
        _mockMemberClient.Setup(x => x.GetMemberSummary(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetMemberSummaryClientResponseFactory.Create());
        _mockMemberClient.Setup(x => x.GetPensionDetails(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetPensionDetailsClientResponseFactory.Create());

        var result = await _sut.GetLinkedRecordTableData();

        result.IfRight(x => x.hasOutsideRecords.Should().BeTrue());
    }

    public async Task WhenLinkedRecordTableDataHasDuplicate_ThenRemoveDuplicateRecords()
    {
        SetupValidRequest();
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(GetMemberAccessClientResponseFactory.Create(TestData.Sub));
        _mockMemberClient.Setup(x => x.GetBcUkLinkedRecord(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetLinkedRecordClientResponseFactory.Create(TestData.Bgroup));
        _authClientMock.Setup(x => x.CheckEligibility(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(CheckEligibilityClientResponseFactory.Create(true));
        _mockMemberClient.Setup(x => x.GetMemberSummary(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetMemberSummaryClientResponseFactory.Create());
        _mockMemberClient.Setup(x => x.GetPensionDetails(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetPensionDetailsClientResponseFactory.Create());

        var result = await _sut.GetLinkedRecordTableData();
        AssertSuccess(result, LinkedRecordServiceResultFactory.CreateItem());

    }

    public async Task WhenCheckMemberAccessIsCalledWithValidData_ThenReturnTrueAndExpectedLoggingOccurred()
    {
        SetupValidRequest();
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(GetMemberAccessClientResponseFactory.Create(TestData.Sub));

        var result = await _sut.CheckMemberAccess(TestData.Sub, TestData.Bgroup);


        AssertSuccess(result, BgroupRefnoDataFactory.Create(TestData.Bgroup, TestData.RefNo, TestData.Bgroup, TestData.RefNo));
        _loggerMock.VerifyLogging("Member access record matching header values found", LogLevel.Information, Times.Once());
    }

    public async Task WhenBusinessGroupHeaderIsNotFoundDuringCheckMemberAccess_ThenReturnErrorAndExpectedLoggingOccurred()
    {
        SetupValidRequest();
        _httpContextAccessorMock.SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext()
            {
                Request = { Headers = { { MdpConstants.ReferenceNumberHeaderName, TestData.RefNo } } }
            });

        var result = await _sut.CheckMemberAccess(TestData.Sub, TestData.Bgroup);

        AssertError(result, $"Header {MdpConstants.BusinessGroupHeaderName} not found in request");
        _loggerMock.VerifyLogging($"Header {MdpConstants.BusinessGroupHeaderName} not found in request", LogLevel.Error, Times.Once());
    }

    public async Task WhenCheckMemberAccessIsCalledAndTenantBgroupDoesNotMatchBgroupHeader_ThenReturnErrorAndExpectedLoggingOccurred()
    {
        _httpContextAccessorMock.SetupGet(x => x.HttpContext)
           .Returns(new DefaultHttpContext()
           {
               Request = { Headers = { { MdpConstants.BusinessGroupHeaderName,"WIF" },
                                       { MdpConstants.ReferenceNumberHeaderName, TestData.RefNo }} }
           });

        var result = await _sut.CheckMemberAccess(TestData.Sub, TestData.Bgroup);

        AssertError(result, $"Bgroup header WIF and tenant bgroup header {TestData.Bgroup} don't match");
        _loggerMock.VerifyLogging($"Bgroup header WIF and tenant bgroup header {TestData.Bgroup} don't match", LogLevel.Error, Times.Once());
    }

    public async Task WhenMemberAccessDataNotFoundDuringGetMemberAccessCheck_ThenGetLinkedRecordsAndReturnMatchingHeaderRecordAndExpectedLoggingOccurred()
    {
        var memberAccessRefno = "111";
        SetupValidRequest();
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(GetMemberAccessClientResponseFactory.Create(It.IsAny<Guid>(), memberAccessRefno, TestData.Bgroup));
        _mockMemberClient.Setup(x => x.GetBcUkLinkedRecord(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(GetLinkedRecordClientResponseFactory.Create(TestData.Bgroup, TestData.RefNo));

        var result = await _sut.CheckMemberAccess(TestData.Sub, TestData.Bgroup);

        AssertSuccess(result, BgroupRefnoDataFactory.Create(TestData.Bgroup, TestData.RefNo, TestData.MainBgroup, memberAccessRefno));
        _loggerMock.VerifyLogging("Matching member access record not found in current tenant,checking linked record", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"Linked record found which has single auth access with refno {TestData.RefNo}", LogLevel.Information, Times.Once());

    }

    public async Task WhenHeaderBgroupRefNoNotFoundWhenCheckMemberAccessIsCalled_ThenReturnErrorAndExpectedLoggingOccurred()
    {
        SetupValidRequest();
        _authClientMock.Setup(x => x.GetMemberAccess(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(GetMemberAccessClientResponseFactory.Create(TestData.Sub, "Inactive"));
        _mockMemberClient.Setup(x => x.GetBcUkLinkedRecord(It.IsAny<string>(), It.IsAny<string>()))
          .ReturnsAsync(new GetLinkedRecordClientResponse());

        var result = await _sut.CheckMemberAccess(TestData.Sub, TestData.Bgroup);

        AssertError(result, "Member nor its link records have access");
        _loggerMock.VerifyLogging($"No active memberAccess record found for {TestData.Sub}", LogLevel.Warning, Times.Once());
        _loggerMock.VerifyLogging("Member nor its link records have access", LogLevel.Warning, Times.Once());
    }

    public void WhenIgnoreClaimTransformationCheckIsCalledWithLoginPath_ThenReturnTrue()
    {
        _httpContextAccessorMock.SetupGet(x => x.HttpContext)
           .Returns(new DefaultHttpContext()
           {
               Request = {
                   Path = "/api/login"
               }
           });

        var result = _sut.IgnoreClaimTransformationCheck();

        AssertSuccess(result, true);
    }

    public async Task WhenGetOutboundTokenIsCallesWithPrimaryRecord_ThenReturnToken()
    {
        SetupValidRequest();
        var recordNumber = 1;
        _authClientMock.Setup(x => x.GenerateToken(It.IsAny<OutboundSsoGenerateTokenClientRequest>()))
            .ReturnsAsync(OutboundSsoGenerateTokenClientResponseFactory.Create());

        var result = await _sut.GetOutboundToken(recordNumber, true);

        result.Should().Be(TestData.ValidToken);
    }

    public async Task WhenGetOutboundTokenIsCallesWithNonPrimaryRecordAndHasEpaUserSetup_ThenReturnToken()
    {
        SetupValidRequest();
        var recordNumber = 2;
        _authClientMock.Setup(x => x.GenerateToken(It.IsAny<OutboundSsoGenerateTokenClientRequest>()))
            .ReturnsAsync(OutboundSsoGenerateTokenClientResponseFactory.Create());

        _mockEpaClient.Setup(x => x.GetEpaUser(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetEpaUserClientResponseFactory.Create());

        var result = await _sut.GetOutboundToken(recordNumber, true);

        result.Should().Be(TestData.ValidToken);
    }

    public async Task WhenGetOutboundTokenIsCallesWithNonPrimaryRecordAndHasNooEpaUserSetup_ThenReturnToken()
    {
        var claims = new[]
       {
        new Claim(MdpConstants.MemberClaimNames.MainBusinessGroup,TestData.Bgroup),
        new Claim(MdpConstants.MemberClaimNames.MainReferenceNumber,TestData.RefNo),
         new Claim(MdpConstants.MemberClaimNames.BusinessGroup,TestData.Bgroup),
        new Claim(MdpConstants.MemberClaimNames.ReferenceNumber,TestData.RefNo)
        };
        _httpContextAccessorMock.SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            });
        var recordNumber = 2;
        _authClientMock.Setup(x => x.GenerateToken(It.IsAny<OutboundSsoGenerateTokenClientRequest>()))
            .ReturnsAsync(OutboundSsoGenerateTokenClientResponseFactory.Create());

        var result = await _sut.GetOutboundToken(recordNumber, true);

        result.Should().Be(TestData.ValidToken);
    }

    public void WhenIsAnonRequestIsCalledForAuthenticatedRequest_ThenReturnFalse()
    {
        SetupValidRequest();

        var result = _sut.IsAnonRequest();

        result.Should().BeFalse();
    }

    public void WhenAnonRequest_ThenReturnTrue()
    {
        var context = new DefaultHttpContext();
        var endpoint = new Endpoint(null, new EndpointMetadataCollection(new AllowAnonymousAttribute()), "TestEndpoint");
        var mockEndpointFeature = new Mock<IEndpointFeature>();
        mockEndpointFeature.Setup(e => e.Endpoint).Returns(endpoint);
        context.Features.Set(mockEndpointFeature.Object);
        _httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(context);

        var result = _sut.IsAnonRequest();

        result.Should().BeTrue();
    }

    void SetupValidRequest()
    {
        var claims = new[]
       {
        new Claim(MdpConstants.MemberClaimNames.Sub,TestData.Sub.ToString()),
        new Claim(MdpConstants.MemberClaimNames.ExternalId,TestData.Sub.ToString())
        };
        _httpContextAccessorMock.SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext
            {
                Request = { Headers = { { MdpConstants.BusinessGroupHeaderName, TestData.Bgroup },
                                        { MdpConstants.ReferenceNumberHeaderName, TestData.RefNo },
                                        { MdpConstants.CorrelationHeaderKey,Guid.NewGuid().ToString()} }  },
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            });
    }

    private static void AssertError<T>(Either<Error, T> result, string expectedErrorMessage)
    {
        result.IsLeft.Should().BeTrue();
        result.IfLeft(x => x.Should().BeOfType<Error>());
        result.IfLeft(x => x.Message.Should().Be(expectedErrorMessage));
        result.IsRight.Should().BeFalse();
    }

    private static void AssertSuccess<T>(Either<Error, T> result, T data)
    {
        result.IsLeft.Should().BeFalse();
        result.IfRight(x => x.Should().BeEquivalentTo(data));
        result.IsRight.Should().BeTrue();
    }

}

public static class LinkedRecordServiceResultDataFactory
{
    public static LinkedRecordServiceResultData Create()
    {
        return new LinkedRecordServiceResultData()
        {
            BusinessGroup = TestData.Bgroup,
            ReferenceNumber = TestData.RefNo,
            RecordNumber = TestData.RecordNumber,
            RecordType = TestData.RecordType,
        };
    }
    public static List<LinkedRecordServiceResultData> Create(string bgroup, string refno)
    {
        return new List<LinkedRecordServiceResultData>()
        {
            new LinkedRecordServiceResultData()
            {
                BusinessGroup =TestData.Bgroup,
                ReferenceNumber = TestData.RefNo,
            }
        };
    }

    public static List<LinkedRecordServiceResultData> CreateList(string bgroup, string refno)
    {
        return new List<LinkedRecordServiceResultData>()
        {
            new LinkedRecordServiceResultData()
            {
                BusinessGroup =TestData.Bgroup,
                ReferenceNumber = TestData.RefNo,
                 },
            new LinkedRecordServiceResultData()
            {
                BusinessGroup =bgroup,
                ReferenceNumber = refno,
            }
        };
    }
    public static List<LinkedRecordServiceResultData> CreateListWithRecordTypeAndRecordNumber()
    {
        return new List<LinkedRecordServiceResultData>()
        {
            Create(),
            Create()
        };
    }
}
public static class LinkedRecordServiceResultFactory
{
    public static LinkedRecordServiceResult Create()
    {
        return new LinkedRecordServiceResult()
        {
            Members = LinkedRecordServiceResultDataFactory.CreateListWithRecordTypeAndRecordNumber()
        };
    }
    public static LinkedRecordServiceResult CreateItem()
    {
        return new LinkedRecordServiceResult()
        {
            Members = new List<LinkedRecordServiceResultData>()
            {
                new LinkedRecordServiceResultData(){
                    BusinessGroup =TestData.Bgroup,
                    ReferenceNumber=TestData.RefNo,
                }
            }
        };
    }
}

public static class ContentResponseFactory
{
    public static ContentResponse CreateBlockEmail(bool blockEmail)
    {
        return new ContentResponse()
        {
            Elements = new ContentElement()
            {
                BlockSingleAuthWelcomeEmail = new ContentFlag()
                {
                    ElementType = "Toggle",
                    Value = blockEmail
                }
            }
        };
    }
    public static ContentResponse CreateBlockRelatedRegistration(bool blockRelatedRegistration)
    {
        return new ContentResponse()
        {
            Elements = new ContentElement()
            {
                BlockSaRelatedMemberDataRegistration = new ContentFlag()
                {
                    ElementType = "Toggle",
                    Value = blockRelatedRegistration
                }
            }
        };
    }
}

public static class BgroupRefnoDataFactory
{
    public static BgroupRefnoData Create()
    {
        return new BgroupRefnoData(TestData.Bgroup, TestData.RefNo, TestData.MainBgroup, TestData.MainRefNo);
    }
    public static BgroupRefnoData Create(string bgroup, string refno, string mainBgroup, string mainRefno)
    {
        return new BgroupRefnoData(bgroup, refno, mainBgroup, mainRefno);
    }
}