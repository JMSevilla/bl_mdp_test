using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Beneficiaries;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Domain.Members.Beneficiaries;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.EmailConfirmation;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.PdfGenerator;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Beneficiaries;

public class BeneficiaryControllerTest
{
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly Mock<IEmailConfirmationSmtpClient> _smtpClientMock;
    private readonly Mock<IMemberDbUnitOfWork> _memberDbUnitOfWorkMock;
    private readonly Mock<IDbContextTransaction> _dbContextTransactionMock;
    private readonly Mock<IContentClient> _contentClientMock;
    private readonly Mock<ILogger<BeneficiaryController>> _loggerMock;
    private readonly Mock<IPdfGenerator> _pdfGeneratorMock;
    private readonly BeneficiaryController _sut;
    public BeneficiaryControllerTest()
    {
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _smtpClientMock = new Mock<IEmailConfirmationSmtpClient>();
        _memberDbUnitOfWorkMock = new Mock<IMemberDbUnitOfWork>();
        _dbContextTransactionMock = new Mock<IDbContextTransaction>();
        _contentClientMock = new Mock<IContentClient>();
        _loggerMock = new Mock<ILogger<BeneficiaryController>>();
        _pdfGeneratorMock = new Mock<IPdfGenerator>();
        _sut = new BeneficiaryController(
            _memberRepositoryMock.Object,
            _tenantRepositoryMock.Object,
            _smtpClientMock.Object,
            _memberDbUnitOfWorkMock.Object,
            _contentClientMock.Object,
            _loggerMock.Object,
            _pdfGeneratorMock.Object);

        SetupControllerContext();
    }

