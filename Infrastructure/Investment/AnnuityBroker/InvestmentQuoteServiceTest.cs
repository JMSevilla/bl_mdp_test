using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Moq;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Infrastructure.Investment.AnnuityBroker;
using WTW.MdpService.Infrastructure.MemberService;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.Investment.AnnuityBroker;

public class InvestmentQuoteServiceTest
{
    private readonly Mock<IMemberServiceClient> _memberServiceClientMock;
    private readonly InvestmentQuoteService _sut;

    public InvestmentQuoteServiceTest()
    {
        _memberServiceClientMock = new Mock<IMemberServiceClient>();
        _sut = new InvestmentQuoteService(_memberServiceClientMock.Object);
    }

    public async Task CreateAnnuityQuoteRequest_ReturnsRequest_WhenAllMemberDataPresent()
    {
        var businessGroup = "BG";
        var referenceNumber = "REF123";
        var retirementV2Params = new RetirementV2Params()
        {
            ResidualFundValue = 1000,
        };
        var retirementV2 = new RetirementV2(retirementV2Params);

        _memberServiceClientMock.Setup(x => x.GetMemberSummary(businessGroup, referenceNumber))
            .ReturnsAsync(Option<GetMemberSummaryClientResponse>.Some(new GetMemberSummaryClientResponse { SchemeTranslation = "Scheme", SchemeType = "Type" }));

        _memberServiceClientMock.Setup(x => x.GetPersonalDetail(businessGroup, referenceNumber))
            .ReturnsAsync(Option<GetMemberPersonalDetailClientResponse>.Some(new GetMemberPersonalDetailClientResponse { NiNumber = "NI", Surname = "Smith" }));

        _memberServiceClientMock.Setup(x => x.GetPensionDetails(businessGroup, referenceNumber))
            .ReturnsAsync(Option<GetPensionDetailsClientResponse>.Some(new GetPensionDetailsClientResponse { PayrollNo = "PR123" }));

        _memberServiceClientMock.Setup(x => x.GetContactDetails(businessGroup, referenceNumber))
            .ReturnsAsync(Option<MemberContactDetailsClientResponse>.Some(new MemberContactDetailsClientResponse
            {
                Address = new MemberContactDetailsAddressResponse { Line1 = "L1", Country = "UK", PostCode = "PC" },
                Telephone = "123456789",
                Email = "test@example.com",
                NonStandardCommsType = "None"
            }));

        var result = await _sut.CreateAnnuityQuoteRequest(businessGroup, referenceNumber, retirementV2);

        result.IsRight.Should().BeTrue();
        var request = result.Right();
        request.Should().NotBeNull();
        request.Member.Should().NotBeNull();
        request.Quote.Should().NotBeNull();
        request.Member.NiNumber.Should().Be("NI");
        request.Member.Surname.Should().Be("Smith");
        request.Quote.QuoteType.Should().NotBeNull();
        request.Quote.ResidualFundValue.Should().Be(1000);
    }

    public async Task CreateAnnuityQuoteRequest_ReturnsError_WhenMemberSummaryMissing()
    {
        var businessGroup = "BG";
        var referenceNumber = "REF123";
        var retirementV2 = new RetirementV2(new RetirementV2Params());

        _memberServiceClientMock.Setup(x => x.GetMemberSummary(businessGroup, referenceNumber))
            .ReturnsAsync(Option<GetMemberSummaryClientResponse>.None);

        var result = await _sut.CreateAnnuityQuoteRequest(businessGroup, referenceNumber, retirementV2);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Contain("Member summary not found");
    }

    public async Task CreateAnnuityQuoteRequest_ReturnsError_WhenPersonalDetailsMissing()
    {
        var businessGroup = "BG";
        var referenceNumber = "REF123";
        var retirementV2 = new RetirementV2(new RetirementV2Params());

        _memberServiceClientMock.Setup(x => x.GetMemberSummary(businessGroup, referenceNumber))
            .ReturnsAsync(Option<GetMemberSummaryClientResponse>.Some(new GetMemberSummaryClientResponse()));
        _memberServiceClientMock.Setup(x => x.GetPersonalDetail(businessGroup, referenceNumber))
            .ReturnsAsync(Option<GetMemberPersonalDetailClientResponse>.None);

        var result = await _sut.CreateAnnuityQuoteRequest(businessGroup, referenceNumber, retirementV2);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Contain("Member personal details not found");
    }

    public async Task CreateAnnuityQuoteRequest_ReturnsError_WhenPensionDetailsMissing()
    {
        var businessGroup = "BG";
        var referenceNumber = "REF123";
        var retirementV2 = new RetirementV2(new RetirementV2Params());

        _memberServiceClientMock.Setup(x => x.GetMemberSummary(businessGroup, referenceNumber))
            .ReturnsAsync(Option<GetMemberSummaryClientResponse>.Some(new GetMemberSummaryClientResponse()));
        _memberServiceClientMock.Setup(x => x.GetPersonalDetail(businessGroup, referenceNumber))
            .ReturnsAsync(Option<GetMemberPersonalDetailClientResponse>.Some(new GetMemberPersonalDetailClientResponse()));
        _memberServiceClientMock.Setup(x => x.GetPensionDetails(businessGroup, referenceNumber))
            .ReturnsAsync(Option<GetPensionDetailsClientResponse>.None);

        var result = await _sut.CreateAnnuityQuoteRequest(businessGroup, referenceNumber, retirementV2);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Contain("Member contact details not found for BG with reference number REF123");
    }

    public async Task CreateAnnuityQuoteRequest_ReturnsError_WhenContactDetailsMissing()
    {
        var businessGroup = "BG";
        var referenceNumber = "REF123";
        var retirementV2 = new RetirementV2(new RetirementV2Params());

        _memberServiceClientMock.Setup(x => x.GetMemberSummary(businessGroup, referenceNumber))
            .ReturnsAsync(Option<GetMemberSummaryClientResponse>.Some(new GetMemberSummaryClientResponse()));
        _memberServiceClientMock.Setup(x => x.GetPersonalDetail(businessGroup, referenceNumber))
            .ReturnsAsync(Option<GetMemberPersonalDetailClientResponse>.Some(new GetMemberPersonalDetailClientResponse()));
        _memberServiceClientMock.Setup(x => x.GetPensionDetails(businessGroup, referenceNumber))
            .ReturnsAsync(Option<GetPensionDetailsClientResponse>.Some(new GetPensionDetailsClientResponse()));
        _memberServiceClientMock.Setup(x => x.GetContactDetails(businessGroup, referenceNumber))
            .ReturnsAsync(Option<MemberContactDetailsClientResponse>.None);

        var result = await _sut.CreateAnnuityQuoteRequest(businessGroup, referenceNumber, retirementV2);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Contain("Member contact details not found");
    }
}
