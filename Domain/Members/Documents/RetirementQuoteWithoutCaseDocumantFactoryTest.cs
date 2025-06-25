using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members.Documents;

public class RetirementQuoteWithoutCaseDocumantFactoryTest
{
    private readonly RetirementQuoteWithoutCaseDocumantFactory _sut;

    public RetirementQuoteWithoutCaseDocumantFactoryTest()
    {
        _sut = new RetirementQuoteWithoutCaseDocumantFactory();
    }

    public void DocumentType_ShouldReturnRetirementQuoteWithoutCase()
    {
        _sut.DocumentType.Should().Be(DocumentType.RetirementQuoteWithoutCase);
    }

    public void Create_ShouldReturnValidDocument_WhenParametersAreValid()
    {
        string businessGroup = "WPS";
        string referenceNumber = "0003994";
        string caseNumber = "0003994";
        int id = 123456;
        int imageId = 654321;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var document = _sut.Create(businessGroup, referenceNumber, id, imageId, now, caseNumber);
        document.Should().NotBeNull();
        document.Type.Should().Be("Retirement quote");
        document.FileName.Should().Be($"{businessGroup}-{referenceNumber}-{imageId}_mdp.pdf");
        document.BusinessGroup.Should().Be("WPS");
        document.ReferenceNumber.Should().Be("0003994");
        document.Date.Should().Be(now);
        document.LastReadDate.Should().BeNull();
        document.Name.Should().Be("Retirement quote");
        document.ImageId.Should().Be(654321);
        document.TypeId.Should().Be("RETQU");
        document.Id.Should().Be(123456);
    }
}