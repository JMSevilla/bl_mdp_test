using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Moq;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.Journeys;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.Web.LanguageExt;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Infrastructure.Journeys;

public class GenericJourneyDetailsTest
{
    private readonly Mock<IRetirementService> _retirementServiceMock;
    private readonly Mock<IJourneysRepository> _journeysRepositoryMock;
    private readonly Mock<IRetirementJourneyRepository> _retirementJourneyRepositoryMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<IJsonConversionService> _jsonConversionServiceMock;
    private readonly GenericJourneyDetails _sut;

    public GenericJourneyDetailsTest()
    {
        _retirementServiceMock = new Mock<IRetirementService>();
        _journeysRepositoryMock = new Mock<IJourneysRepository>();
        _retirementJourneyRepositoryMock = new Mock<IRetirementJourneyRepository>();
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _jsonConversionServiceMock = new Mock<IJsonConversionService>();
        _sut = new GenericJourneyDetails(
            _retirementServiceMock.Object,
            _journeysRepositoryMock.Object,
            _retirementJourneyRepositoryMock.Object,
            _calculationsParserMock.Object,
            _jsonConversionServiceMock.Object);
    }

    public void GetsAllJourneyDataFromGenericJourney()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var journey = new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", utcNow, utcNow.AddDays(2));
        journey.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow, "TestQuestionKey", "TestAnswerKey");
        journey.GetFirstStep().UpdateGenericData("preJourneyForm", "{\"formKey\":\"formValue\"}");

        var result = _sut.GetAll(journey);

        result.ExpirationDate.Should().Be(utcNow.AddDays(2));
        result.Type.Should().Be("transfer");
        result.Status.Should().Be("Started");
        result.PreJourneyData.First().Key.Should().Be("preJourneyForm");
    }

    public async Task GetsAllReturnsNone_WhenRetirementJourneyDoesNotExist()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var result = await _sut.GetAll(It.IsAny<string>(), It.IsAny<string>(), "dbretirementapplication");

        result.IsNone.Should().BeTrue();
    }

    public async Task GetsAllReturnsNone_WhenGenericJourneyDoesNotExist()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var result = await _sut.GetAll(It.IsAny<string>(), It.IsAny<string>(), "Generic");

        result.IsNone.Should().BeTrue();
    }

    public async Task GetsAllJourneyDataFromRetirementJourney()
    {
        var expectedJourney = new RetirementJourneyBuilder().BuildWithSteps();
        expectedJourney.SetCalculation(new CalculationBuilder().BuildV2());
        expectedJourney.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow, "questKet", "answKey");
        expectedJourney.TrySubmitStep("step3", "lta_summary_amount", DateTimeOffset.UtcNow);
        expectedJourney.TrySubmitStep("lta_summary_amount", "pw_guidance_a", DateTimeOffset.UtcNow);
        expectedJourney.TrySubmitStep("pw_guidance_a", "pw_guidance_c", DateTimeOffset.UtcNow);
        expectedJourney.TrySubmitStep("pw_guidance_c", "step4", DateTimeOffset.UtcNow);
        expectedJourney.GetStepByKey("step3").Value().UpdateGenericData("forma-key", "{\"property\":\"value\"}");
        expectedJourney.TrySubmitStep("step4", "step5", DateTimeOffset.UtcNow);
        expectedJourney.GetStepByKey("step4").Value().UpdateGenericData("full-name-form-key", "{\"name\":\"name\", \"surname\":\"surname\"}");
        expectedJourney.GetStepByKey("step3").Value()
            .AddCheckboxesList(new CheckboxesList("test-list-key", new List<(string, bool)> { ("check-box-key-test", true) }));
        _retirementJourneyRepositoryMock.Setup(repo => repo.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<RetirementJourney>.Some(expectedJourney));

        _retirementServiceMock
            .Setup(x => x.GetSelectedQuoteDetails(It.IsAny<string>(), It.IsAny<RetirementV2>()))
            .Returns(new Dictionary<string, object>());

        _jsonConversionServiceMock
            .Setup(x => x.Serialize(It.IsAny<object>()))
            .Returns("{\"test\":\"aa\"}");

        var utcNow = DateTimeOffset.UtcNow;
        var result = await _sut.GetAll(It.IsAny<string>(), It.IsAny<string>(), "dbretirementapplication");

        result.Value().ExpirationDate.Should().NotBeNull();
        result.Value().Type.Should().Be("dbretirementapplication");
        result.Value().Status.Should().Be("StartedRA");
        result.Value().PreJourneyData.First().Key.Should().Be("JourneySubmissionDetails");
        result.Value().StepsWithData.First().Key.Should().Be("lta_summary_amount");
        result.Value().StepsWithData.Should().HaveCount(5);
        result.Value().StepsWithQuestion.First().Key.Should().Be("step2");
        result.Value().StepsWithCheckboxes.Should().HaveCount(3);
        result.Value().StepsWithCheckboxes.Should().Contain(x => x.Key == "pw_guidance_d");
        result.Value().StepsWithCheckboxes.Should().Contain(x => x.Key == "submit_retirement_app");
        var checkBoxListValue = JsonSerializer.Serialize(result.Value().StepsWithCheckboxes.First().Value);
        var checkBoxListDictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(checkBoxListValue);
        checkBoxListDictionary.First().Key.Should().Be("pw_opt_out_form");
        checkBoxListDictionary.First().Value.ToString().Should().Be("{\"optOutPensionWise\":{\"AnswerValue\":false,\"Answer\":\"No\"}}");
    }

    public async Task GetsAllJourneyDataFromRetirementJourney_WhenPensionSummaryAmountPensionWiseAndFinancialAdviceStepsDoesNotExist()
    {
        var expectedJourney = new RetirementJourneyBuilder().BuildWithSteps();
        expectedJourney.SetCalculation(new CalculationBuilder().BuildV2());
        expectedJourney.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow, "questKet", "answKey");

        expectedJourney.TrySubmitStep("step3", "step4", DateTimeOffset.UtcNow);
        expectedJourney.GetStepByKey("step3").Value().UpdateGenericData("forma-key", "{\"property\":\"value\"}");
        expectedJourney.TrySubmitStep("step4", "step5", DateTimeOffset.UtcNow);
        expectedJourney.GetStepByKey("step3").Value()
            .AddCheckboxesList(new CheckboxesList("test-list-key", new List<(string, bool)> { ("check-box-key-test", true) }));
        _retirementJourneyRepositoryMock.Setup(repo => repo.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<RetirementJourney>.Some(expectedJourney));

        _retirementServiceMock
            .Setup(x => x.GetSelectedQuoteDetails(It.IsAny<string>(), It.IsAny<RetirementV2>()))
            .Returns(new Dictionary<string, object>());

        _jsonConversionServiceMock
            .Setup(x => x.Serialize(It.IsAny<object>()))
            .Returns("{\"test\":\"aa\"}");

        var utcNow = DateTimeOffset.UtcNow;
        var result = await _sut.GetAll(It.IsAny<string>(), It.IsAny<string>(), "dbretirementapplication");

        result.Value().ExpirationDate.Should().NotBeNull();
        result.Value().Type.Should().Be("dbretirementapplication");
        result.Value().Status.Should().Be("StartedRA");
        result.Value().PreJourneyData.First().Key.Should().Be("JourneySubmissionDetails");
        result.Value().StepsWithData.First().Key.Should().Be("step3");
        result.Value().StepsWithData.Should().HaveCount(1);
        result.Value().StepsWithQuestion.First().Key.Should().Be("step2");
        result.Value().StepsWithCheckboxes.Should().HaveCount(3);
        result.Value().StepsWithCheckboxes.First().Key.Should().Be("pw_guidance_d");
    }
}