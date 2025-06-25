using FluentAssertions;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.TestCommon.FixieConfig;

namespace WTW.MdpService.Test.Domain.Common;

public class QuestionFormTest
{
    public void CreatesQuestionForm()
    {
        var sut = new QuestionFromBuilder()
            .QuestionKey("test_question_key")
            .AnswerValue("test_answer_key")
            .Build();

        sut.AnswerKey.Should().Be("test_answer_key");
        sut.QuestionKey.Should().Be("test_question_key");
    }

    public void CreatesQuestionFormFromOtherQuestionForm()
    {
        var sut = new QuestionFromBuilder()
            .QuestionKey("test_question_key")
            .AnswerValue("test_answer_key")
            .Build();

        var result = sut.Duplicate();

        result.Should().NotBeNull();
        result.Should().NotBeSameAs(sut);
        result.QuestionKey.Should().Be("test_question_key");
        result.AnswerKey.Should().Be("test_answer_key");
    }

    [Input("question-key-1", "answer-key-1", "value-1")]
    [Input("", "", "")]
    [Input(null, null, null)]
    public void UpdatesQuestionFormFromOtherQuestionForm(string questioKey, string answerKey, string answerValue)
    {
        var sut = new QuestionFromBuilder()
            .QuestionKey("test_question_key")
            .AnswerValue("test_answer_key")
            .AnswerValue("value")
            .Build();

        var newQuestionForm = questioKey == null ? null : new QuestionForm(questioKey, answerKey, answerValue);
        sut.Update(newQuestionForm);

        sut.Should().NotBeNull();
        sut.Should().NotBeSameAs(newQuestionForm);
        sut.QuestionKey.Should().Be(questioKey);
        sut.AnswerKey.Should().Be(answerKey);
        sut.AnswerValue.Should().Be(answerValue);
    }
}