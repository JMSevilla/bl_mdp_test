using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members.Documents;

public class TransferDocumentFactoryTest
{
    private readonly TransferDocumentFactory _sut;

    public TransferDocumentFactoryTest()
    {
        _sut = new TransferDocumentFactory();
    }
    
    public void DocumentType_ShouldReturnTransfer()
    {
        _sut.DocumentType.Should().Be(DocumentType.Transfer);
    }

    public void Create_ShouldReturnValidDocument_WhenParametersAreValid()
    {
        string businessGroup = "RBS";
        string referenceNumber = "0003994";
        int id = 123456;
        int imageId = 654321;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var document = _sut.Create(businessGroup, referenceNumber, id, imageId, now);
        document.Should().NotBeNull();
        document.Type.Should().Be("Transfer Quote");
        document.FileName.Should().Be($"{businessGroup}-{referenceNumber}_mdp.pdf");
        document.BusinessGroup.Should().Be("RBS");
        document.ReferenceNumber.Should().Be("0003994");
        document.Date.Should().Be(now);
        document.LastReadDate.Should().BeNull();
        document.Name.Should().Be("Transfer Quote");
        document.ImageId.Should().Be(654321);
        document.TypeId.Should().Be("TRNQU");
        document.Id.Should().Be(123456);
    }
}
