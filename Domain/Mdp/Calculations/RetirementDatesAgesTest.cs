using System;
using System.Text.Json;
using FluentAssertions;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.TestCommon.FixieConfig;
using WTW.Web.Models.Internal;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Domain.Mdp.Calculations;

public class RetirementDatesAgesTest
{
    public void CanCreateRetirementDatesAgesFromRetirementDatesAgesResponse()
    {
        var response = JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponse, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(response);

        sut.EarliestRetirementDate.Should().Be(DateTimeOffset.Parse("2017-11-07T00:00:00+00:00"));
        sut.NormalRetirementDate.Should().Be(DateTimeOffset.Parse("2022-11-07T00:00:00+00:00"));
        sut.LatestRetirementDate.Should().BeNull();
        sut.EarliestRetirementAge.Should().Be(55);
        sut.NormalRetirementAge.Should().Be(60);
        sut.LatestRetirementAge.Should().BeNull();
        sut.TargetRetirementDate.Value.Date.Should().Be(DateTime.Parse("2022-11-07"));
        sut.TargetRetirementAgeIso.Should().Be("P65Y20D");
        sut.WordingFlags.Should().BeEmpty();
    }

    public void CanCreateRetirementDatesAgesFromRetirementDatesAgesDto()
    {
        var response = JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(response);

        sut.EarliestRetirementDate.Should().Be(DateTimeOffset.Parse("2017-11-07T00:00:00+00:00"));
        sut.NormalRetirementDate.Should().Be(DateTimeOffset.Parse("2022-11-07T00:00:00+00:00"));
        sut.LatestRetirementDate.Should().BeNull();
        sut.EarliestRetirementAge.Should().Be(55);
        sut.NormalRetirementAge.Should().Be(60);
        sut.LatestRetirementAge.Should().BeNull();
        sut.TargetRetirementDate.Value.Date.Should().Be(DateTime.Parse("2022-11-07"));
        sut.TargetRetirementAgeIso.Should().Be("P65Y20D");
        sut.WordingFlags.Should().BeEmpty();
    }

    public void ReturnsEarliestRetirementDateWithAppliedRetirementProcessingPeriod1()
    {
        var date = new DateTimeOffset(new DateTime(2016, 2, 2));
        var response = JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(response);

        var result = sut.EarliestRetirementDateWithAppliedRetirementProcessingPeriod(37, date);

        result.Should().Be(new DateTime(2017, 11, 7));
    }

    public void ReturnsEarliestRetirementDateWithAppliedRetirementProcessingPeriod2()
    {
        var date = new DateTimeOffset(new DateTime(2022, 11, 15));
        var response = JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(response);

        var result = sut.EarliestRetirementDateWithAppliedRetirementProcessingPeriod(37, date);

        result.Should().Be(date.AddDays(37).Date);
    }

    public void ReturnsEffectiveDate1()
    {
        var date = new DateTimeOffset(new DateTime(2023, 11, 15));
        var response = JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(response);

        var result = sut.EffectiveDate(date, "RBS");

        result.Should().Be(date);
    }

    public void ReturnsEffectiveDate2()
    {
        var date = new DateTimeOffset(new DateTime(2021, 11, 15));
        var response = JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(response);

        var result = sut.EffectiveDate(date, "RBS");

        result.Should().Be(new DateTimeOffset(new DateTime(2022, 11, 07), TimeSpan.FromHours(0)));
    }

    public void ReturnsEffectiveDate22()
    {
        var date = new DateTimeOffset(new DateTime(2023, 11, 15));
        var response = JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(response);

        var result = sut.EffectiveDate(date, "BCL");

        result.Should().Be(date.AddMonths(6).AddDays(-1));
    }

    public void ReturnsEffectiveDate222()
    {
        var date = new DateTimeOffset(new DateTime(2022, 8, 8));
        var response = JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(response);

        var result = sut.EffectiveDate(date, "BCL");

        result.Should().Be(new DateTimeOffset(new DateTime(2022, 11, 7), TimeSpan.FromHours(0)));
    }

    public void ReturnsEffectiveDate4()
    {
        var date = new DateTimeOffset(new DateTime(2016, 11, 15), TimeSpan.FromHours(0));
        var response = JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(response);

        var result = sut.EffectiveDate(date, "BCL");

        result.Should().Be(new DateTimeOffset(new DateTime(2016, 11, 14), TimeSpan.FromHours(0)).AddMonths(6));
    }

