using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members.Documents;

public class DocumentTest
{
    public void CanCreateDocument()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new Document("RBS", "0003994", "pdf", now, "summary", "summary.pdf", 123456, 654321, "123456", "testScheme");

        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferenceNumber.Should().Be("0003994");
        sut.Type.Should().Be("pdf");
        sut.Date.Should().Be(now);
        sut.LastReadDate.Should().BeNull();
        sut.Name.Should().Be("summary");
        sut.FileName.Should().Be("summary.pdf");
        sut.ImageId.Should().Be(654321);
        sut.TypeId.Should().Be("123456");
        sut.Id.Should().Be(123456);
        sut.Schema.Should().Be("testScheme");
    }

    public void CanMarkDocumentAsRead()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new Document("RBS", "0003994", "pdf", now.AddDays(-34), "summary", "summary.pdf", 123456, 654321, "123456", "testScheme");

        sut.MarkAsRead(now);
      
        sut.LastReadDate.Should().Be(now);        
    }
}