using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members.Documents;

public class TransferV2DocumentFactoryTest
{
    private readonly TransferV2DocumentFactory _sut;

    public TransferV2DocumentFactoryTest()
    {
        _sut = new TransferV2DocumentFactory();
    }
    
    public void DocumentType_ShouldReturnTransferV2()
    {
        _sut.DocumentType.Should().Be(DocumentType.TransferV2);
    }

    public void Create_ShouldThrowException_WhenCaseNumberIsNull()
    {
        Action action = () => _sut.Create( "businessGroup", "referenceNumber", 1, 1, DateTimeOffset.UtcNow, null);
        action.Should().Throw<ArgumentNullException>().WithMessage("Value cannot be null. (Parameter 'caseNumber')");
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
        document.Type.Should().Be("Transfer Out Application");
        document.FileName.Should().Be($"{businessGroup}-{referenceNumber}-{caseNumber}-{imageId}_mdp.pdf");
        document.BusinessGroup.Should().Be("RBS");
        document.ReferenceNumber.Should().Be("0003994");
        document.Date.Should().Be(now);
        document.LastReadDate.Should().BeNull();
        document.Name.Should().Be("Transfer Application");
        document.ImageId.Should().Be(654321);
        document.TypeId.Should().Be("TRNAPP");
        document.Id.Should().Be(123456);
    }
}