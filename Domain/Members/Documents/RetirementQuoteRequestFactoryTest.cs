using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members.Documents;

public class RetirementQuoteRequestFactoryTest
{
    private readonly RetirementQuoteRequestFactory _sut;

    public RetirementQuoteRequestFactoryTest()
    {
        _sut = new RetirementQuoteRequestFactory();
    }

    public void DocumentType_ShouldReturnRetirementQuoteRequest()
    {
        _sut.DocumentType.Should().Be(DocumentType.RetirementQuoteRequest);
    }

    public void Create_ShouldReturnValidDocument_WhenParametersAreValid()
    {
        string businessGroup = "RBS";
        string referenceNumber = "0003994";
        string caseNumber = "0003994";
        int id = 123456;
        int imageId = 654321;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var document = _sut.Create(businessGroup, referenceNumber, id, imageId, now, caseNumber);
        document.Should().NotBeNull();
        document.Type.Should().Be("Retirement Quote-Journey Summary(Assure)");
        document.FileName.Should().Be($"{businessGroup}-{referenceNumber}-{caseNumber}-{imageId}_mdp.pdf");
        document.BusinessGroup.Should().Be("RBS");
        document.ReferenceNumber.Should().Be("0003994");
        document.Date.Should().Be(now);
        document.LastReadDate.Should().BeNull();
        document.Name.Should().Be("Assure Retirement Quote");
        document.ImageId.Should().Be(654321);
        document.TypeId.Should().Be("RETQU_SUM");
        document.Id.Should().Be(123456);
    }
}