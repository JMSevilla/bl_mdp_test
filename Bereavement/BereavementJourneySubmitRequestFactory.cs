using System;
using System.Collections.Generic;
using WTW.MdpService.BereavementJourneys;

namespace WTW.MdpService.Test.Bereavement;

public class BereavementJourneySubmitRequestFactory
{
    public BereavementJourneySubmitRequest CreateValidRequest()
    {
        return new BereavementJourneySubmitRequest
        {
            TenantUrl = "http://example.com",
            Deceased = new BereavementJourneySubmitRequest.BereavementJourneyDeceasedPerson
            {
                Name = "John",
                Surname = "Doe",
                DateOfBirth = new DateTime(1950, 1, 1),
                DateOfDeath = new DateTime(2023, 1, 1),
                Address = CreateAddress(),
                Identification = new BereavementJourneySubmitRequest.DeceasedPersonIdentification
                {
                    Type = "Type",
                    NationalInsuranceNumber = "AB123456C",
                    PersonalPublicServiceNumber = "1234567TA",
                    PensionReferenceNumbers = new List<string> { "PRN123", "PRN456" }
                }
            },
            Reporter = CreatePerson(),
            NextOfKin = CreatePerson(),
            Executor = CreatePerson(),
            ContactPerson = CreatePerson()
        };
    }

    private BereavementJourneySubmitRequest.BereavementJourneyPerson CreatePerson()
    {
        return new BereavementJourneySubmitRequest.BereavementJourneyPerson
        {
            Name = "Jane",
            Surname = "Doe",
            Relationship = "Wife",
            Email = "jane.doe@example.com",
            PhoneCode = "123",
            PhoneNumber = "4567890",
            Address = CreateAddress()
        };
    }

    private static BereavementJourneySubmitRequest.BereavementJourneyAddressRequest CreateAddress()
    {
        return new BereavementJourneySubmitRequest.BereavementJourneyAddressRequest
        {
            Line1 = "100 Test St.",
            Line2 = "Apt. 10",
            Line3 = "TestCity",
            Line4 = "TestState",
            Line5 = "TestDistrict",
            Country = "TestCountry",
            CountryCode = "TC",
            PostCode = "10000"
        };
    }
}