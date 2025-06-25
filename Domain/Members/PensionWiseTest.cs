using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class PensionWiseTest
{
    public void CreatePensionWiseRetirementCaseWithReasonForExemption_WhenPwAnswerKeyBProvided()
    {
        var financialAdviseDate = DateTimeOffset.UtcNow;
        var pensionWiseDate = DateTimeOffset.UtcNow;
        var pwAnswerKey = "pw_guidance_b";
        var sut = PensionWise
            .Create("BusinessGroup", "ReferenceNumber", "CaseNumber", financialAdviseDate,
                pensionWiseDate, pwAnswerKey);

        sut.BusinessGroup.Should().Be("BusinessGroup");
        sut.ReferenceNumber.Should().Be("ReferenceNumber");
        sut.SequenceNumber.Should().Be(0);
        sut.CaseNumber.Should().Be("CaseNumber");
        sut.FinancialAdviseDate.Should().Be(financialAdviseDate);
        sut.PensionWiseDate.Should().Be(pensionWiseDate);
        sut.ReasonForExemption.Should().Be("C");
        sut.PensionWiseSettlementCaseType.Should().Be("1");
    }
    
    public void CreatePensionWiseRetirementCaseWithReasonForExemption_WhenPwAnswerKeyCProvided()
    {
        var financialAdviseDate = DateTimeOffset.UtcNow;
        var pensionWiseDate = DateTimeOffset.UtcNow;
        var pwAnswerKey = "pw_guidance_c";
        var sut = PensionWise
            .Create("BusinessGroup", "ReferenceNumber", "CaseNumber", financialAdviseDate,
                pensionWiseDate, pwAnswerKey);

        sut.FinancialAdviseDate.Should().Be(financialAdviseDate);
        sut.PensionWiseDate.Should().Be(pensionWiseDate);
        sut.ReasonForExemption.Should().Be("B");
        sut.PensionWiseSettlementCaseType.Should().Be("1");
    }
    
    public void CreatePensionWiseRetirementCaseWithPwResponse_WhenPwAnswerKeyAProvided()
    {
        var financialAdviseDate = DateTimeOffset.UtcNow;
        var pensionWiseDate = DateTimeOffset.UtcNow;
        var pwAnswerKey = "pw_guidance_a";
        var sut = PensionWise
            .Create("BusinessGroup", "ReferenceNumber", "CaseNumber", financialAdviseDate,
                pensionWiseDate, pwAnswerKey);

        sut.FinancialAdviseDate.Should().Be(financialAdviseDate);
        sut.PensionWiseDate.Should().Be(pensionWiseDate);
        sut.PwResponse.Should().Be("4");
        sut.PensionWiseSettlementCaseType.Should().Be("1");
    }
    
    public void CreatePensionWiseRetirementCaseWithPwResponse_WhenPwAnswerKeyBProvided()
    {
        var financialAdviseDate = DateTimeOffset.UtcNow;
        var pensionWiseDate = DateTimeOffset.UtcNow;
        var pwAnswerKey = "pw_guidance_b";
        var sut = PensionWise
            .Create("BusinessGroup", "ReferenceNumber", "CaseNumber", financialAdviseDate,
                pensionWiseDate, pwAnswerKey);

        sut.FinancialAdviseDate.Should().Be(financialAdviseDate);
        sut.PensionWiseDate.Should().Be(pensionWiseDate);
        sut.PwResponse.Should().Be("1");
        sut.PensionWiseSettlementCaseType.Should().Be("1");
    }
    
    public void CreatePensionWiseRetirementCaseWithPwResponse_WhenPwAnswerKeyCProvided()
    {
        var financialAdviseDate = DateTimeOffset.UtcNow;
        var pensionWiseDate = DateTimeOffset.UtcNow;
        var pwAnswerKey = "pw_guidance_c";
        var sut = PensionWise
            .Create("BusinessGroup", "ReferenceNumber", "CaseNumber", financialAdviseDate,
                pensionWiseDate, pwAnswerKey);

        sut.FinancialAdviseDate.Should().Be(financialAdviseDate);
        sut.PensionWiseDate.Should().Be(pensionWiseDate);
        sut.PwResponse.Should().Be("1");
        sut.PensionWiseSettlementCaseType.Should().Be("1");
    }
    
    public void CreatePensionWiseRetirementCaseWithPwResponse_WhenPwAnswerKeyDProvided()
    {
        var financialAdviseDate = DateTimeOffset.UtcNow;
        var pensionWiseDate = DateTimeOffset.UtcNow;
        var pwAnswerKey = "pw_guidance_d";
        var sut = PensionWise
            .Create("BusinessGroup", "ReferenceNumber", "CaseNumber", financialAdviseDate,
                pensionWiseDate, pwAnswerKey);

        sut.FinancialAdviseDate.Should().Be(financialAdviseDate);
        sut.PensionWiseDate.Should().Be(pensionWiseDate);
        sut.PwResponse.Should().Be("2");
        sut.PensionWiseSettlementCaseType.Should().Be("1");
    }

    public void CreatePensionWiseRetirementCaseWithPwResponse_WhenPwAnswerKeyDefaultProvided()
    {
        var financialAdviseDate = DateTimeOffset.UtcNow;
        var pensionWiseDate = DateTimeOffset.UtcNow;
        var pwAnswerKey = "pw_guidance_default";
        var sut = PensionWise
            .Create("BusinessGroup", "ReferenceNumber", "CaseNumber", financialAdviseDate,
                pensionWiseDate, pwAnswerKey);

        sut.FinancialAdviseDate.Should().Be(financialAdviseDate);
        sut.PensionWiseDate.Should().Be(pensionWiseDate);
        sut.PwResponse.Should().BeNull();
        sut.ReasonForExemption.Should().BeNull();
        sut.PensionWiseSettlementCaseType.Should().Be("1");
    }
}