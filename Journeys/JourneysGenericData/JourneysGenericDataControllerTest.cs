using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Infrastructure.Journeys;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Journeys.JourneysGenericData;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.TestCommon.FixieConfig;
using WTW.Web.Errors;

namespace WTW.MdpService.Test.Journeys.JourneysGenericData;

public class JourneysGenericDataControllerTest
{
    private readonly Mock<IJourneyService> _journeyServiceMock;
    private readonly Mock<IMdpUnitOfWork> _mdpUnitOfWorkMock;
    private readonly Mock<ILogger<JourneysGenericDataController>> _logger;
    private readonly JourneysGenericDataController _sut;

    public JourneysGenericDataControllerTest()
    {
        _journeyServiceMock = new Mock<IJourneyService>();
        _mdpUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _logger = new Mock<ILogger<JourneysGenericDataController>>();
        
        _sut = new JourneysGenericDataController(_mdpUnitOfWorkMock.Object, _logger.Object, _journeyServiceMock.Object);

        SetupControllerContext();
    }

    public async Task GetGenericDataReturns200Response()
    {
        // Arrange
        var journeyType = "transfer2";
        var pageKey = "current";
        var formKey = "formKey";
        var genericDataJson = "{\"test\":\"test\"}";
        var journey = new TransferJourneyBuilder().CurrentPageKey(pageKey).BuildWithSteps();
        journey.JourneyBranches[0].JourneySteps[0].UpdateGenericData(formKey, genericDataJson);
        _journeyServiceMock
            .Setup(x => x.GetJourney(journeyType, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        // Act
        var result = await _sut.GetGenericData(journeyType, pageKey, formKey);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeOfType<JourneyGenericDataResponse>();
        var response = okResult.Value as JourneyGenericDataResponse;
        response.GenericDataJson.Should().BeEquivalentTo(genericDataJson);
    }

    public async Task GetGenericDataReturns200WithEmptyGenericDataResponseForLastPage()
    {
        // Arrange
        var journeyType = "transfer2";
        var pageKey = "current";
        var nextPageKey = "next";
        var formKey = "formKey";
        var genericDataJson = "{\"test\":\"test\"}";
        var journey = new TransferJourneyBuilder().CurrentPageKey(pageKey).NextPageKey(nextPageKey).BuildWithSteps();
        journey.JourneyBranches[0].JourneySteps[0].UpdateGenericData(formKey, genericDataJson);
        _journeyServiceMock
            .Setup(x => x.GetJourney(journeyType, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        // Act
        var result = await _sut.GetGenericData(journeyType, nextPageKey, formKey);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeOfType<JourneyGenericDataResponse>();
        var response = okResult.Value as JourneyGenericDataResponse;
        response.Should().NotBeNull();
        response.FormKey.Should().Be(formKey);
        response.GenericDataJson.Should().BeEmpty();
    }

    [Input("invalid", "current", "formKey")]
    [Input("transfer2", "invalidPageKey", "formKey")]
    [Input("transfer2", "current", "formKey")]
    public async Task GetGenericDataReturns404Response(string journeyType, string pageKey, string formKey)
    {
        // Arrange
        var journey = new TransferJourneyBuilder().CurrentPageKey("current").BuildWithSteps();
        _journeyServiceMock
            .Setup(x => x.GetJourney("transfer2", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        // Act
        var result = await _sut.GetGenericData(journeyType, pageKey, formKey);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    public async Task SaveGenericDataReturns204Response()
    {
        // Arrange
        var journeyType = "transfer2";
        var pageKey = "current";
        var formKey = "formKey";
        var genericDataJson = "{\"test\":\"test\"}";
        var journey = new TransferJourneyBuilder().CurrentPageKey(pageKey).BuildWithSteps();
        var request = new SaveJourneyGenericDataRequest
        {
            GenericDataJson = genericDataJson
        };

        _journeyServiceMock
            .Setup(x => x.GetJourney(journeyType, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        // Act
        var result = await _sut.SaveGenericData(journeyType, pageKey, formKey, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var okResult = result as NoContentResult;
        okResult.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Input("1", "2", "formKey", "2")]
    public async Task SaveGenericDataCreatesStepAndReturns204Response(string currentPageKey, string nextPageKey, string formKey, string requestCurrentPageKey)
    {
        // Arrange
        var journeyType = "transfer2";
        var genericDataJson = "{\"test\":\"test\"}";
        var journey = new TransferJourneyBuilder()
            .CurrentPageKey(currentPageKey)
            .NextPageKey(nextPageKey)
            .BuildWithSteps();

        var request = new SaveJourneyGenericDataRequest
        {
            GenericDataJson = genericDataJson
        };

        _journeyServiceMock
            .Setup(x => x.GetJourney(journeyType, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        // Act
        var result = await _sut.SaveGenericData(journeyType, requestCurrentPageKey, formKey, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var okResult = result as NoContentResult;
        okResult.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }


    [Input("Retirement", "current", "formKey", "Retirement journey is not started yet.", typeof(NotFoundObjectResult))]
    [Input("transfer2", "nonExist", "formKey", "Invalid \"currentPageKey\"", typeof(BadRequestObjectResult))]
    public async Task SaveGenericDataReturnsNotFound(string journeyType, string pageKey, string formKey, string expectedErrorMessage, Type type)
    {
        // Arrange
        var genericDataJson = "{\"test\":\"test\"}";
        var journey = new TransferJourneyBuilder().BuildWithSteps();
        var request = new SaveJourneyGenericDataRequest
        {
            GenericDataJson = genericDataJson
        };

        _journeyServiceMock
            .Setup(x => x.GetJourney(journeyType, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journeyType == "transfer2" ? journey : Option<Journey>.None);

        // Act
        var result = await _sut.SaveGenericData(journeyType, pageKey, formKey, request);

        // Assert
        result.Should().BeOfType(type);
        if(type == typeof(NotFoundObjectResult))
        {
            var response = (result as NotFoundObjectResult).Value as ApiError;
            response.Errors[0].Message.Should().Be(expectedErrorMessage);
        }
        else
        {
            var response = (result as BadRequestObjectResult).Value as ApiError;
            response.Errors[0].Message.Should().Be(expectedErrorMessage);
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