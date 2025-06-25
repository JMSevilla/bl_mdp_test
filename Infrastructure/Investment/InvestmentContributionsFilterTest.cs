using System.Text.Json;
using FluentAssertions;
using WTW.MdpService.Infrastructure.Investment;

namespace WTW.MdpService.Test.Infrastructure.Investment;

public class InvestmentContributionsFilterTest
{
    public void ReturnsTwoContributionTypes_WhenTwoContributionTypesReceived()
    {
        var latestContribution = JsonSerializer.Deserialize<LatestContributionResponse>(InvestmentTestData.LatestContributionJson);

        var sut = InvestmentContributionFilter.Filter(latestContribution);
        var latessst = string.Empty;
        sut.ContributionsList.Should().HaveCount(2);
    }

    public void ReturnsNoContributionTypes_WhenThreeContributionTypesWithNonZeroValueReceived()
    {
        var latestContribution = JsonSerializer.Deserialize<LatestContributionResponse>(InvestmentTestData.ThreeContributionTypesNonZeroValueJson);

        var sut = InvestmentContributionFilter.Filter(latestContribution);

        sut.ContributionsList.Should().HaveCount(0);
    }

    public void ReturnsTwoContributionTypes_WhenThreeContributionTypesWithOneZeroValueReceived()
    {
        var latestContribution = JsonSerializer.Deserialize<LatestContributionResponse>(InvestmentTestData.ThreeContributionTypesWithZeroValueJson);

        var sut = InvestmentContributionFilter.Filter(latestContribution);

        sut.ContributionsList.Should().HaveCount(2);
    }
}
