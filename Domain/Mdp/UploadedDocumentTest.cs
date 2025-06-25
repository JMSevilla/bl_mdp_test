using System.Collections.Generic;
using FluentAssertions;
using WTW.MdpService.Domain.Common;

namespace WTW.MdpService.Test.Domain.Mdp;

public class UploadedDocumentTest
{
    public void CreatesUploadedDocument()
    {
        var sut = new UploadedDocumentsBuilder().Build();

        sut.ReferenceNumber.Should().Be("TestReferenceNumber");
        sut.BusinessGroup.Should().Be("TestBusinessGroup");
        sut.JourneyType.Should().Be("transfer2");
        sut.FileName.Should().Be("TestName.pdf");
        sut.Uuid.Should().NotBeNullOrWhiteSpace();
        sut.DocumentSource.Should().Be(DocumentSource.Outgoing);
        sut.IsEdoc.Should().BeFalse();
        sut.IsEpaOnly.Should().BeFalse();
        sut.Tags.Should().Be("TAG1");
    }

    public void UpdateDocument_Should_UpdateUuidAndTags()
    {
        var uuid = "newUuid";
        var tags = new List<string> { "tag1", "tag2", "tag3" };

        var sut = new UploadedDocumentsBuilder()
            .Uuid("oldUuid")
            .Tags("tag1;tag2")
            .Build();

        var result = sut.UpdateDocument(uuid, tags);
        result.Uuid.Should().Be(uuid);
        result.Tags.Should().Be("tag1;tag2;tag3");
    }

    public void UpdateTags_Should_UpdateTags()
    {
        var tags = new List<string> { "tag1", "tag2", "tag3" };

        var sut = new UploadedDocumentsBuilder()
            .Tags("oldTag1;oldTag2")
            .Build();

        var result = sut.UpdateTags(tags);
        result.Tags.Should().Be("tag1;tag2;tag3");
    }

    public void UpdateFileUuidAndName_Should_UpdateUuidAndFileName()
    {
        var uuid = "newUuid";
        var fileName = "newFileName";

        var sut = new UploadedDocumentsBuilder()
            .Uuid("oldUuid")
            .FileName("oldFileName")
            .Build();

        var result = sut.UpdateFileUuidAndName(uuid, fileName);
        result.Uuid.Should().Be(uuid);
        result.FileName.Should().Be(fileName);
    }

    public void SplitTags_Should_ReturnSplitTags()
    {
        var tags = "tag1;tag2;tag3";

        var sut = new UploadedDocumentsBuilder()
            .Tags(tags)
            .Build();

        var result = sut.SplitTags();
        result.Should().ContainInOrder("tag1", "tag2", "tag3");
    }
}