using System;
using System.Linq;
using FluentAssertions;
using WTW.MdpService.Domain.Mdp;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Mdp;

public class MemberQuoteTest
{
    public void CreateMemberQuote_HappyPath()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var memberQuote = MemberQuote.Create(utcNow,
            "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
            18, 19, 20, 21, 22, false, 85, 55,
            65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);

        var sut = memberQuote.Right();

        sut.SearchedRetirementDate.Should().Be(utcNow);
        sut.Label.Should().Be("label");
        sut.AnnuityPurchaseAmount.Should().Be(1010.99m);
        sut.LumpSumFromDb.Should().Be(10);
        sut.LumpSumFromDc.Should().Be(11);
        sut.SmallPotLumpSum.Should().Be(12);
        sut.TaxFreeUfpls.Should().Be(13);
        sut.TaxableUfpls.Should().Be(14);
        sut.TotalLumpSum.Should().Be(15);
        sut.TotalPension.Should().Be(16);
        sut.TotalSpousePension.Should().Be(17);
        sut.TotalUfpls.Should().Be(18);
        sut.TransferValueOfDc.Should().Be(19);
        sut.MinimumLumpSum.Should().Be(20);
        sut.MaximumLumpSum.Should().Be(21);
        sut.TrivialCommutationLumpSum.Should().Be(22);
        sut.HasAvcs.Should().Be(false);
        sut.LtaPercentage.Should().Be(85);
        sut.EarliestRetirementAge.Should().Be(55);
        sut.NormalRetirementAge.Should().Be(65);
        sut.NormalRetirementDate.Should().Be(utcNow);
        sut.DatePensionableServiceCommenced.Should().Be(utcNow);
        sut.DateOfLeaving.Should().Be(utcNow);
        sut.TransferInService.Should().Be("15");
        sut.TotalPensionableService.Should().Be("16");
        sut.FinalPensionableSalary.Should().Be(17);
        sut.CalculationType.Should().Be("PV");
        sut.WordingFlags.Should().Be("SPD");
        sut.StatePensionDeduction.Should().Be(9);
    }

    public void ReturnsError_OnCreate_WhenNoLabelPassed()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var memberQuote = MemberQuote.Create(utcNow,
            "", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
            18, 19, 20, 21, 22, false, 85, 55,
            65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);

        memberQuote.IsLeft.Should().BeTrue();
        memberQuote.Left().Message.Should().Be("Label should have a value.");
    }

    public void ReturnsError_OnCreate_WhenDefaultDatePassed()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var memberQuote = MemberQuote.Create(default,
            "", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
            18, 19, 20, 21, 22, false, 85, 55,
            65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);

        memberQuote.IsLeft.Should().BeTrue();
        memberQuote.Left().Message.Should().Be("Date should have non-default value.");
    }

    public void CreateMemberQuoteV2_HappyPath()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var memberQuote = MemberQuote.CreateV2(utcNow, "label", true, 10.1m, 55, 65, utcNow.AddYears(10),
            utcNow.AddYears(9), utcNow.AddYears(8), "15", "16", 17.1m, "PV", "SPD", 9.2m);

        var sut = memberQuote.Right();

        sut.SearchedRetirementDate.Should().Be(utcNow);
        sut.Label.Should().Be("label");
        sut.AnnuityPurchaseAmount.Should().BeNull();
        sut.LumpSumFromDb.Should().BeNull();
        sut.LumpSumFromDc.Should().BeNull();
        sut.SmallPotLumpSum.Should().BeNull();
        sut.TaxFreeUfpls.Should().BeNull();
        sut.TaxableUfpls.Should().BeNull();
        sut.TotalLumpSum.Should().BeNull();
        sut.TotalPension.Should().BeNull();
        sut.TotalSpousePension.Should().BeNull();
        sut.TotalUfpls.Should().BeNull();
        sut.TransferValueOfDc.Should().BeNull();
        sut.MinimumLumpSum.Should().BeNull();
        sut.MaximumLumpSum.Should().BeNull();
        sut.TrivialCommutationLumpSum.Should().BeNull();
        sut.HasAvcs.Should().BeTrue();
        sut.LtaPercentage.Should().Be(10.1m);
        sut.EarliestRetirementAge.Should().Be(55);
        sut.NormalRetirementAge.Should().Be(65);
        sut.NormalRetirementDate.Should().Be(utcNow.AddYears(10));
        sut.DatePensionableServiceCommenced.Should().Be(utcNow.AddYears(9));
        sut.DateOfLeaving.Should().Be(utcNow.AddYears(8));
        sut.TransferInService.Should().Be("15");
        sut.TotalPensionableService.Should().Be("16");
        sut.FinalPensionableSalary.Should().Be(17.1m);
        sut.CalculationType.Should().Be("PV");
        sut.WordingFlags.Should().Be("SPD");
        sut.StatePensionDeduction.Should().Be(9.2m);
    }

    public void ReturnsError_OnCreateV2_WhenNoLabelPassed()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var sut = MemberQuote.CreateV2(utcNow, null, true, 10.1m, 55, 65, utcNow.AddYears(10),
            utcNow.AddYears(9), utcNow.AddYears(8), "15", "16", 17.1m, "PV", "SPD", 9.2m);

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Label should have a value.");
    }

    public void ReturnsError_OnCreateV2_WhenDefaultDatePassed()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var sut = MemberQuote.CreateV2(DateTimeOffset.MinValue, "label", true, 10.1m, 55, 65, utcNow.AddYears(10),
            utcNow.AddYears(9), utcNow.AddYears(8), "15", "16", 17.1m, "PV", "SPD", 9.2m);

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Date should have non-default value.");
    }

    public void QuotesAreNotEqualWhenValuesAreNotEqual()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var sut1 = MemberQuote.CreateV2(utcNow, "label1", true, 10.1m, 55, 65, utcNow.AddYears(10),
            utcNow.AddYears(9), utcNow.AddYears(8), "15", "16", 17.1m, "PV", "SPD", 9.2m);
        var sut2 = MemberQuote.CreateV2(utcNow, "label", true, 10.1m, 55, 65, utcNow.AddYears(10),
            utcNow.AddYears(9), utcNow.AddYears(8), "15", "16", 17.1m, "PV", "SPD", 9.2m);

        sut1.IsRight.Should().BeTrue();
        sut1.IsLeft.Should().BeFalse();
        sut2.IsRight.Should().BeTrue();
        sut2.IsLeft.Should().BeFalse();
        sut1.Right().Should().NotBeEquivalentTo(sut2.Right());
    }

    public void QuotesAreEqualWhenAllValuesAreEqual()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var sut1 = MemberQuote.CreateV2(utcNow, "label", true, 10.1m, 55, 65, utcNow.AddYears(10),
            utcNow.AddYears(9), utcNow.AddYears(8), "15", "16", 17.1m, "PV", "SPD", 9.2m);
        var sut2 = MemberQuote.CreateV2(utcNow, "label", true, 10.1m, 55, 65, utcNow.AddYears(10),
            utcNow.AddYears(9), utcNow.AddYears(8), "15", "16", 17.1m, "PV", "SPD", 9.2m);

        sut1.IsRight.Should().BeTrue();
        sut1.IsLeft.Should().BeFalse();
        sut2.IsRight.Should().BeTrue();
        sut2.IsLeft.Should().BeFalse();
        sut1.Right().Should().BeEquivalentTo(sut2.Right());
    }

    public void ReturnsCorrectWordingFlagsList()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var sut = MemberQuote.CreateV2(utcNow, "label", true, 10.1m, 55, 65, utcNow.AddYears(10),
            utcNow.AddYears(9), utcNow.AddYears(8), "15", "16", 17.1m, "PV", "SPD;NPD;VSD;VAT;", 9.2m);

        var result = sut.Right().ParsedWordingFlags();

        result.Count().Should().Be(4);
        result.Contains("SPD").Should().BeTrue();
        result.Contains("NPD").Should().BeTrue();
        result.Contains("VSD").Should().BeTrue();
        result.Contains("VAT").Should().BeTrue();
    }
}