using System;
using System.Threading.Tasks;
using FluentAssertions;
using WTW.MdpService.Journeys.Submit.Services;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Journeys.Submit.Services;

public class DocumentsRendererDataFactoryTest
{
    private readonly DocumentsRendererDataFactory _sut;

    public DocumentsRendererDataFactoryTest()
    {
        _sut = new DocumentsRendererDataFactory();
    }

    public void CreatesDcRetirementSubmitSummaryDocumentsRendererData()
    {
        var result = _sut.CreateForSubmit("dcretirementapplication", "RBS", "1111122", "{}", "1234567");

        result.BusinessGroup.Should().Be("RBS");
        result.ReferenceNumber.Should().Be("1111122");
        result.CaseNumber.Should().Be("1234567");
        result.PdfSummaryTemplateKey.Should().Be("dc_retirement_application_pdf");
        result.EmailTemplateKey.Should().Be("dc_retirement_submission_email");
        result.DataSummaryBlockKey.Should().Be("dc_retirement_application");
        result.JourneyType.Should().Be("dcretirementapplication");
        result.AccessKey.Should().Be("{}");
    }

    public void CreatesDcRetirementSubmitSummaryDocumentsRendererData_withoutCaseNumber()
    {
        var result = _sut.CreateForSubmit("dcretirementapplication", "RBS", "1111122", "{}", "123456");

        result.BusinessGroup.Should().Be("RBS");
        result.ReferenceNumber.Should().Be("1111122");
        result.CaseNumber.Should().Be("123456");
        result.PdfSummaryTemplateKey.Should().Be("dc_retirement_application_pdf");
        result.EmailTemplateKey.Should().Be("dc_retirement_submission_email");
        result.DataSummaryBlockKey.Should().Be("dc_retirement_application");
        result.JourneyType.Should().Be("dcretirementapplication");
        result.AccessKey.Should().Be("{}");
    }

    public void CreatesDbCoreRetirementSubmitSummaryDocumentsRendererData()
    {
        var result = _sut.CreateForSubmit("dbcoreretirementapplication", "RBS", "1111122", "{}", "1234567");

        result.BusinessGroup.Should().Be("RBS");
        result.ReferenceNumber.Should().Be("1111122");
        result.CaseNumber.Should().Be("1234567");
        result.PdfSummaryTemplateKey.Should().Be("dbcoreretirementapplication_pdf");
        result.EmailTemplateKey.Should().Be("dbcoreretirementapplication_submission_email");
        result.DataSummaryBlockKey.Should().Be("dbcoreretirementapplication_data_summary");
        result.JourneyType.Should().Be("dbcoreretirementapplication");
        result.AccessKey.Should().Be("{}");
    }

    public void CreatesDbRetirementSubmitSummaryDocumentsRendererData()
    {
        var result = _sut.CreateForSubmit("dbretirementapplication", "RBS", "1111122", "{}", "1234567");

        result.BusinessGroup.Should().Be("RBS");
        result.ReferenceNumber.Should().Be("1111122");
        result.CaseNumber.Should().Be("1234567");
        result.PdfSummaryTemplateKey.Should().Be("db_retirement_application_pdf");
        result.EmailTemplateKey.Should().Be(null);
        result.DataSummaryBlockKey.Should().Be("db_retirement_application");
        result.JourneyType.Should().Be("dbretirementapplication");
        result.AccessKey.Should().Be("{}");
    }

    [Input("random")]
    [Input("requestquote")]
    public void CreatesThrows_WhenUnsupportedJourneyTypeProvided(string journeyType)
    {
        var action = () => _sut.CreateForSubmit(journeyType, "RBS", "1111122", "{}", "1234567");

        action.Should().Throw<ArgumentException>().WithMessage($"Unsupported journey type: {journeyType}.");
    }

    public void CreatesDocumentsRendererTypeDocumentsRendererData_WhenCaseTypeRetirement()
    {
        var result = _sut.CreateForQuoteRequest("RBS", "1111122", "1234567", "{}", "Retirement");

        result.BusinessGroup.Should().Be("RBS");
        result.ReferenceNumber.Should().Be("1111122");
        result.CaseNumber.Should().Be("1234567");
        result.PdfSummaryTemplateKey.Should().Be("request_retirement_quote_pdf");
        result.EmailTemplateKey.Should().Be("quote_request_submission_email");
        result.DataSummaryBlockKey.Should().Be("retirement_quote_data_summary");
        result.JourneyType.Should().Be("requestquote");
        result.AccessKey.Should().Be("{}");
    }

    public void CreatesDocumentsRendererTypeDocumentsRendererData_WhenCaseTypeTransfer()
    {
        var result = _sut.CreateForQuoteRequest("RBS", "1111122", "1234567", "{}", "Transfer");

        result.BusinessGroup.Should().Be("RBS");
        result.ReferenceNumber.Should().Be("1111122");
        result.CaseNumber.Should().Be("1234567");
        result.PdfSummaryTemplateKey.Should().Be("request_transfer_quote_pdf");
        result.EmailTemplateKey.Should().Be("quote_request_submission_email");
        result.DataSummaryBlockKey.Should().Be("retirement_quote_data_summary");
        result.JourneyType.Should().Be("requestquote");
        result.AccessKey.Should().Be("{}");
    }

    public void CreateForQuoteRequestThrowsWhenQuoteRequestTypeIsInvalid()
    {
        var action = () => _sut.CreateForQuoteRequest("RBS", "1111122", "1234567", "{}", "random");

        action.Should().Throw<ArgumentException>().WithMessage("No template key exists for the Quote request case type: random.");
    }

    public void CreatesDocumentsRendererDataForDirectPdfDownload_WithPdfInTemplateKey()
    {
        var result = _sut.CreateForDirectPdfDownload("dcexploreoptions", "dc_pension_pot_summary_pdf", "RBS", "1111122","{}");

        result.BusinessGroup.Should().Be("RBS");
        result.ReferenceNumber.Should().Be("1111122");
        result.CaseNumber.Should().BeNull();
        result.PdfSummaryTemplateKey.Should().Be("dc_pension_pot_summary_pdf");
        result.EmailTemplateKey.Should().BeNull();
        result.DataSummaryBlockKey.Should().Be("dc_pension_pot_summary");
        result.JourneyType.Should().Be("dcexploreoptions");
        result.AccessKey.Should().Be("{}");
    }
    
    public void CreatesDocumentsRendererDataForDirectPdfDownload_WithoutPdfInTemplateKey()
    {
        var result = _sut.CreateForDirectPdfDownload("dcexploreoptions", "dc_pension_pot_summary", "RBS", "1111122", "{}");

        result.BusinessGroup.Should().Be("RBS");
        result.ReferenceNumber.Should().Be("1111122");
        result.CaseNumber.Should().BeNull();
        result.PdfSummaryTemplateKey.Should().Be("dc_pension_pot_summary");
        result.EmailTemplateKey.Should().BeNull();
        result.DataSummaryBlockKey.Should().Be("dc_pension_pot_summary");
        result.JourneyType.Should().Be("dcexploreoptions");
        result.AccessKey.Should().Be("{}");
    }
}