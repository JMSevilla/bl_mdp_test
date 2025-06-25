using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Documents;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Infrastructure.CasesApi;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Journeys.Submit.Services;
using WTW.TestCommon.FixieConfig;
using WTW.Web.Errors;
using static LanguageExt.Prelude;

namespace WTW.MdpService.Test.Documents;

public class CaseDocumentsControllerTest
{
    private readonly CaseDocumentsController _sut;
    private readonly Mock<ICaseDocumentsService> _caseDocumentsServiceMock;
    private readonly Mock<ICasesClient> _casesClientMock;
    private readonly Mock<ILogger<CaseDocumentsController>> _loggerMock;
    public CaseDocumentsControllerTest()
    {
        _caseDocumentsServiceMock = new Mock<ICaseDocumentsService>();
        _casesClientMock = new Mock<ICasesClient>();
        _loggerMock = new Mock<ILogger<CaseDocumentsController>>();
        _sut = new CaseDocumentsController(

            _casesClientMock.Object,
            _loggerMock.Object,
            _caseDocumentsServiceMock.Object);

        SetupControllerContext();
    }

    [Input(true, true, false, typeof(BadRequestObjectResult), "Error from api")]
    [Input(true, true, true, typeof(OkObjectResult), "123456")]
    public async Task ListCaseDocumentsReturnsCorrectHttpStatusCodeAndContent(bool journeyExists, bool isJourneySubmitted, bool caseApiReturnsDocs, Type expectedType, string expectedMessage)
    {
        var journey = TransferJourney.Create("TestBusinessGroup", "TestReferenceNumber", DateTimeOffset.UtcNow, 1);
        var journeyOption = journeyExists ? journey : Option<TransferJourney>.None;
        if (isJourneySubmitted)
            journey.Submit("123456", DateTimeOffset.UtcNow);

        _caseDocumentsServiceMock
            .Setup(x => x.GetCaseNumber(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Right<Error, string>("123456"));

        Either<DocumentsErrorResponse, DocumentsResponse> documentsResponse = caseApiReturnsDocs ?
            new DocumentsResponse
            {
                CaseNumber = "123456",
                CaseCode = "TOP9",
                Documents = new List<WTW.MdpService.Infrastructure.CasesApi.DocumentResponse>
                {
                    new WTW.MdpService.Infrastructure.CasesApi.DocumentResponse
                    {
                        DocId = "TVOUT",
                        ImageId = 123456,
                        Narrative = "Transfer request form",
                        DateReceived = new DateTimeOffset(new DateTime(2023,05,01)),
                        Status = "MIR",
                        Notes = "Missing information"
                    }
                },
            } : new DocumentsErrorResponse() { Detail = "Error from api" };
        _casesClientMock.Setup(x => x.ListDocuments(It.IsAny<string>(), "123456")).ReturnsAsync(documentsResponse);

        var result = await _sut.ListCaseDocuments(new CaseDocumentsRequest { CaseCode = "Transfer" });

        result.Should().BeOfType(expectedType);

        switch (result)
        {
            case BadRequestObjectResult badRequestObjectResult when expectedType == typeof(BadRequestObjectResult):
                {
                    var apiErrorResult = result as ObjectResult;
                    var apiError = apiErrorResult?.Value as ApiError;
                    apiError.Should().NotBeNull();
                    apiError.Errors.Should().Contain(e => e.Message == expectedMessage);
                }
                break;
            case OkObjectResult okObjectResult when expectedType == typeof(OkObjectResult):
                {
                    var caseDocumentsResponse = okObjectResult?.Value as CaseDocumentsResponse;
                    caseDocumentsResponse?.CaseNumber.Should().Be(expectedMessage);
                }
                break;
            default:
                throw new Exception("Unexpected response type.");
        }
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