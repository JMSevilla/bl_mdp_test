using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Domain.Mdp.Calculations;

public class RetirementTest
{
    public void CanCreateRetirementFromRetirementResponse()
    {
        var response = JsonSerializer.Deserialize<RetirementResponse>(TestData.RetirementResponseJson, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response, "PV");

        sut.Should().NotBeNull();
        sut.CalculationEventType.Should().Be("PV");
        sut.DateOfBirth.Should().Be(DateTime.Parse("1962-11-07T00:00:00"));
        sut.DatePensionableServiceCommenced.Should().Be(DateTime.Parse("1980-08-05T00:00:00"));
        sut.DateOfLeaving.Should().Be(DateTime.Parse("2010-09-06T00:00:00"));
        sut.StatePensionDate.Should().BeNull();
        sut.StatePensionDeduction.Should().BeNull();
        sut.GMPAge.Should().Be("P60Y0M");
        sut.Post88GMPIncreaseCap.Should().Be(3);
        sut.Pre88GMPAtGMPAge.Should().Be(960.96m);
        sut.Post88GMPAtGMPAge.Should().Be(1604.2m);
        sut.TransferInService.Should().BeNull();
        sut.TotalPensionableService.Should().BeNull();
        sut.FinalPensionableSalary.Should().BeNull();
        sut.InternalAVCFundValue.Should().Be(0);
        sut.ExternalAvcFundValue.Should().Be(0);
        sut.TotalAvcFundValue.Should().Be(0);
        sut.StandardLifetimeAllowance.Should().Be(1073100m);
        sut.TotalLtaUsedPercentage.Should().Be(60.32m);
        sut.MaximumPermittedTotalLumpSum.Should().Be(165346.35m);
        sut.TotalLtaRemainingPercentage.Should().Be(0);
        sut.NormalMinimumPensionAge.Should().Be("P55Y0M");
        sut.Quotes.Single(x => x.Name == "FullPension").SequenceNumber.Should().Be(1);
        sut.Quotes.Single(x => x.Name == "FullPension").LumpSumFromDb.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").LumpSumFromDc.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").SmallPotLumpSum.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").TaxFreeUfpls.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").TaxableUfpls.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").TotalLumpSum.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").TotalPension.Should().Be(32369.4m);
        sut.Quotes.Single(x => x.Name == "FullPension").TotalSpousePension.Should().Be(12247.2m);
        sut.Quotes.Single(x => x.Name == "FullPension").TotalUfpls.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").TransferValueOfDc.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").TrivialCommutationLumpSum.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").AnnuityPurchaseAmount.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").MinimumLumpSum.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").MaximumLumpSum.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").MaximumLumpSum.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").PensionTranches.First().TrancheTypeCode.Should().Be("post88GMP");
        sut.Quotes.Single(x => x.Name == "FullPension").PensionTranches.First().IncreaseTypeCode.Should().Be("GMP");
        sut.Quotes.Single(x => x.Name == "FullPension").PensionTranches.First().Value.Should().Be(1604.2m);
        sut.WordingFlags.First().Should().Be("GMP");
    }

    public void CanCreateRetirementFromRetirementDto()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        sut.Should().NotBeNull();
        sut.CalculationEventType.Should().Be("PV");
        sut.DateOfBirth.Should().Be(DateTime.Parse("1962-11-07T00:00:00"));
        sut.DatePensionableServiceCommenced.Should().Be(DateTime.Parse("1980-08-05T00:00:00"));
        sut.DateOfLeaving.Should().Be(DateTime.Parse("2010-09-06T00:00:00"));
        sut.StatePensionDate.Should().BeNull();
        sut.StatePensionDeduction.Should().BeNull();
        sut.GMPAge.Should().Be("P60Y2M");
        sut.Post88GMPIncreaseCap.Should().Be(3);
        sut.Pre88GMPAtGMPAge.Should().Be(960.96m);
        sut.Post88GMPAtGMPAge.Should().Be(1604.2m);
        sut.TransferInService.Should().BeNull();
        sut.TotalPensionableService.Should().BeNull();
        sut.FinalPensionableSalary.Should().BeNull();
        sut.InternalAVCFundValue.Should().Be(0);
        sut.ExternalAvcFundValue.Should().Be(0);
        sut.TotalAvcFundValue.Should().Be(0);
        sut.StandardLifetimeAllowance.Should().Be(1073100m);
        sut.TotalLtaUsedPercentage.Should().Be(60.32m);
        sut.MaximumPermittedTotalLumpSum.Should().Be(165346.35m);
        sut.TotalLtaRemainingPercentage.Should().Be(0);
        sut.NormalMinimumPensionAge.Should().Be("P55Y3M");
        sut.Quotes.Single(x => x.Name == "FullPension").SequenceNumber.Should().Be(1);
        sut.Quotes.Single(x => x.Name == "FullPension").LumpSumFromDb.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").LumpSumFromDc.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").SmallPotLumpSum.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").TaxFreeUfpls.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").TaxableUfpls.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").TotalLumpSum.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").TotalPension.Should().Be(32369.4m);
        sut.Quotes.Single(x => x.Name == "FullPension").TotalSpousePension.Should().Be(12247.2m);
        sut.Quotes.Single(x => x.Name == "FullPension").TotalUfpls.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").TransferValueOfDc.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").TrivialCommutationLumpSum.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").AnnuityPurchaseAmount.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").MinimumLumpSum.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").MaximumLumpSum.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").MaximumLumpSum.Should().BeNull();
        sut.Quotes.Single(x => x.Name == "FullPension").PensionTranches.First().TrancheTypeCode.Should().Be("post88GMP");
        sut.Quotes.Single(x => x.Name == "FullPension").PensionTranches.First().IncreaseTypeCode.Should().Be("GMP");
        sut.Quotes.Single(x => x.Name == "FullPension").PensionTranches.First().Value.Should().Be(1604.2m);
        sut.WordingFlags.First().Should().Be("GMP");
    }

    public void CalculatesTotalPension()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.TotalPension();

        result.Should().Be(32369.4M);
    }

    public void CalculatesTotalPension2()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJsonNew, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.TotalPension();

        result.Should().Be(24801.96M);
    }

    public void CalculatesTotalAVCFund()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.TotalAVCFund();

        result.Should().BeNull();
    }

    public void CalculatesTotalAVCFund2()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJsonNew, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.TotalAVCFund();

        result.Should().Be(0.1m);
    }

    public void CalculatesIfHasAVC()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.IsAvc();

        result.Should().BeFalse();
    }

    public void CalculatesIfHasAVC2()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJsonNew, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.IsAvc();

        result.Should().BeTrue();
    }

    public void CalculatesIfHasAdditionalContributions()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.HasAdditionalContributions();

        result.Should().BeFalse();
    }

    public void CalculatesFullPensionYearlyIncome()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.FullPensionYearlyIncome();

        result.Should().Be(32369.4M);
    }

    public void CalculatesFullPensionYearlyIncome2()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJsonNew, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.FullPensionYearlyIncome();

        result.Should().Be(24801.96M);
    }

    public void CalculatesMaxLumpSum()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.MaxLumpSum();

        result.Should().Be(165346.35M);
    }

    public void CalculatesMaxLumpSum2()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJsonNew, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.MaxLumpSum();

        result.Should().BeNull();
    }

    public void CalculatesMaxLumpSumYearlyIncome()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.MaxLumpSumYearlyIncome();

        result.Should().Be(24801.96M);
    }

    public void CalculatesMaxLumpSumYearlyIncome2()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJsonNew, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.MaxLumpSumYearlyIncome();

        result.Should().BeNull();
    }

    public void FindsQuoteByName()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.FindQuote("ReducedPension");

        result.Should().NotBeNull();
    }

    public void ThowsIfQuoteNotFound()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var func = () => sut.FindQuote("NonExistingQuote");

        func.Should().Throw<InvalidOperationException>();
    }

    public void ReturnsWordingFlagsAsString()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.WordingFlagsAsString();

        result.Should().Be("GMP;GMPINPAY;GMPPOST88;GMPPRE88");
    }

    public void UpdatesWordingFlags()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        sut.UpdateWordingFlags(new List<string> { "A", "b", "C" });

        sut.WordingFlagsAsString().Should().Be("A;b;C");
    }

    public void ReturnsGMPAgeYears()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.GMPAgeYears();

        result.Should().Be(60);
    }

    public void ReturnsGMPAgeYears2()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJsonNew, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.GMPAgeYears();

        result.Should().BeNull();
    }

    public void GMPAgeMonths()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.GMPAgeMonths();

        result.Should().Be(2);
    }

    public void GMPAgeMonths2()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJsonNew, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.GMPAgeMonths();

        result.Should().BeNull();
    }

    public void ReturnsNormalMinimumPensionAgeYears()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.NormalMinimumPensionAgeYears();

        result.Should().Be(55);
    }

    public void ReturnsNormalMinimumPensionAgeYears2()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJsonNew, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.NormalMinimumPensionAgeYears();

        result.Should().BeNull();
    }

    public void GMPNormalMinimumPensionAgeMonths()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.NormalMinimumPensionAgeMonths();

        result.Should().Be(3);
    }

    public void GMPNormalMinimumPensionAgeMonths2()
    {
        var response = JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJsonNew, SerialiationBuilder.Options());
        var sut = new MdpService.Domain.Mdp.Calculations.Retirement(response);

        var result = sut.NormalMinimumPensionAgeMonths();

        result.Should().BeNull();
    }
}