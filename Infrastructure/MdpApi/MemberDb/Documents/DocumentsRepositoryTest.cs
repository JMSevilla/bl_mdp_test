using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.MemberDb.Documents;

namespace WTW.MdpService.Test.Infrastructure.MemberDb.Documents;

public class DocumentsRepositoryTest
{
    private readonly Mock<DbSet<Document>> _documentsDbSetMock;
    private readonly DocumentsRepository _repository;
    private readonly DateTimeOffset _testDate = new(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly IQueryable<Document> _stubQueryReturnData;

    public DocumentsRepositoryTest()
    {
        _documentsDbSetMock = new Mock<DbSet<Document>>();

        // Update test data to match the test parameters
        _stubQueryReturnData = new List<Document>
        {
            CreateDocument("BG1", "1234567", "Type1", id: 1),
            CreateDocument("BG2", "2345678", "Type2", id: 2)
        }.AsQueryable();

        var asyncProvider = new AsyncHelper.TestAsyncQueryProvider<Document>(_stubQueryReturnData.Provider);

        _documentsDbSetMock.As<IQueryable<Document>>()
            .Setup(m => m.Provider)
            .Returns(asyncProvider);

        _documentsDbSetMock.As<IQueryable<Document>>()
            .Setup(m => m.Expression)
            .Returns(_stubQueryReturnData.Expression);

        _documentsDbSetMock.As<IQueryable<Document>>()
            .Setup(m => m.ElementType)
            .Returns(_stubQueryReturnData.ElementType);

        _documentsDbSetMock.As<IQueryable<Document>>()
            .Setup(m => m.GetEnumerator())
            .Returns(_stubQueryReturnData.GetEnumerator());

        _documentsDbSetMock.As<IAsyncEnumerable<Document>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new AsyncHelper.TestAsyncEnumerator<Document>(_stubQueryReturnData.GetEnumerator()));

        // Setup context
        var options = new DbContextOptionsBuilder<MemberDbContext>()
            .Options;
            
        var context = new MemberDbContext(options);

        _documentsDbSetMock.Setup(x => x.AsQueryable())
            .Returns(_documentsDbSetMock.Object);
        var documentsProperty = typeof(MemberDbContext).GetProperty(nameof(MemberDbContext.Documents));
        documentsProperty?.SetValue(context, _documentsDbSetMock.Object);
        
        _repository = new DocumentsRepository(context);
    }

    public async Task FindByDocumentId_ReturnsMatchingDocument()
    {
        var result = await _repository.FindByDocumentId("1234567", "BG1", 1);

        result.IsSome.Should().BeTrue();
        var document = result.SingleOrDefault();
        document.Should().NotBeNull();
        document.ReferenceNumber.Should().Be("1234567");
        document.BusinessGroup.Should().Be("BG1");
        document.Id.Should().Be(1);
    }

    public async Task FindByDocumentId_ReturnsNone_WhenDocumentNotFound()
    {
        var result = await _repository.FindByDocumentId("NON_EXISTENT", "BG1", 999);

        result.IsNone.Should().BeTrue();
    }

    private Document CreateDocument(string businessGroup, string referenceNumber, string type, int id = 1, int imageId = 1)
    {
        return new Document(
            businessGroup: businessGroup,
            referenceNumber: referenceNumber,
            type: type,
            date: _testDate,
            name: "Test Document",
            fileName: "test.pdf",
            id: id,
            imageId: imageId,
            typeId: "TYPE1",
            schema: "Schema1"
        );
    }
}