    public void ReturnsNormalRetirementAge()
    {
        var response = JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(response);

        var result = sut.NormalRetirement();

        result.Should().Be(60);
    }

    public void ReturnsEarliestRetirementAge()
    {
        var response = JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(response);

        var result = sut.EarliestRetirement();

        result.Should().Be(55);
    }

    [Input("BCL", "2017-11-7", "2028-5-6", "2018-5-6")]
    [Input("BCL", "2017-11-7", "2018-3-6", "2018-3-6")]
    [Input("BCL", "2022-9-7", "2028-11-7", "2022-11-7")]
    [Input("BCL", "2023-9-7", "2028-03-06", "2024-03-06")]
    [Input("RBS", "2015-11-7", "2028-11-7", "2022-11-7")]
    [Input("RBS", "2024-11-7", "2028-11-7", "2024-11-7")]
    [Input("RBS", "2024-11-7", "2023-11-7", "2023-11-7")]
    public void ReturnsNormalRetirementDate(string businessGroup, string now, string latestRetirementDate, string expectedDate)
    {
        var response = JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(response);

        var result = sut.NormalRetirement(businessGroup, new DateTimeOffset(DateTime.Parse(now), TimeSpan.FromHours(0)), DateTime.Parse(latestRetirementDate));

        result.Should().Be(DateTime.Parse(expectedDate));
    }

    [Input(false, "2030-06-30", "exceededTRD")]
    [Input(true, "2030-07-01", "exceededTRD")]
    [Input(true, "2030-06-30", "exceededTRD")]
    [Input(true, "2029-06-30", "closeToTRD")]
    [Input(true, "2029-06-29", "approachingTRD")]
    [Input(true, "2027-06-29", "farFromTRD")]
    public void RetrievesCorrectDcLifeStageStatus(bool targetRetirementDateExists, string nowDate, string expectedStatus)
    {
        var sut = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(new RetirementDatesAgesDto
        {
            EarliestRetirementAge = 50,
            NormalMinimumPensionAge = 55,
            NormalRetirementAge = 62,
            EarliestRetirementDate = new DateTimeOffset(DateTime.Parse("2018-06-30")),
            NormalMinimumPensionDate = new DateTimeOffset(DateTime.Parse("2023-06-30")),
            NormalRetirementDate = new DateTimeOffset(DateTime.Parse("2030-06-30")),
            TargetRetirementDate = targetRetirementDateExists ? new DateTimeOffset(DateTime.Parse("2030-06-30")) : null
        });

        var result = sut.GetDCLifeStageStatus(new DateTimeOffset(DateTime.Parse(nowDate)));

        result.Should().Be(expectedStatus);
    }

    [Input("2025-02-28", "2022-11-07")]
    [Input("2022-02-28", "2022-08-28")]
    [Input("2022-07-28", "2022-11-07")]
    public void ReturnsCorrectLastAvailableQuoteDate(string nowDate, string expectedLatestAvailableDate)
    {
        var date = new DateTimeOffset(new DateTime(2016, 2, 2));
        var response = JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(response);

        var result = sut.LastAvailableQuoteDate(DateTimeOffset.Parse(nowDate),
            new BusinessGroup { Bgroup = "WPS", MinQuoteWindowIsoDuration = "P37D", MaxQuoteWindowIsoDuration = "P6M" });

        result.Date.Should().Be(DateTime.Parse(expectedLatestAvailableDate).Date);
    }

    [Input("2025-02-28", "2025-04-06")]
    [Input("2015-02-28", "2017-11-07")]
    [Input("2017-11-07", "2017-12-14")]
    public void ReturnsCorrectFirstAvailableQuoteDate(string nowDate, string expectedFirstAvailableDate)
    {
        var date = new DateTimeOffset(new DateTime(2016, 2, 2));
        var response = JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options());

        var sut = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(response);

        var result = sut.FirstAvailableQuoteDate(DateTimeOffset.Parse(nowDate),
            new BusinessGroup { Bgroup = "WPS", MinQuoteWindowIsoDuration = "P37D", MaxQuoteWindowIsoDuration = "P6M" });

        result.Date.Should().Be(DateTime.Parse(expectedFirstAvailableDate).Date);
    }
}