using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Cases;
using WTW.MdpService.Infrastructure.CasesApi;
using System.Threading.Tasks;
using LanguageExt;
using System.Linq;
using static LanguageExt.Prelude;
using FluentAssertions;
using WTW.Web.Pagination;

namespace WTW.MdpService.Test.Cases;

public class CasesControllerTest
{
    private readonly Mock<ICasesClient> _casesClientsMock;
    private readonly Mock<ILogger<CasesController>> _loggerMock;
    private readonly CasesController _sut;

    public CasesControllerTest()
    {
        _casesClientsMock = new Mock<ICasesClient>();
        _loggerMock = new Mock<ILogger<CasesController>>();
        _sut = new CasesController(_casesClientsMock.Object, _loggerMock.Object);

        SetupControllerContext();
    }

    public async Task GetCaseList_WhenCaseListIsReturned_ReturnsOk()
    {
        var request = new CaseListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            PropertyName = CaseSortPropertyName.CaseCode,
            Ascending = true
        };

        var caseList = new List<CasesResponse>
        {
            new ()
            {
                CaseCode = "TOP9",
                CaseStatus = "Created",
                CaseNumber = "2222222",
                CreationDate = "2023-10-10 00:00:00",
                CompletionDate = null
            },
            new ()
            {
                CaseCode = "RTP9",
                CaseStatus = "Closed",
                CaseNumber = "1111111",
                CreationDate = "2023-10-09 00:00:00",
                CompletionDate = "2023-10-09 00:00:00"
            }
        };

        _casesClientsMock
            .Setup(x => x.GetCaseList(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Right<CasesErrorResponse, IEnumerable<CasesResponse>>(caseList));

        var result = await _sut.GetCaseList(request);

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as PaginatedList<CaseListResponse>;
   
        response.TotalItems.Should().Be(2);
        response.Items[0].CaseCode.Should().Be("RTP9");
        response.Items[1].CaseCode.Should().Be("TOP9");
    }

    public async Task GetCaseList_WhenCaseNotFound_ReturnsNotFound()
    {
        var request = new CaseListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            PropertyName = CaseSortPropertyName.CaseCode,
            Ascending = true
        };

        var caseListError = new CasesErrorResponse
        {
            Error = "cw_get_cases_002",
            Detail = "No case found for bgroup refno"
        };

        _casesClientsMock
            .Setup(x => x.GetCaseList(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Left<CasesErrorResponse, IEnumerable<CasesResponse>>(caseListError));

        var result = await _sut.GetCaseList(request);

        result.Should().BeOfType<NotFoundObjectResult>();
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
