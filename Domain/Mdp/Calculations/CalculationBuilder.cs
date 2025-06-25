using System;
using WTW.MdpService.Domain.Mdp.Calculations;

namespace WTW.MdpService.Test.Domain.Mdp.Calculations;

public class CalculationBuilder
{
    private string _referenceNumber = "0003994";
    private string _businessGroup = "RBS";
    private string _retirementDatesAgesJson = TestData.RetiremntDatesAgesJson;
    private string _retirementJson = TestData.RetiremntJson;
    private string _retirementJsonV2 = TestData.RetirementJsonV2;
    private string _transferQuote = TestData.TransferQuoteJson;
    private DateTime _effectiveRetirementDate = DateTime.UtcNow;
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;
    private bool? _isCalculationSuccessful = true;

    public CalculationBuilder EffectiveRetirementDate(DateTime effectiveRetirementDate)
    {
        _effectiveRetirementDate = effectiveRetirementDate;
        return this;
    }

    public CalculationBuilder CurrentDate(DateTimeOffset currentDate)
    {
        _utcNow = currentDate;
        return this;
    }

    public CalculationBuilder RetirementDatesAgesJson(string retirementDatesAgesJson)
    {
        _retirementDatesAgesJson = retirementDatesAgesJson;
        return this;
    }

    public CalculationBuilder RetirementJsonV2(string retirementJsonV2)
    {
        _retirementJsonV2 = retirementJsonV2;
        return this;
    }

    public CalculationBuilder SetCalculationSuccessStatus(bool? status)
    {
        _isCalculationSuccessful = status;
        return this;
    }

    public CalculationBuilder SetBusinessGroup(string businessGroup)
    {
        _businessGroup = businessGroup;
        return this;
    }

    public Calculation BuildV1()
    {
        return new Calculation(
            _referenceNumber,
            _businessGroup,
            _retirementDatesAgesJson,
            _retirementJson,
            _effectiveRetirementDate,
            _utcNow,
            _isCalculationSuccessful,
            false,
            null);
    }

    public Calculation BuildV2()
    {
        return new Calculation(
            _referenceNumber,
            _businessGroup,
            _retirementDatesAgesJson,
            _retirementJsonV2,
            _transferQuote,
            _effectiveRetirementDate,
            _utcNow,
            _isCalculationSuccessful);
    }
}
