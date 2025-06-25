using System;
using System.Collections.Generic;
using WTW.MdpService.Domain.Mdp;

namespace WTW.MdpService.Test.Domain.Mdp.Journeys
{
    public class TransferJourneyBuilder
    {
        private string _businessGroup = "RBS";
        private string _referenceNumber = "0304442";
        private string _currentPageKey = "current";
        private string _nextPageKey = "transfer_start_1";
        private int _transferImageId = 10;
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        private string _fileUuid = Guid.NewGuid().ToString();

        public TransferJourney Build()
        {
            return TransferJourney
                .Create(_businessGroup, _referenceNumber, _now, _transferImageId);
        }

        public TransferJourney BuildWithSteps()
        {
            return TransferJourney
                .Create(_businessGroup, _referenceNumber, _now, _currentPageKey, _nextPageKey, _transferImageId);
        }

        public TransferJourneyBuilder CurrentPageKey(string currentPagekey)
        {
            _currentPageKey = currentPagekey;
            return this;
        }

        public TransferJourneyBuilder NextPageKey(string nextPageKey)
        {
            _nextPageKey = nextPageKey;
            return this;
        }

        public TransferJourneyBuilder Date(DateTimeOffset now)
        {
            _now = now;
            return this;
        }
    }
}