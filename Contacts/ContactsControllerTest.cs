using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Contacts;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.FixieConfig;

namespace WTW.MdpService.Test.Contacts;

public class ContactsControllerTest
{
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<ISystemRepository> _systemRepositoryMock;
    private readonly Mock<IObjectStatusRepository> _objectStatusRepositoryMock;
    private readonly Mock<IMemberDbUnitOfWork> _memberDbUnitOfWorkMock;
    private readonly Mock<IDbContextTransaction> _dbContextTransactionMock;
    private readonly Mock<ILogger<ContactsController>> _logger;
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly ContactsController _sut;

    public ContactsControllerTest()
    {
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _systemRepositoryMock = new Mock<ISystemRepository>();
        _objectStatusRepositoryMock = new Mock<IObjectStatusRepository>();
        _memberDbUnitOfWorkMock = new Mock<IMemberDbUnitOfWork>();
        _dbContextTransactionMock = new Mock<IDbContextTransaction>();
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _logger = new Mock<ILogger<ContactsController>>();
        _sut = new ContactsController(
            _memberRepositoryMock.Object,
            _systemRepositoryMock.Object,
            _objectStatusRepositoryMock.Object,
            _memberDbUnitOfWorkMock.Object,
            _logger.Object,
            _tenantRepositoryMock.Object);

        SetupControllerContext();
    }

    public async Task GetCurrentEmailReturns204StatusCode_WhenNoMemberExists()
    {
        var result = await _sut.GetCurrentEmail();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task GetCurrentEmailReturns204StatusCode_WhenNoEmailExists()
    {
        _memberRepositoryMock
           .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
           .ReturnsAsync(new MemberBuilder().EmailView(null).Build());

        var result = await _sut.GetCurrentEmail();

        result.Should().BeOfType<NoContentResult>();
    }

    public async Task GetCurrentEmailReturns200StatusCode_WhenEmailExists()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Build());

        var result = await _sut.GetCurrentEmail();

