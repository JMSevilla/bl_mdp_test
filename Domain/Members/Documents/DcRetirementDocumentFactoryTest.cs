using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members.Documents;

public class DcRetirementDocumentFactoryTest
{
    private readonly DcRetirementDocumentFactory _sut;

    public DcRetirementDocumentFactoryTest()
    {
        _sut = new DcRetirementDocumentFactory();
    }

    public void DocumentType_ShouldReturnDcRetirement()
    {
        _sut.DocumentType.Should().Be(DocumentType.DcRetirement);
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
        document.Type.Should().Be("DC Retirement Application");
        document.FileName.Should().Be($"{businessGroup}-{referenceNumber}-{caseNumber}-{imageId}_mdp.pdf");
        document.BusinessGroup.Should().Be("RBS");
        document.ReferenceNumber.Should().Be("0003994");
        document.Date.Should().Be(now);
        document.LastReadDate.Should().BeNull();
        document.Name.Should().Be("DC Retirement Application");
        document.ImageId.Should().Be(654321);
        document.TypeId.Should().Be("MDPRETAPP");
        document.Id.Should().Be(123456);
    }
}