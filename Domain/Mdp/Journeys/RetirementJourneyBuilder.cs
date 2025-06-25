using System;
using WTW.MdpService.Domain.Mdp;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Mdp.Journeys;

public class RetirementJourneyBuilder
{
    private string _businessGroup = "RBS";
    private string _referenceNumber = "0003994";
    private int _daysToExpire = 90;
    private int _processingInDays = 30;
    private string _selectedQuoteLabel = "label";
    private DateTimeOffset? SubmissionDate = null;

    public RetirementJourneyBuilder daysToExpire(int daysToExpire)
    {
        _daysToExpire = daysToExpire;
        return this;
    }

    public RetirementJourney Build()
    {
        var now = DateTimeOffset.UtcNow;
        var quote = MemberQuote.CreateV2(now,
           _selectedQuoteLabel, false, 85, 55,
           65, now, now, now, "15", "16", 17, "PV", "SPD", 1);

        return new(_businessGroup, _referenceNumber, now, null, null, quote.Right(), _daysToExpire, _processingInDays);
    }

    public RetirementJourney BuildWithSteps()
    {
        var now = DateTimeOffset.UtcNow;
        var quote = MemberQuote.CreateV2(now,
           "label", false, 85, 55,
           65, now, now, now, "15", "16", 17, "PV", "SPD", 1);

        return new(_businessGroup, _referenceNumber, now, "current", "step2", quote.Right(), _daysToExpire, _processingInDays);
    }

    public RetirementJourneyBuilder SelectedQuoteName(string name)
    {
        _selectedQuoteLabel = name;
        return this;
    }

    public RetirementJourney BuildWithSubmissionDate(DateTimeOffset submissionDate)
    {
        var now = DateTimeOffset.UtcNow;
        var quote = MemberQuote.CreateV2(now,
           "label", false, 85, 55,
           65, now, now, now, "15", "16", 17, "PV", "SPD", 1);
        SubmissionDate = submissionDate;

        return new(_businessGroup, _referenceNumber, now, null, null, quote.Right(), _daysToExpire, _processingInDays, SubmissionDate);
    }
}

