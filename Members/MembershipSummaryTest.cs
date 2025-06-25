using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using LanguageExt.Common;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Members;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Members;
using WTW.Web.Serialization;
using Calculations = WTW.MdpService.Domain.Mdp.Calculations;

namespace WTW.MdpService.Test.Members;

public class MembershipSummaryTest
{
    public void CreatesMembershipSummary1()
    {
        RetirementDatesAgesResponse retirementDatesAgesResponse = new RetirementDatesAgesResponse
        {
            RetirementAges = new RetirementAgesResponse
            {
                NormalRetirementAge = 65,
                EarliestRetirementAge = 60
            },
            RetirementDates = new RetirementDatesResponse
            {
                NormalRetirementDate = DateTimeOffset.UtcNow
            },
            WordingFlags = new List<string>(),
        };

        var member = new MemberBuilder().Build();
        var sut = new MembershipSummary(member, retirementDatesAgesResponse, new DateTimeOffset(new DateTime(2023, 6, 14)));

        sut.ReferenceNumber.Should().Be("0003994");
        sut.DateOfBirth.Should().Be(new DateTimeOffset(new DateTime(1985, 10, 10)));
        sut.Title.Should().Be("Mrs.");
        sut.Forenames.Should().Be("forenames");
        sut.Surname.Should().Be("surname");
        sut.InsuranceNumber.Should().Be("insuranceNumber");
        sut.MembershipNumber.Should().Be("memberNumber");
        sut.Status.Should().Be(MemberStatus.Active);
        sut.PayrollNumber.Should().Be("payrollNumber");
        sut.DateJoinedScheme.Should().Be(new DateTimeOffset(new DateTime(1985, 10, 10)));
        sut.DateLeftScheme.Should().BeNull();
        sut.SchemeName.Should().Be("name");
        sut.FloorRoundedAge.Should().Be(37);
        sut.Age.Should().Be(38);

        sut.HasAdditionalContributions.Should().BeFalse();
        sut.DateOfLeaving.Should().BeNull();
        sut.TransferInServiceYears.Should().BeNull();
        sut.TransferInServiceMonths.Should().BeNull();
        sut.TotalPensionableServiceYears.Should().BeNull();
        sut.TotalPensionableServiceMonths.Should().BeNull();
        sut.FinalPensionableSalary.Should().BeNull();
        sut.NormalRetirementAge.Should().Be(65);
        sut.NormalRetirementDate.Should().Be(retirementDatesAgesResponse.RetirementDates.NormalRetirementDate);
    }

    public void CreatesMembershipSummary11()
    {
        var retirementDatesAgesResponse = Result<RetirementDatesAgesResponse>.Bottom;

        var member = new MemberBuilder().Build();
        var sut = new MembershipSummary(member, retirementDatesAgesResponse, new DateTimeOffset(new DateTime(2023, 6, 14)));

        sut.ReferenceNumber.Should().Be("0003994");
        sut.DateOfBirth.Should().Be(new DateTimeOffset(new DateTime(1985, 10, 10)));
        sut.Title.Should().Be("Mrs.");
        sut.Forenames.Should().Be("forenames");
        sut.Surname.Should().Be("surname");
        sut.InsuranceNumber.Should().Be("insuranceNumber");
        sut.MembershipNumber.Should().Be("memberNumber");
        sut.Status.Should().Be(MemberStatus.Active);
        sut.PayrollNumber.Should().Be("payrollNumber");
        sut.DateJoinedScheme.Should().Be(new DateTimeOffset(new DateTime(1985, 10, 10)));
        sut.DateLeftScheme.Should().BeNull();
        sut.SchemeName.Should().Be("name");
        sut.FloorRoundedAge.Should().Be(37);
        sut.Age.Should().Be(38);

        sut.HasAdditionalContributions.Should().BeFalse();
        sut.DateOfLeaving.Should().BeNull();
        sut.TransferInServiceYears.Should().BeNull();
        sut.TransferInServiceMonths.Should().BeNull();
        sut.TotalPensionableServiceYears.Should().BeNull();
        sut.TotalPensionableServiceMonths.Should().BeNull();
        sut.FinalPensionableSalary.Should().BeNull();
        sut.NormalRetirementAge.Should().Be(0);
        sut.NormalRetirementDate.Should().Be(DateTimeOffset.MinValue);
    }