    [Input(false, typeof(NotFoundObjectResult))]
    [Input(true, typeof(OkObjectResult))]
    public async Task BeneficiariesReturnsCorrectHttpStatusCode(bool beneficiariesExists, Type expectedType)
    {
        var address = BeneficiaryAddress.Create("Towers Watson Westgate",
               "122-130 Station Road",
               "test dada for addres3",
               "Redhill",
               "Surrey",
               "United Kingdom",
               "GB",
               "RH1 1WS").Right();
        var details = BeneficiaryDetails.CreateCharity("Unicef", 12345678, 25, "note1").Right();
        var date = DateTimeOffset.UtcNow;

        Member member = null;
        if (beneficiariesExists)
            member = new MemberBuilder().AddBeneficiary(new Beneficiary(1, address, details, date)).Build();

        _memberRepositoryMock
            .Setup(x => x.FindMemberWithBeneficiaries(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(member);

        var result = await _sut.Beneficiaries();

        result.Should().BeOfType(expectedType);
        if (beneficiariesExists)
        {
            var response = (result as OkObjectResult).Value as BeneficiariesResponse;
            response.Beneficiaries.Single().Id.Should().Be(1);
            response.Beneficiaries.Single().Relationship.Should().Be("Charity");
            response.Beneficiaries.Single().Forenames.Should().BeNull();
            response.Beneficiaries.Single().Surname.Should().BeNull();
            response.Beneficiaries.Single().DateOfBirth.Should().BeNull();
            response.Beneficiaries.Single().CharityName.Should().Be("Unicef");
            response.Beneficiaries.Single().CharityNumber.Should().Be(12345678L);
            response.Beneficiaries.Single().LumpSumPercentage.Should().Be(25M);
            response.Beneficiaries.Single().IsPensionBeneficiary.Should().BeFalse();
            response.Beneficiaries.Single().Notes.Should().Be("note1");
            response.Beneficiaries.Single().Address.Line1.Should().Be("Towers Watson Westgate");
            response.Beneficiaries.Single().Address.Line2.Should().Be("122-130 Station Road");
            response.Beneficiaries.Single().Address.Line3.Should().Be("test dada for addres3");
            response.Beneficiaries.Single().Address.Line4.Should().Be("Redhill");
            response.Beneficiaries.Single().Address.Line5.Should().Be("Surrey");
            response.Beneficiaries.Single().Address.Country.Should().Be("United Kingdom");
            response.Beneficiaries.Single().Address.CountryCode.Should().Be("GB");
            response.Beneficiaries.Single().Address.PostCode.Should().Be("RH1 1WS");
        }
    }

    [Input(false, "non-existing", 25, null, "beneficiaries_save_email", typeof(NotFoundObjectResult))]
    [Input(true, "non-existing", 25, null, "beneficiaries_save_email", typeof(BadRequestObjectResult))]
    [Input(true, "Wife", 125, null, "beneficiaries_save_email", typeof(BadRequestObjectResult))]
    [Input(true, "Wife", 25, 3, "beneficiaries_save_email", typeof(BadRequestObjectResult))]
    [Input(true, "Wife", 100, null, "beneficiaries_save_email", typeof(OkObjectResult))]
    public async Task UpdateBeneficiariesReturnsCorrectHttpStatusCode(bool beneficiariesExists, string relation, int lumpSumPercentage, int? beneficiaryId, string templateKey, Type expectedType)
     {
        Member member = null;
        if (beneficiariesExists)
            member = new MemberBuilder().Build();

        _memberDbUnitOfWorkMock
            .Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(_dbContextTransactionMock.Object);

        _memberRepositoryMock
        .Setup(x => x.FindMemberWithBeneficiaries(It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(member);

        _tenantRepositoryMock
            .Setup(x => x.ListRelationships(It.IsAny<string>()))
            .ReturnsAsync(new List<SdDomainList> { new SdDomainList(It.IsAny<string>(), It.IsAny<string>(), "Wife") });

        _contentClientMock
           .Setup(x => x.FindTemplate(templateKey, It.IsAny<string>(), "scheme-category"))
           .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });

        var result = await _sut.UpdateBeneficiaries(new UpdateBeneficiariesRequest
        {
            Beneficiaries = new List<UpdateBeneficiaryRequest>
            {
                new UpdateBeneficiaryRequest
                {
                    Relationship = relation,
                    Id = beneficiaryId,
                    Forenames = "Jane",
                    Surname =  "Doe",
                    DateOfBirth = new DateTime(1988,1,1),
                    CharityName = null,
                    CharityNumber = null,
                    LumpSumPercentage = lumpSumPercentage,
                    IsPensionBeneficiary = true,
                    Notes = "test note",
                    Address = new UpdateBeneficiaryAddressRequest
                    {
                        Line1 = "Line1",
                        Line2 = "Line2",
                        Line3 = "Line3",
                        Line4 = "Line4",
                        Line5 = "Line5",
                        Country = "United Kingdom",
                        CountryCode = "GB",
                        PostCode = "RH1 1WS"
                    }
                }
            }
        });

        result.Should().BeOfType(expectedType);
    }


    [Input(true, "Wife", 100, null, "beneficiaries_save_email")]
    public async Task UpdateBeneficiariesCallsSysAuditMethods(bool beneficiariesExists, string relation, int lumpSumPercentage, int? beneficiaryId, string templateKey)
    {
        Member member = null;
        if (beneficiariesExists)
            member = new MemberBuilder().Build();

        _memberDbUnitOfWorkMock
            .Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(_dbContextTransactionMock.Object);

        _memberRepositoryMock
        .Setup(x => x.FindMemberWithBeneficiaries(It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(member);

        _tenantRepositoryMock
            .Setup(x => x.ListRelationships(It.IsAny<string>()))
            .ReturnsAsync(new List<SdDomainList> { new SdDomainList(It.IsAny<string>(), It.IsAny<string>(), "Wife") });

        _contentClientMock
           .Setup(x => x.FindTemplate(templateKey, It.IsAny<string>(), "scheme-category"))
           .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });

        var result = await _sut.UpdateBeneficiaries(new UpdateBeneficiariesRequest
        {
            Beneficiaries = new List<UpdateBeneficiaryRequest>
            {
                new UpdateBeneficiaryRequest
                {
                    Relationship = relation,
                    Id = beneficiaryId,
                    Forenames = "Jane",
                    Surname =  "Doe",
                    DateOfBirth = new DateTime(1988,1,1),
                    CharityName = null,
                    CharityNumber = null,
                    LumpSumPercentage = lumpSumPercentage,
                    IsPensionBeneficiary = true,
                    Notes = "test note",
                    Address = new UpdateBeneficiaryAddressRequest
                    {
                        Line1 = "Line1",
                        Line2 = "Line2",
                        Line3 = "Line3",
                        Line4 = "Line4",
                        Line5 = "Line5",
                        Country = "United Kingdom",
                        CountryCode = "GB",
                        PostCode = "RH1 1WS"
                    }
                }
            }
        });

        _memberRepositoryMock.Verify(x => x.PopulateSessionDetails(It.IsAny<string>()), Times.Once);
        _memberRepositoryMock.Verify(x => x.DisableSysAudit(It.IsAny<string>()), Times.Once);
    }

    public async Task UpdateBeneficiariesPopulateSessionDetailsThrowsException()
    {
        Member member = new MemberBuilder().Build();

        _memberDbUnitOfWorkMock
            .Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(_dbContextTransactionMock.Object);

        _memberRepositoryMock
            .Setup(x => x.PopulateSessionDetails(It.IsAny<string>()))
            .ThrowsAsync(new Exception());

        _memberRepositoryMock
            .Setup(x => x.FindMemberWithBeneficiaries(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(member);

        _tenantRepositoryMock
            .Setup(x => x.ListRelationships(It.IsAny<string>()))
            .ReturnsAsync(new List<SdDomainList> { new SdDomainList(It.IsAny<string>(), It.IsAny<string>(), "Wife") });

        _contentClientMock
           .Setup(x => x.FindTemplate("beneficiaries_save_email", It.IsAny<string>(), "scheme-category"))
           .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });

        Func<Task> func = async () => {
            await _sut.UpdateBeneficiaries(new UpdateBeneficiariesRequest
            {
                Beneficiaries = new List<UpdateBeneficiaryRequest>
                {
                    new UpdateBeneficiaryRequest
                    {
                        Relationship = "Wife",
                        Id = null,
                        Forenames = "Jane",
                        Surname =  "Doe",
                        DateOfBirth = new DateTime(1988,1,1),
                        CharityName = null,
                        CharityNumber = null,
                        LumpSumPercentage = 100,
                        IsPensionBeneficiary = true,
                        Notes = "test note",
                        Address = new UpdateBeneficiaryAddressRequest
                        {
                            Line1 = "Line1",
                            Line2 = "Line2",
                            Line3 = "Line3",
                            Line4 = "Line4",
                            Line5 = "Line5",
                            Country = "United Kingdom",
                            CountryCode = "GB",
                            PostCode = "RH1 1WS"
                        }
                    }
                }
            });
        };

        _memberRepositoryMock.Verify(x => x.PopulateSessionDetails(It.IsAny<string>()), Times.Never);
        _memberRepositoryMock.Verify(x => x.DisableSysAudit(It.IsAny<string>()), Times.Never);
        await func.Should().ThrowAsync<Exception>();
    }


    [Input(false, false, typeof(NotFoundObjectResult))]
    [Input(true, false, typeof(NotFoundObjectResult))]
    [Input(true, true, typeof(FileStreamResult))]
    public async Task DownloadBeneficiariesPdfReturnsCorrectHttpStatusCode(bool memberExista, bool beneficiariesExists, Type expectedType)
    {
        var address = BeneficiaryAddress.Create("Towers Watson Westgate",
               "122-130 Station Road",
               "test dada for addres3",
               "Redhill",
               "Surrey",
               "United Kingdom",
               "GB",
               "RH1 1WS").Right();
        var details = BeneficiaryDetails.CreateCharity("Unicef", 12345678, 25, "note1").Right();
        var date = DateTimeOffset.UtcNow;

        Member member = null;
        if (memberExista)
        {
            var builder = new MemberBuilder();
            if (beneficiariesExists)
                builder.AddBeneficiary(new Beneficiary(1, address, details, date));
            member = builder.Build();
        }

        _memberRepositoryMock
            .Setup(x => x.FindMemberWithBeneficiaries(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(member);

        _contentClientMock
          .Setup(x => x.FindTemplate("beneficiaries_pdf", It.IsAny<string>(), It.IsAny<string>()))
          .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>", TemplateName = "test name" });

        using var mem = new MemoryStream();
        _pdfGeneratorMock.Setup(x => x.Generate(It.IsAny<string>(), It.IsAny<Option<string>>(), It.IsAny<Option<string>>())).ReturnsAsync(mem);

        var result = await _sut.DownloadBeneficiariesPdf(It.IsAny<string>());

        result.Should().BeOfType(expectedType);
    }

    private void SetupControllerContext(string referenceNumber = "reference_number", string value = "TestReferenceNumber")
    {
        var id = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("business_group", "TestBusinessGroup"),
            new Claim(referenceNumber, value),
        };

        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }
}