using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members.Documents;

public class TransferV2OutsideAssureQuoteLockDocumentFactoryTest
{
    private readonly TransferV2OutsideAssureQuoteLockDocumentFactory _sut;

    public TransferV2OutsideAssureQuoteLockDocumentFactoryTest()
    {
        _sut = new TransferV2OutsideAssureQuoteLockDocumentFactory();
    }

    public void DocumentType_ShouldReturnTransferV2OutsideAssureQuoteLock()
    {
        _sut.DocumentType.Should().Be(DocumentType.TransferV2OutsideAssureQuoteLock);
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
        document.Type.Should().Be("Transfer Quote");
        document.FileName.Should().Be($"{businessGroup}-{referenceNumber}-{imageId}_mdp.pdf");
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