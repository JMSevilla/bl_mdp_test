using System;
using System.Collections.Generic;
using Castle.Core.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members.Documents;

public class DocumentFactoryProviderTest
{
    private readonly Mock<IDocumentFactory> _retirementFactory;
    private readonly Mock<IDocumentFactory> _transferFactory;
    private readonly Mock<IDocumentFactory> _transferV2Factory;
    private readonly Mock<ILogger<DocumentFactoryProvider>> _logger;
    private readonly DocumentFactoryProvider _sut;
    private readonly Mock<IDocumentFactory> _transferV2OutsideAssureQuoteLockFactory;
    private readonly Mock<IDocumentFactory> _retirementQuoteRequestFactory;
    private Mock<IDocumentFactory> _transferQuoteRequestFactory;

    public DocumentFactoryProviderTest()
    {
        _retirementFactory = new Mock<IDocumentFactory>();
        _retirementFactory.Setup(f => f.DocumentType).Returns(DocumentType.Retirement);

        _transferFactory = new Mock<IDocumentFactory>();
        _transferFactory.Setup(f => f.DocumentType).Returns(DocumentType.Transfer);

        _transferV2Factory = new Mock<IDocumentFactory>();
        _transferV2Factory.Setup(f => f.DocumentType).Returns(DocumentType.TransferV2);

        _transferV2OutsideAssureQuoteLockFactory = new Mock<IDocumentFactory>();
        _transferV2OutsideAssureQuoteLockFactory.Setup(f => f.DocumentType).Returns(DocumentType.TransferV2OutsideAssureQuoteLock);

        _retirementQuoteRequestFactory = new Mock<IDocumentFactory>();
        _retirementQuoteRequestFactory.Setup(f => f.DocumentType).Returns(DocumentType.RetirementQuoteRequest);

        _transferQuoteRequestFactory = new Mock<IDocumentFactory>();
        _transferQuoteRequestFactory.Setup(f => f.DocumentType).Returns(DocumentType.TransferQuoteRequest);

        _logger = new Mock<ILogger<DocumentFactoryProvider>>();

        _sut = new DocumentFactoryProvider(new[]
        {
            _retirementFactory.Object,
            _transferFactory.Object,
            _transferV2Factory.Object,
            _transferV2OutsideAssureQuoteLockFactory.Object,
            _retirementQuoteRequestFactory.Object,
            _transferQuoteRequestFactory.Object,
        },
        _logger.Object);
    }

    public void GetFactory_ShouldReturnCorrectFactory_GivenDocumentType()
    {
        _sut.GetFactory(DocumentType.Retirement).Should().Be(_retirementFactory.Object);
        _sut.GetFactory(DocumentType.Transfer).Should().Be(_transferFactory.Object);
        _sut.GetFactory(DocumentType.TransferV2).Should().Be(_transferV2Factory.Object);
        _sut.GetFactory(DocumentType.TransferV2OutsideAssureQuoteLock).Should().Be(_transferV2OutsideAssureQuoteLockFactory.Object);
        _sut.GetFactory(DocumentType.RetirementQuoteRequest).Should().Be(_retirementQuoteRequestFactory.Object);
        _sut.GetFactory(DocumentType.TransferQuoteRequest).Should().Be(_transferQuoteRequestFactory.Object);
    }

    public void GetFactory_ShouldThrowException_WhenTypeIsInvalid()
    {
        DocumentType type = (DocumentType)999;

        Action action = () => _sut.GetFactory(type);
        action.Should().Throw<ArgumentException>().WithMessage("No document factory defined for type: 999");
    }
}