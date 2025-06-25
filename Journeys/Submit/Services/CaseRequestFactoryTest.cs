using System;
using System.Globalization;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Journeys.Submit.Services;
using WTW.MdpService.Journeys.Submit.Services.Dto;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Journeys.Submit.Services;

public class CaseRequestFactoryTest
{
    private readonly Mock<IJourneysRepository> _journeysRepositoryMock;
    private readonly Mock<IJsonConversionService> _jsonConversionServiceMock;
    private readonly Mock<ILogger<CaseRequestFactory>> _loggerMock;
    private readonly CaseRequestFactory _sut;
   
    public CaseRequestFactoryTest()
    {
        _journeysRepositoryMock = new Mock<IJourneysRepository>();
        _jsonConversionServiceMock = new Mock<IJsonConversionService>();
        _loggerMock = new Mock<ILogger<CaseRequestFactory>>();
        _sut = new CaseRequestFactory(
            _journeysRepositoryMock.Object,
            _jsonConversionServiceMock.Object,
            _loggerMock.Object);
    }

    public async Task ReturnsCorrectErrorMessage_WhenJourneyDoesNotsExist()
    {
        var result = await _sut.CreateForQuoteRequest("RBS", "1111122", "retirement");

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Member: RBS 1111122 does not have started journey with type: \"requestquote\".");
    }

    public async Task ReturnsCorrectErrorMessage_WhenStepDoesNotsExist()
    {
        var journey = new GenericJourney("RBS", "1234567", "requestquote", "step-0", "step-1", true, "Started", DateTimeOffset.UtcNow);
        _journeysRepositoryMock
            .Setup(x => x.Find("RBS", "1111122", "requestquote"))
            .ReturnsAsync(journey);

        var result = await _sut.CreateForQuoteRequest("RBS", "1111122", "retirement");

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Member: RBS 1111122 does not have step \"quote_choose_ret_date\" within journey type: \"requestquote\".");
    }

    public async Task ReturnsCorrectErrorMessage_WhenGenericDataDoesNotsExist()
    {
        var journey = new GenericJourney("RBS", "1234567", "requestquote", "quote_choose_ret_date", "step-1", true, "Started", DateTimeOffset.UtcNow);
        _journeysRepositoryMock
            .Setup(x => x.Find("RBS", "1111122", "requestquote"))
            .ReturnsAsync(journey);

        var result = await _sut.CreateForQuoteRequest("RBS", "1111122", "retirement");

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Member: RBS 1111122 does not have generic data with key \"date_picker_with_age_\" for step \"quote_choose_ret_date\" within journey type: \"requestquote\".");
    }

    public async Task ReturnsCorrectErrorMessage_WhenUnableToParseRetirementDate()
    {
        var journey = new GenericJourney("RBS", "1234567", "requestquote", "quote_choose_ret_date", "step-1", true, "Started", DateTimeOffset.UtcNow);
        var step = journey.GetStepByKey("quote_choose_ret_date");
        step.Value().UpdateGenericData("date_picker_with_age_", "\"retirementDate\":\"2024-02-02\"");
        _journeysRepositoryMock
            .Setup(x => x.Find("RBS", "1111122", "requestquote"))
            .ReturnsAsync(journey);

        _jsonConversionServiceMock
            .Setup(x => x.Deserialize<GenericDataForRetirmentDate>(It.IsAny<string>()))
            .Returns(new GenericDataForRetirmentDate { SelectedDate = null });

        var result = await _sut.CreateForQuoteRequest("RBS", "1111122", "retirement");

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Member: RBS 1111122. Unable parse retirement date from generic data.");
    }

    public async Task CreatesCaseApiRetirementQuoteRequestModel_WhenAllDataIsValid()
    {
        var journey = new GenericJourney("RBS", "1234567", "requestquote", "quote_choose_ret_date", "step-1", true, "Started", DateTimeOffset.UtcNow);
        var step = journey.GetStepByKey("quote_choose_ret_date");
        step.Value().UpdateGenericData("date_picker_with_age_", "\"retirementDate\":\"2024-02-02\"");
        _journeysRepositoryMock
            .Setup(x => x.Find("RBS", "1111122", "requestquote"))
            .ReturnsAsync(journey);

        _jsonConversionServiceMock
            .Setup(x => x.Deserialize<GenericDataForRetirmentDate>(It.IsAny<string>()))
            .Returns(new GenericDataForRetirmentDate { SelectedDate = new DateTime(2024, 2, 3) });

        var result = await _sut.CreateForQuoteRequest("RBS", "1111122", "retirement");

        result.IsLeft.Should().BeFalse();
        result.Right().BusinessGroup.Should().Be("RBS");
        result.Right().ReferenceNumber.Should().Be("1111122");
        result.Right().CaseCode.Should().Be("RTQ9");
        result.Right().BatchSource.Should().Be("MDP");
        result.Right().BatchDescription.Should().Be("Case created by an online application");
        result.Right().Narrative.Should().Be("");
        result.Right().Notes.Should().Be("Assure Calc fatal at 03/02/2024");
        result.Right().StickyNotes.Should().Be("Assure Calc fatal at 03/02/2024");
    }

    public async Task CreatesCaseApiTransferQuoteRequestModel()
    {
        var result = await _sut.CreateForQuoteRequest("RBS", "1111122", "transfer");

        result.Right().BusinessGroup.Should().Be("RBS");
        result.Right().ReferenceNumber.Should().Be("1111122");
        result.Right().CaseCode.Should().Be("TOQ9");
        result.Right().BatchSource.Should().Be("MDP");
        result.Right().BatchDescription.Should().Be("Case created by an online application");
        result.Right().Narrative.Should().Be("");
        result.Right().Notes.Should().Be($"Assure Calc fatal at {DateTime.UtcNow.ToString("d", CultureInfo.CreateSpecificCulture("en-GB"))}");
        result.Right().StickyNotes.Should().Be($"Assure Calc fatal at {DateTime.UtcNow.ToString("d", CultureInfo.CreateSpecificCulture("en-GB"))}");
    }

    [Input("AAA", "AAA")]
    [Input("TOP9", "TOP9")]
    [Input(null, "RTP9")]
    public async Task CreatesCaseApiGenericRetirementRequestModel(string caseCode, string expectedCaseCode)
    {
        var result = caseCode is null
            ? _sut.CreateForGenericRetirement("RBS", "1111122")
            : _sut.CreateForGenericRetirement("RBS", "1111122", caseCode);

        result.BusinessGroup.Should().Be("RBS");
        result.ReferenceNumber.Should().Be("1111122");
        result.CaseCode.Should().Be(expectedCaseCode);
        result.BatchSource.Should().Be("MDP");
        result.BatchDescription.Should().Be("Case created by an online application");
        result.Narrative.Should().Be("");
        result.Notes.Should().Be("Case created by an online application");
        result.StickyNotes.Should().Be("Case created by an online retirement application");
    }
}