    public void CreatesMembershipSummary2()
    {
        var member = new MemberBuilder().Status(MemberStatus.Deferred).Build();
        var retirement = new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options()));
        var retirementDatesAges = new Calculations.RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options()));

        var sut = new MembershipSummary(member, retirementDatesAges, retirement, new DateTimeOffset(new DateTime(2023, 6, 14)));

        sut.ReferenceNumber.Should().Be("0003994");
        sut.DateOfBirth.Should().Be(new DateTimeOffset(new DateTime(1985, 10, 10)));
        sut.Title.Should().Be("Mrs.");
        sut.Forenames.Should().Be("forenames");
        sut.Surname.Should().Be("surname");
        sut.InsuranceNumber.Should().Be("insuranceNumber");
        sut.MembershipNumber.Should().Be("memberNumber");
        sut.Status.Should().Be(MemberStatus.Deferred);
        sut.PayrollNumber.Should().Be("payrollNumber");
        sut.DateJoinedScheme.Should().Be(new DateTimeOffset(new DateTime(1985, 10, 10)));
        sut.DateLeftScheme.Should().Be(new DateTimeOffset(new DateTime(1985, 10, 10)));
        sut.SchemeName.Should().Be("name");
        sut.FloorRoundedAge.Should().Be(37);
        sut.Age.Should().Be(38);

        sut.HasAdditionalContributions.Should().BeFalse();
        sut.DateOfLeaving.Should().Be(new DateTime(2010, 9, 6));
        sut.TransferInServiceYears.Should().BeNull();
        sut.TransferInServiceMonths.Should().BeNull();
        sut.TotalPensionableServiceYears.Should().BeNull();
        sut.TotalPensionableServiceMonths.Should().BeNull();
        sut.FinalPensionableSalary.Should().BeNull();
        sut.NormalRetirementAge.Should().Be(65);
        sut.NormalRetirementDate.Date.Should().Be(new DateTimeOffset(new DateTime(2022, 11, 7), TimeSpan.FromHours(0)).Date);
    }

    public void CreatesMembershipSummary3()
    {
        var member = new MemberBuilder()
            .Status(MemberStatus.Deferred)
            .DateOfBirth(null)
            .Build();
        var retirement = new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options()));
        var retirementDatesAges = new Calculations.RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options()));

        var sut = new MembershipSummary(member, retirementDatesAges, retirement, new DateTimeOffset(new DateTime(2023, 6, 14)));

        sut.ReferenceNumber.Should().Be("0003994");
        sut.DateOfBirth.Should().BeNull();
        sut.Title.Should().Be("Mrs.");
        sut.Forenames.Should().Be("forenames");
        sut.Surname.Should().Be("surname");
        sut.InsuranceNumber.Should().Be("insuranceNumber");
        sut.MembershipNumber.Should().Be("memberNumber");
        sut.Status.Should().Be(MemberStatus.Deferred);
        sut.PayrollNumber.Should().Be("payrollNumber");
        sut.DateJoinedScheme.Should().Be(new DateTimeOffset(new DateTime(1985, 10, 10)));
        sut.DateLeftScheme.Should().Be(new DateTimeOffset(new DateTime(1985, 10, 10)));
        sut.SchemeName.Should().Be("name");
        sut.FloorRoundedAge.Should().Be(0);
        sut.Age.Should().Be(0);

        sut.HasAdditionalContributions.Should().BeFalse();
        sut.DateOfLeaving.Should().Be(new DateTime(2010, 9, 6));
        sut.TransferInServiceYears.Should().BeNull();
        sut.TransferInServiceMonths.Should().BeNull();
        sut.TotalPensionableServiceYears.Should().BeNull();
        sut.TotalPensionableServiceMonths.Should().BeNull();
        sut.FinalPensionableSalary.Should().BeNull();
        sut.NormalRetirementAge.Should().Be(65);
        sut.NormalRetirementDate.Date.Should().Be(new DateTimeOffset(new DateTime(2022, 11, 7), TimeSpan.FromHours(0)).Date);
    }

    public void CreatesMembershipSummary_WithTargetRetirementDateAndAge()
    {
        var member = new MemberBuilder()
            .Status(MemberStatus.Deferred)
            .DateOfBirth(null)
            .Build();
        var retirement = new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options()));
        var retirementDatesAges = new Calculations.RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson2, SerialiationBuilder.Options()));

        var sut = new MembershipSummary(member, retirementDatesAges, retirement, new DateTimeOffset(new DateTime(2023, 6, 14)));

        sut.NormalRetirementAge.Should().Be(66);
        sut.NormalRetirementDate.Date.Date.Should().Be(new DateTimeOffset(new DateTime(2022, 12, 7), TimeSpan.FromHours(0)).Date);
    }

    public void CreatesMembershipSummary_WithNormalRetirementDateAndAge_WhenSRDFlagExists()
    {
        var member = new MemberBuilder()
            .Status(MemberStatus.Deferred)
            .DateOfBirth(null)
            .Build();
        var retirement = new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options()));
        var retirementDatesAges = new Calculations.RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson3, SerialiationBuilder.Options()));

        var sut = new MembershipSummary(member, retirementDatesAges, retirement, new DateTimeOffset(new DateTime(2023, 6, 14)));

        sut.NormalRetirementAge.Should().Be(60);
        sut.NormalRetirementDate.Should().Be(new DateTimeOffset(new DateTime(2022, 11, 7), TimeSpan.FromHours(0)));
    }

    public void CreatesCacheKey()
    {
        var result = MembershipSummary.CacheKey("RBS", "1111124");

        result.Should().Be("membership-summary-RBS-1111124");
    }
}