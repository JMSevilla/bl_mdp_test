using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.SingleAuth;
using WTW.MdpService.SingleAuth.Services;
using WTW.MdpService.Test.SingleAuth.Services;
using WTW.TestCommon;
using WTW.TestCommon.Helpers;
using WTW.Web;
using WTW.Web.Errors;
using Error = LanguageExt.Common.Error;


namespace WTW.MdpService.Test.SingleAuth;

public class SingleAuthControllerTest
{
    private readonly SingleAuthController _sut;
    private readonly Mock<ISingleAuthService> _singleAuthServiceMock;
    private readonly Mock<ILogger<SingleAuthController>> _loggerMock;
    public SingleAuthControllerTest()
    {
        _singleAuthServiceMock = new Mock<ISingleAuthService>();
        _loggerMock = new Mock<ILogger<SingleAuthController>>();
        _sut = new SingleAuthController(_singleAuthServiceMock.Object, _loggerMock.Object);
    }
    public async Task WhenUserRegistrationSuccessful_ThenReturn204Status()
    {
        _singleAuthServiceMock.Setup(x => x.RegisterUser(It.IsAny<string>())).ReturnsAsync(true);

        var result = await _sut.Registration(It.IsAny<string>());

        result.Should().BeOfType<NoContentResult>();
        var okResult = result as NoContentResult;
        okResult.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    public async Task WhenUserRegistrationValidationFails_ThenReturn400Status()
    {
        var error = $"No {MdpConstants.MemberClaimNames.Sub} or {MdpConstants.MemberClaimNames.ExternalId} claim found in provided token";
        _singleAuthServiceMock.Setup(x => x.RegisterUser(It.IsAny<string>())).ReturnsAsync(Error.New(error));

        var result = await _sut.Registration(It.IsAny<string>());

        var objectResult = result as BadRequestObjectResult;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        objectResult.Value.Should().BeOfType<ApiError>();
        var response = objectResult.Value as ApiError;
        response.Errors[0].Message.Should().BeEquivalentTo(error);
    }

    public async Task WhenUserRegistrationUpdateAuthFails_ThenThrowException()
    {
        _singleAuthServiceMock.Setup(x => x.RegisterUser(It.IsAny<string>())).Throws<HttpRequestException>();

        var result = async () => await _sut.Registration(It.IsAny<string>());

        await result.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task WhenMemberRegistered_ThenReturnLoginDetailsAndExpectedLoggingOccurred()
    {
        _singleAuthServiceMock.Setup(x => x.GetLoginDetails()).ReturnsAsync(LinkedRecordServiceResultDataFactory.CreateList(TestData.Bgroup, TestData.RefNo));

        var result = await _sut.Login();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeOfType<SingleAuthLoginResponse>();
        var response = okResult.Value as SingleAuthLoginResponse;
        response.Should().BeEquivalentTo(SingleAuthLoginResponseFactory.Create(true));
        _loggerMock.VerifyLogging($"login successful for {TestData.Bgroup} and {TestData.RefNo} and has multi record status as - True",
            LogLevel.Information, Times.Once());
    }
    public async Task WhenLoginCalledWithInvalidRequest_ThenReturnBadRequest()
    {
        var error = "BGROUP header not found";
        _singleAuthServiceMock.Setup(x => x.GetLoginDetails()).ReturnsAsync(Error.New(error));

        var result = await _sut.Login();

        result.Should().BeOfType<BadRequestObjectResult>();
        var objectResult = result as BadRequestObjectResult;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        objectResult.Value.Should().BeOfType<ApiError>();
        var response = objectResult.Value as ApiError;
        response.Errors[0].Message.Should().BeEquivalentTo(error);
    }

    public async Task WhenNoRegisteredUserFound_ThenReturnNotFoundResult()
    {
        _singleAuthServiceMock.Setup(x => x.GetLoginDetails()).ReturnsAsync(new List<LinkedRecordServiceResultData>());

        var result = await _sut.Login();

        result.Should().BeOfType<NotFoundObjectResult>();
        var objectResult = result as NotFoundObjectResult;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        _loggerMock.VerifyLogging("No Record Found", LogLevel.Warning, Times.Once());
    }

    public async Task WhenGetLinkedRecordIsCalledWithValidData_ThenReturnLinkedRecordsResponse()
    {
        _singleAuthServiceMock.Setup(x => x.GetLinkedRecordTableData()).ReturnsAsync(LinkedRecordServiceResultFactory.Create());

        var result = await _sut.GetLinkedRecord();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeOfType<LinkedRecordsResponse>();
        var response = okResult.Value as LinkedRecordsResponse;
        response.Should().BeEquivalentTo(LinkedRecordsResponseFactory.Create());
        response.Members.Should().AllSatisfy(member =>
        {
            member.RecordNumber.Should().Be(TestData.RecordNumber);
            member.RecordType.Should().Be(TestData.RecordType);
        });
    }

    public async Task WhenNoLinkedRecordFound_ThenNotFoundResponse()
    {
        _singleAuthServiceMock.Setup(x => x.GetLinkedRecordTableData()).ReturnsAsync(new LinkedRecordServiceResult());

        var result = await _sut.GetLinkedRecord();

        result.Should().BeOfType<NotFoundObjectResult>();
        var objectResult = result as NotFoundObjectResult;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    public async Task WhenGetLinkedRecordIsCalledWithInValidData_ThenReturnBadRequest()
    {
        var error = "BGROUP header not found";
        _singleAuthServiceMock.Setup(x => x.GetLinkedRecordTableData())
            .ReturnsAsync(LanguageExtHelper.SetLeft<LinkedRecordServiceResult>(error));

        var result = await _sut.GetLinkedRecord();

        result.Should().BeOfType<BadRequestObjectResult>();
        var objectResult = result as BadRequestObjectResult;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        objectResult.Value.Should().BeOfType<ApiError>();
        var response = objectResult.Value as ApiError;
        response.Errors[0].Message.Should().BeEquivalentTo(error);
    }

    public async Task WhenProcessOutboundIsCalledWithValidData_ThenReturnToken()
    {
        _singleAuthServiceMock.Setup(x => x.GetOutboundToken(It.IsAny<int>(), true))
            .ReturnsAsync(TestData.ValidToken);

        var result = await _sut.ProcessOutbound(1, true);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeOfType<ProcessOutboundResponse>();
        var response = okResult.Value as ProcessOutboundResponse;
        response.Should().BeEquivalentTo(ProcessOutboundResponseFactory.Create());
    }

}
public static class SingleAuthLoginResponseFactory
{
    public static SingleAuthLoginResponse Create(bool hasMultiple)
    {
        return new SingleAuthLoginResponse()
        {
            ReferenceNumber = TestData.RefNo,
            BusinessGroup = TestData.Bgroup,
            HasMultipleRecords = hasMultiple,
            EligibleRecords = new List<string>()
            {
                TestData.RefNo,
                TestData.RefNo
            }
        };
    }
}

public static class LinkedRecordsResponseFactory
{
    public static LinkedRecordsResponse Create()
    {
        return new LinkedRecordsResponse(LinkedRecordServiceResultFactory.Create());
    }
}

public static class ProcessOutboundResponseFactory
{
    public static ProcessOutboundResponse Create()
    {
        return new ProcessOutboundResponse()
        {
            LookupCode = TestData.ValidToken
        };
    }
}
