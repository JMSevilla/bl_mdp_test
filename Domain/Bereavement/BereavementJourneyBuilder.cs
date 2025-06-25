using System;
using LanguageExt;
using LanguageExt.Common;
using WTW.MdpService.Domain.Bereavement;

namespace WTW.MdpService.Test.Domain.Bereavement;

public class BereavementJourneyBuilder
{
    private string _businessGroup = "RBS";
    private Guid _referenceNumber = Guid.NewGuid();
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;
    private int _validityPeriodInMin = 1;
    private string _currentPageKey = "currentPageKey";
    private string _nextPageKey = "nextPageKey";

    public BereavementJourneyBuilder BusinessGroup(string businessGroup)
    {
        _businessGroup = businessGroup;
        return this;
    }

    public BereavementJourneyBuilder ReferenceNumber(Guid referenceNumber)
    {
        _referenceNumber = referenceNumber;
        return this;
    }

    public BereavementJourneyBuilder ValidityPeriodInMin(int validityPeriodInMin)
    {
        _validityPeriodInMin = validityPeriodInMin;
        return this;
    }

    public BereavementJourneyBuilder CurrentPageKey(string currentPageKey)
    {
        _currentPageKey = currentPageKey;
        return this;
    }

    public BereavementJourneyBuilder NextPageKey(string nextPageKey)
    {
        _nextPageKey = nextPageKey;
        return this;
    }

    public BereavementJourneyBuilder Date(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
        return this;
    }

    public Either<Error, BereavementJourney> Build()
    {
        return BereavementJourney.Create(
            _referenceNumber,
            _businessGroup,
            _utcNow,
            _currentPageKey,
            _nextPageKey,
            _validityPeriodInMin);
    }
}