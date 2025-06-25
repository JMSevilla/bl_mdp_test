using System;
using System.Text.Json;
using FluentAssertions;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Domain.Mdp.Calculations;

public class TransferQuoteTest
{
    public void CanCreateTransferQuote()
    {
        var dto = JsonSerializer.Deserialize<TransferQuoteDto>(TestData.TransferQuoteJson, SerialiationBuilder.Options());
        var sut = new TransferQuote(dto);

        sut.GuaranteeDate.Value.Date.Should().Be(new DateTime(2022, 11, 12));
        sut.GuaranteePeriod.Should().Be("P3M");
        sut.ReplyByDate.Value.Date.Should().Be(new DateTime(2022, 10, 22));
        sut.TotalPensionAtDOL.Should().Be(19514.56M);
        sut.MaximumResidualPension.Should().Be(17910.46M);
        sut.MinimumResidualPension.Should().Be(0.1m);
        sut.TransferValues.TotalTransferValue.Should().Be(1220780.55M);
        sut.TransferValues.MinimumPartialTransferValue.Should().Be(537223.23M);
        sut.TransferValues.TotalGuaranteedTransferValue.Should().Be(1074446.46M);
        sut.TransferValues.TotalNonGuaranteedTransferValue.Should().Be(146334.09M);
        sut.IsGuaranteedQuote.Should().BeTrue();
        sut.OriginalEffectiveDate.Value.Date.Should().Be(new DateTime(2022, 11, 12));
    }

    public void CanCreateTransferQuote2()
    {
        var response = JsonSerializer.Deserialize<TransferResponse>(TestData.TransferQuoteResponseJson, SerialiationBuilder.Options());
        var sut = new TransferQuote(response);

        sut.GuaranteeDate.Value.Date.Should().Be(new DateTime(2023, 02, 16));
        sut.GuaranteePeriod.Should().Be("P3M");
        sut.ReplyByDate.Value.Date.Should().Be(new DateTime(2023, 01, 26));
        sut.TotalPensionAtDOL.Should().Be(739.0M);
        sut.MaximumResidualPension.Should().Be(369.5M);
        sut.MinimumResidualPension.Should().Be(0);
        sut.TransferValues.TotalTransferValue.Should().Be(23249.08M);
        sut.TransferValues.MinimumPartialTransferValue.Should().Be(11094.81M);
        sut.TransferValues.TotalGuaranteedTransferValue.Should().Be(22189.62M);
        sut.TransferValues.TotalNonGuaranteedTransferValue.Should().Be(1059.46M);
        sut.IsGuaranteedQuote.Should().BeFalse();
        sut.OriginalEffectiveDate.Value.Date.Should().Be(new DateTime(2022, 11, 16));
    }

    public void CanCreateTransferValues()
    {
        var response = JsonSerializer.Deserialize<TransferResponse>(TestData.TransferQuoteResponseJson, SerialiationBuilder.Options());
        var sut = new TransferValues(response.Results.Mdp.TransferValues);

        sut.TotalTransferValue.Should().Be(23249.08M);
        sut.MinimumPartialTransferValue.Should().Be(11094.81M);
        sut.TotalGuaranteedTransferValue.Should().Be(22189.62M);
        sut.TotalNonGuaranteedTransferValue.Should().Be(1059.46M);
    }

    public void CanCreateTransferValues2()
    {
        var dto = JsonSerializer.Deserialize<TransferQuoteDto>(TestData.TransferQuoteJson, SerialiationBuilder.Options());
        var sut = new TransferValues(dto.TransferValues);

        sut.TotalTransferValue.Should().Be(1220780.55M);
        sut.MinimumPartialTransferValue.Should().Be(537223.23M);
        sut.TotalGuaranteedTransferValue.Should().Be(1074446.46M);
        sut.TotalNonGuaranteedTransferValue.Should().Be(146334.09M);
    }

    public void ReturnsBothType()
    {
        var dto = JsonSerializer.Deserialize<TransferQuoteDto>(TestData.TransferQuoteJson, SerialiationBuilder.Options());
        var sut = new TransferValues(dto.TransferValues);

        var result = sut.Type();

        result.Should().Be("Both");
    }

    public void ReturnsGuaranteedType()
    {
        var response = JsonSerializer.Deserialize<TransferResponse>(TestData.TransferQuoteResponseJson, SerialiationBuilder.Options());
        response.Results.Mdp.TransferValues.TotalGuaranteedTransferValue = 3m;
        response.Results.Mdp.TransferValues.TotalNonGuaranteedTransferValue = 0;
        var sut = new TransferValues(response.Results.Mdp.TransferValues);

        var result = sut.Type();

        result.Should().Be("Guaranteed");
    }

    public void ReturnsNotGuaranteedType()
    {
        var response = JsonSerializer.Deserialize<TransferResponse>(TestData.TransferQuoteResponseJson, SerialiationBuilder.Options());
        response.Results.Mdp.TransferValues.TotalGuaranteedTransferValue = 0;
        response.Results.Mdp.TransferValues.TotalNonGuaranteedTransferValue = 2m;
        var sut = new TransferValues(response.Results.Mdp.TransferValues);

        var result = sut.Type();

        result.Should().Be("NotGuaranteed");
    }
}