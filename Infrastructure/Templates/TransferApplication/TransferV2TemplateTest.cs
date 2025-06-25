using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Moq;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.Templates.TransferApplication;
using WTW.MdpService.Test.Domain.Mdp;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.Web.Serialization;
using WTW.Web;

namespace WTW.MdpService.Test.Infrastructure.Templates.TransferApplication;

public class TransferV2TemplateTest
{
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly TransferV2Template _sut;

    public TransferV2TemplateTest()
    {
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _sut = new TransferV2Template(_calculationsParserMock.Object);
    }

    [Input(false)]
    [Input(true)]
    public async Task AppliesArgs(bool calculationExists)
    {
        var now = DateTimeOffset.UtcNow;
        var journey = new TransferJourneyBuilder()
            .Date(now)
            .NextPageKey("current")
            .BuildWithSteps();

        var member = new MemberBuilder()
               .MinimumPensionAge(59)
               .DateOfBirth(now.AddYears(-58))
               .Status(MemberStatus.Active)
               .Build();

        var calculation = calculationExists ? new CalculationBuilder().RetirementJsonV2(null).BuildV2() : Option<Calculation>.None;

        var document = new UploadedDocumentsBuilder().Build();

        var transferQuote = new TransferQuote(JsonSerializer.Deserialize<TransferQuoteDto>(TestData.TransferQuoteJson, SerialiationBuilder.Options()));

        var result = await _sut.RenderHtml("<div>{{transfer_application_status}}<\\div>", journey, member, transferQuote, TransferApplicationStatus.StartedTA, now, new List<UploadedDocument> { document }, calculation);
        result.Should().Be("<div>StartedTA<\\div>");
    }

    public async Task FiltersIdentityDocuments()
    {
        var now = DateTimeOffset.UtcNow;
        var journey = new TransferJourneyBuilder()
            .Date(now)
            .NextPageKey("current")
            .BuildWithSteps();

        var member = new MemberBuilder()
               .MinimumPensionAge(59)
               .DateOfBirth(now.AddYears(-58))
               .Status(MemberStatus.Active)
               .Build();

        var calculation = Option<Calculation>.None;

        var identityDocument1 = new UploadedDocumentsBuilder()
            .Type(MdpConstants.IdentityDocumentType)
            .FileName("passport.pdf")
            .Build();

        var identityDocument2 = new UploadedDocumentsBuilder()
            .Type(MdpConstants.IdentityDocumentType)
            .FileName("driving_license.pdf")
            .Build();

        var regularDocument = new UploadedDocumentsBuilder()
            .Type("transfer2")
            .FileName("transfer_form.pdf")
            .Build();

        var documents = new List<UploadedDocument> { identityDocument1, regularDocument, identityDocument2 };

        var transferQuote = new TransferQuote(JsonSerializer.Deserialize<TransferQuoteDto>(TestData.TransferQuoteJson, SerialiationBuilder.Options()));

        var template = "{{for filename in identity_uploaded_documents}}{{filename}},{{end}}";
        var result = await _sut.RenderHtml(template, journey, member, transferQuote, TransferApplicationStatus.StartedTA, now, documents, calculation);
        
        result.Should().Be("passport.pdf,driving_license.pdf,");
    }

    public async Task ReturnsEmptyWhenNoIdentityDocuments()
    {
        var now = DateTimeOffset.UtcNow;
        var journey = new TransferJourneyBuilder()
            .Date(now)
            .NextPageKey("current")
            .BuildWithSteps();

        var member = new MemberBuilder()
               .MinimumPensionAge(59)
               .DateOfBirth(now.AddYears(-58))
               .Status(MemberStatus.Active)
               .Build();

        var calculation = Option<Calculation>.None;

        var regularDocument1 = new UploadedDocumentsBuilder()
            .Type("transfer2")
            .FileName("transfer_form.pdf")
            .Build();

        var regularDocument2 = new UploadedDocumentsBuilder()
            .Type("retirement")
            .FileName("retirement_form.pdf")
            .Build();

        var documents = new List<UploadedDocument> { regularDocument1, regularDocument2 };

        var transferQuote = new TransferQuote(JsonSerializer.Deserialize<TransferQuoteDto>(TestData.TransferQuoteJson, SerialiationBuilder.Options()));

        var template = "{{for filename in identity_uploaded_documents}}{{filename}},{{end}}";
        var result = await _sut.RenderHtml(template, journey, member, transferQuote, TransferApplicationStatus.StartedTA, now, documents, calculation);
        
        result.Should().Be("");
    }
}