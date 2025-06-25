using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members.Documents;

public class TransferQuoteRequestFactoryTest
{
    private readonly TransferQuoteRequestFactory _sut;

    public TransferQuoteRequestFactoryTest()
    {
        _sut = new TransferQuoteRequestFactory();
    }

    public void DocumentType_ShouldReturnTransferQuoteRequest()
    {
        _sut.DocumentType.Should().Be(DocumentType.TransferQuoteRequest);
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
        document.Type.Should().Be("Transfer Quote-Journey Summary(Assure)");
        document.FileName.Should().Be($"{businessGroup}-{referenceNumber}-{caseNumber}-{imageId}_mdp.pdf");
        document.BusinessGroup.Should().Be("RBS");
        document.ReferenceNumber.Should().Be("0003994");
        document.Date.Should().Be(now);
        document.LastReadDate.Should().BeNull();
        document.Name.Should().Be("Assure Transfer Out Quote");
        document.ImageId.Should().Be(654321);
        document.TypeId.Should().Be("TRNQU_SUM");
        document.Id.Should().Be(123456);
    }
}