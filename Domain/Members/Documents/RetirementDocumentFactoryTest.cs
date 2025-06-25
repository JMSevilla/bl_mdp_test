using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members.Documents;

public class RetirementDocumentFactoryTest
{
    private readonly RetirementDocumentFactory _sut;

    public RetirementDocumentFactoryTest()
    {
        _sut = new RetirementDocumentFactory();
    }
    
    public void DocumentType_ShouldReturnRetirement()
    {
        _sut.DocumentType.Should().Be(DocumentType.Retirement);
    }

    public void Create_ShouldThrowException_WhenCaseNumberIsNull()
    {
        Action action = () => _sut.Create("businessGroup", "referenceNumber", 1, 1, DateTimeOffset.UtcNow, null);
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
        document.Type.Should().Be("MDP Retirement Application");
        document.FileName.Should().Be($"{businessGroup}-{referenceNumber}-{caseNumber}-{654321}_mdp.pdf");
        document.BusinessGroup.Should().Be("RBS");
        document.ReferenceNumber.Should().Be("0003994");
        document.Date.Should().Be(now);
        document.LastReadDate.Should().BeNull();
        document.Name.Should().Be("MDP Retirement Application");
        document.ImageId.Should().Be(654321);
        document.TypeId.Should().Be("MDPRETAPP");
        document.Id.Should().Be(123456);
        document.Schema.Should().Be("M");
    }
}