        result.Should().BeOfType<OkObjectResult>();
        var response = (result as OkObjectResult).Value as EmailResponse;
        response.Email.Should().Be("test@gmail.com");
    }

    public async Task CurrentMobilePhoneReturns204StatusCode_WhenNoMemberExists()
    {
        var result = await _sut.CurrentMobilePhone();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task CurrentMobilePhoneReturns204StatusCode_WhenNoPhoneNumberExists()
    {
        _memberRepositoryMock
           .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
           .ReturnsAsync(new MemberBuilder().Build());

        var result = await _sut.CurrentMobilePhone();

        result.Should().BeOfType<NoContentResult>();
    }

    public async Task CurrentMobilePhoneReturns200StatusCode_WhenPhoneNumberExists()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().AddContactReference().Build());

        var result = await _sut.CurrentMobilePhone();

        result.Should().BeOfType<OkObjectResult>();
        var response = (result as OkObjectResult).Value as MobilePhoneResponse;
        response.Code.Should().Be("44");
        response.Number.Should().Be("7544111111");
    }

    public async Task RetrieveNotificationsSettingsReturns404StatusCode_WhenNoMemberExists()
    {
        var result = await _sut.RetrieveNotificationsSettings();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task RetrieveNotificationsSettingsReturns200StatusCode_WhenMemberExists()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Build());

        var result = await _sut.RetrieveNotificationsSettings();

        result.Should().BeOfType<OkObjectResult>();
        var response = (result as OkObjectResult).Value as NotificationSettingsResponse;
        response.Email.Should().BeFalse();
        response.Sms.Should().BeFalse();
        response.Post.Should().BeTrue();
    }

    [Input(false, typeof(NotFoundObjectResult))]
    [Input(true, typeof(BadRequestObjectResult))]
    public async Task UpdateNotificationsSettingsReturnsCorrect4xxStatusCode(bool memberExists, Type expetedType)
    {
        if (memberExists)
            _memberRepositoryMock
                .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
                .ReturnsAsync(new MemberBuilder().Build());

        var result = await _sut.UpdateNotificationsSettings(new NotificationSettingsRequest(NotificationType.POST, false, false, false));

        result.Should().BeOfType(expetedType);
    }

    public async Task UpdateNotificationsSettingsReturns200StatusCode()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Build());

        var result = await _sut.UpdateNotificationsSettings(new NotificationSettingsRequest(NotificationType.POST, false, false, true));

        result.Should().BeOfType<OkObjectResult>();
    }


    [Input(false, typeof(NotFoundObjectResult))]
    [Input(true, typeof(BadRequestObjectResult))]
    public async Task SaveAddressReturnsCorrect4xxStatusCode(bool memberExists, Type expetedType)
    {
        if (memberExists)
            _memberRepositoryMock
                .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
                .ReturnsAsync(new MemberBuilder().Build());

        var result = await _sut.SaveAddress(new AddressRequest { StreetAddress1 = "1 Test st", Country = "UK", CountryCode = "UK", PostCode = "abcd12345" });

        result.Should().BeOfType(expetedType);
    }

    [Input(true)]
    [Input(false)]
    public async Task SaveAddressReturns204StatusCode(bool isInputAddresDifferentThanCurrent)
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().AddContactReference().Build());
        _memberDbUnitOfWorkMock.Setup(x => x.BeginTransactionAsync()).ReturnsAsync(_dbContextTransactionMock.Object);

        var result = await _sut.SaveAddress(new AddressRequest
        {
            StreetAddress1 = isInputAddresDifferentThanCurrent ? "Towers Watson Westgate different" : "Towers Watson Westgate",
            StreetAddress2 = "122-130 Station Road",
            StreetAddress3 = "test dada for addres3",
            StreetAddress4 = "Redhill",
            StreetAddress5 = "Surrey",
            Country = "United Kingdom",
            CountryCode = "GB",
            PostCode = "RH1 1WS"
        });

        result.Should().BeOfType<NoContentResult>();
    }


    [Input(false, typeof(NotFoundObjectResult))]
    [Input(true, typeof(NotFoundObjectResult))]
    public async Task RetrieveAddressReturns404StatusCode(bool memberExists, Type expetedType)
    {
        if (memberExists)
            _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Build());

        var result = await _sut.RetrieveAddress();

        result.Should().BeOfType(expetedType);
    }

    public async Task RetrieveAddressReturns200StatusCode()
    {
        _memberRepositoryMock
        .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
        .ReturnsAsync(new MemberBuilder().AddContactReference().Build());

        var result = await _sut.RetrieveAddress();

        result.Should().BeOfType<OkObjectResult>();
        var response = (result as OkObjectResult).Value as MemberAddressResponse;
        response.StreetAddress1.Should().Be("Towers Watson Westgate");
        response.StreetAddress2.Should().Be("122-130 Station Road");
        response.StreetAddress3.Should().Be("test dada for addres3");
        response.StreetAddress4.Should().Be("Redhill");
        response.StreetAddress5.Should().Be("Surrey");
        response.Country.Should().Be("United Kingdom");
        response.CountryCode.Should().Be("GB");
        response.PostCode.Should().Be("RH1 1WS");
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
    public async Task WhenGetContactCountriesIsCalled_ThenReturnCountriesData()
    {
        string country = "AFGHANISTAN";
        string countryCode = "AF";
        string dialCode = "91";
        _tenantRepositoryMock
            .Setup(x => x.GetAddressCountries(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<SdDomainList>
            {
                new SdDomainList(It.IsAny<string>(),It.IsAny<string>(),dialCode,country,countryCode)
            });

        var result = await _sut.GetContactCountries();
        var response = ((result as OkObjectResult).Value as IEnumerable<GetContactCountriesResponse>).ToList();

        result.Should().BeOfType<OkObjectResult>();
        response[0].Code.Should().Be(countryCode);
        response[0].Name.Should().Be(country);
        response[0].DialCode.Should().Be($"+{dialCode}");
    }
}