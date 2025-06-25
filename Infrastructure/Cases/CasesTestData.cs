namespace WTW.MdpService.Test.Infrastructure.Cases;

public record CasesTestData
{
    public const string CaseListResponseJson = @"[
    {
        ""casecode"": ""GEN9"",
        ""caseStatus"": ""Closed"",
        ""creationDate"": ""2020-06-04 00:00:00"",
        ""completionDate"": ""2020-06-04 00:00:00"",
        ""caseno"": ""1320137""
    },
    {
        ""casecode"": ""FDC9"",
        ""caseStatus"": null,
        ""creationDate"": ""2020-10-05 00:00:00"",
        ""completionDate"": null,
        ""caseno"": ""1402697""
    },
    {
        ""casecode"": ""TOQ9"",
        ""caseStatus"": null,
        ""creationDate"": ""2020-09-15 00:00:00"",
        ""completionDate"": null,
        ""caseno"": ""1386617""
    },
    {
        ""casecode"": ""FDC9"",
        ""caseStatus"": null,
        ""creationDate"": ""2021-12-06 00:00:00"",
        ""completionDate"": null,
        ""caseno"": ""1603846""
    }
]";

    public const string CaseListErrorResponseJson = @"{
    ""error"": ""cw_get_cases_002"",
    ""detail"": ""No case found for bgroup LIF refno 111111 query_params {}"",
    ""message"": ""No cases found""
}";
}
