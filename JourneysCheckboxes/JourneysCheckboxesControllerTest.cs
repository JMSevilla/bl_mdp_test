using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Infrastructure.BereavementDb;
using WTW.MdpService.Infrastructure.Journeys;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.JourneysCheckboxes;
using WTW.MdpService.Test.Domain.Bereavement;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.TestCommon.FixieConfig;
using WTW.Web.Errors;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.JourneysCheckboxes;

public class JourneysCheckboxesControllerTest
{
    private readonly Mock<IJourneyService> _journeyServiceMock;
    private readonly Mock<IMdpUnitOfWork> _mdpUnitOfWork;
    private readonly Mock<IBereavementUnitOfWork> _bereavementUnitOfWork;
    private readonly Mock<ILogger<JourneysCheckboxesController>> _loggerMock;
    private readonly JourneysCheckboxesController _sut;

    public JourneysCheckboxesControllerTest()
    {
        _journeyServiceMock = new Mock<IJourneyService>();
        _mdpUnitOfWork = new Mock<IMdpUnitOfWork>();
        _bereavementUnitOfWork = new Mock<IBereavementUnitOfWork>();
        _loggerMock = new Mock<ILogger<JourneysCheckboxesController>>();

        _sut = new JourneysCheckboxesController(
            _journeyServiceMock.Object,
            _mdpUnitOfWork.Object,
            _bereavementUnitOfWork.Object,
            _loggerMock.Object);

        SetupControllerContext();
    }

    [Input("nonExist", "current", "test1", "Journey is not started yet.")]
    public async Task SaveCheckboxesReturns404Response(string journeyType, string pageKey, string checkboxesListKey, string expectedErrorMessage)
    {
        _journeyServiceMock
            .Setup(x => x.GetJourney(journeyType, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journeyType == "transfer2" ? new TransferJourneyBuilder().BuildWithSteps() : Option<Journey>.None);

        var request = new SaveJourneyCheckboxesRequest
        {
            CheckboxesListKey = checkboxesListKey,
            Checkboxes = new List<SaveCheckboxRequest>
            {
                new SaveCheckboxRequest{Key = "checkboxKey1", AnswerValue=true},
                new SaveCheckboxRequest{Key = "checkboxKey2", AnswerValue=false}
            }
        };

        var result = await _sut.SaveCheckboxes(journeyType, pageKey, request);
        var response = (result as NotFoundObjectResult).Value as ApiError;

        result.Should().BeOfType<NotFoundObjectResult>();
        response.Errors[0].Message.Should().Be(expectedErrorMessage);
    }

    public async Task SaveCheckboxesReturns204ResponseWhenCurrentPageKeyDoesNotExist()
    {
        var transferJourney = new TransferJourneyBuilder()
            .CurrentPageKey("page_key_1")
            .NextPageKey("page_key_2")
            .BuildWithSteps();

        _journeyServiceMock
            .Setup(x => x.GetJourney("transfer2", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(transferJourney);

        var request = new SaveJourneyCheckboxesRequest
        {
            CheckboxesListKey = "test1",
            Checkboxes = new List<SaveCheckboxRequest>
            {
                new SaveCheckboxRequest{Key = "checkboxKey1", AnswerValue=true},
                new SaveCheckboxRequest{Key = "checkboxKey2", AnswerValue=false}
            }
        };

        var result = await _sut.SaveCheckboxes("transfer2", "page_key_2", request);
       
        result.Should().BeOfType<NoContentResult>();
    }

    public async Task SaveCheckboxesReturns204Response()
    {
        _journeyServiceMock
            .Setup(x => x.GetJourney("Retirement", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new RetirementJourneyBuilder().BuildWithSteps());

        var request = new SaveJourneyCheckboxesRequest
        {
            CheckboxesListKey = "test1",
            Checkboxes = new List<SaveCheckboxRequest>
            {
                new SaveCheckboxRequest{Key = "chekcboxKey1", AnswerValue=true},
                new SaveCheckboxRequest{Key = "chekcboxKey2", AnswerValue=false}
            }
        };

        var result = await _sut.SaveCheckboxes("Retirement", "current", request);

        result.Should().BeOfType<NoContentResult>();
    }

    [Input("nonExist", "currentPageKey", "test1")]
    [Input("Bereavement", "nonExist", "test1")]
    [Input("Bereavement", "currentPageKey", "test1")]
    public async Task GetCheckboxesReturns404Response(string journeyType, string pageKey, string checkboxesListKey)
    {
        SetupControllerContext("bereavement_reference_number", Guid.NewGuid().ToString());
        _journeyServiceMock
            .Setup(x => x.GetJourney("Bereavement", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new BereavementJourneyBuilder().Build().Right());

        var result = await _sut.Checkboxes(journeyType, pageKey, checkboxesListKey);
        var response = (result as NotFoundObjectResult).Value as ApiError;

        result.Should().BeOfType<NotFoundObjectResult>();
        response.Errors[0].Message.Should().Be("Not found");
    }

    public async Task GetCheckboxesReturns200Response()
    {
        SetupControllerContext("bereavement_reference_number", Guid.NewGuid().ToString());
        var journey = new BereavementJourneyBuilder().Build().Right();
        journey.GetStepByKey("currentPageKey").Value().AddCheckboxesList(new CheckboxesList("testKey", new List<(string, bool)> { ("a1", true) }));
        _journeyServiceMock
            .Setup(x => x.GetJourney("Bereavement", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        var result = await _sut.Checkboxes("Bereavement", "currentPageKey", "testKey");
        var response = (result as OkObjectResult).Value as JourneyCheckboxesResponse;

        result.Should().BeOfType<OkObjectResult>();
        response.CheckboxesListKey.Should().Be("testKey");
        response.Checkboxes.Single().Key.Should().Be("a1");
        response.Checkboxes.Single().AnswerValue.Should().BeTrue();
    }

    public async Task GetCheckboxesReturns200WithEmptyCheckboxesResponseForLastPage()
    {
        SetupControllerContext("bereavement_reference_number", Guid.NewGuid().ToString());
        var journey = new BereavementJourneyBuilder().NextPageKey("lastPage").Build().Right();
        _journeyServiceMock
            .Setup(x => x.GetJourney("Bereavement", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        var result = await _sut.Checkboxes("Bereavement", "lastPage", "testKey");
        var response = (result as OkObjectResult).Value as JourneyCheckboxesResponse;

        result.Should().BeOfType<OkObjectResult>();
        response.CheckboxesListKey.Should().Be("testKey");
        response.Checkboxes.Should().NotBeNull().And.BeEmpty();
    }

    private void SetupControllerContext(string referenceNumber = "reference_number", string value = "TestReferenceNumber")
    {
        var id = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("business_group", "TestBusinessGroup"),
            new Claim(referenceNumber, value),
        };

        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }
}