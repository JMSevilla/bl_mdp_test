using WTW.MdpService.Domain.Common.Journeys;

namespace WTW.MdpService.Test.Domain.Common;

public class QuestionFromBuilder
{
    public string _questionKey = "test_question_key";
    public string _answerKey = "test_answer_key";
    public string _answerValue = null;

    public QuestionFromBuilder QuestionKey(string questionKey)
    {
        _questionKey = questionKey;
        return this;
    }

    public QuestionFromBuilder AnswerKey(string answerKey)
    {
        _answerKey = answerKey;
        return this;
    }

    public QuestionFromBuilder AnswerValue(string answerValue)
    {
        _answerValue = answerValue;
        return this;
    }

    public QuestionForm Build()
    {
        return new QuestionForm(_questionKey, _answerKey, _answerValue);
    }
}