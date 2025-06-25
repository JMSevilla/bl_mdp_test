using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WTW.MdpService.Addresses;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Geolocation;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.Web.Errors;

namespace WTW.MdpService.Test.Addresses;

public class AddressesControllerTest
{
    private readonly Mock<ILoqateApiClient> _loqateApiClientMock;
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly AddressesController _sut;
    public AddressesControllerTest()
    {
        _loqateApiClientMock = new Mock<ILoqateApiClient>();
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _sut = new AddressesController(_loqateApiClientMock.Object, _tenantRepositoryMock.Object);

        SetupControllerContext();
    }

    public async Task FindAddressSummariesReturns200StatusCodeAndCorrectData()
    {
        _loqateApiClientMock
            .Setup(x => x.Find("test-text", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new LocationApiAddressSummaryResponse
            {
                Items = new List<LocationApiAddressSummary>
                {
                    new LocationApiAddressSummary
                    {
                        Highlight = "test-Highlight",
                        Id = "test-Id",
                        Text = "test-Text",
                        Type = "test-Type",
                        Error = "test-error",
                        Cause= "test-Cause",
                        Description= "test-Description"
                    }
                }
            });

        var result = await _sut.FindAddressSummaries(new AddressSummaryRequest
        {
            Text = "test-text",
            Container = It.IsAny<string>(),
            Language = It.IsAny<string>(),
            Countries = It.IsAny<string>()
        });
        var response = ((result as OkObjectResult).Value as IEnumerable<AddressSummariesResponse>).ToList();

        result.Should().BeOfType<OkObjectResult>();
        response[0].Highlight.Should().Be("test-Highlight");
        response[0].AddressId.Should().Be("test-Id");
        response[0].Text.Should().Be("test-Text");
        response[0].Type.Should().Be("test-Type");
        response[0].Description.Should().Be("test-Description");
    }

    public async Task FindAddressSummariesReturns400StatusCodeAndCorrectData()
    {
        _loqateApiClientMock
            .Setup(x => x.Find("test-text", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Error.New("test-error"));

        var result = await _sut.FindAddressSummaries(new AddressSummaryRequest
        {
            Text = "test-text",
            Container = It.IsAny<string>(),
            Language = It.IsAny<string>(),
            Countries = It.IsAny<string>()
        });
        var response = (result as BadRequestObjectResult).Value as ApiError;

        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("test-error");
    }

    public async Task GetAddressByIdReturns200StatusCodeAndCorrectData()
    {
        _loqateApiClientMock
            .Setup(x => x.GetDetails(It.IsAny<string>()))
            .ReturnsAsync(new LocationApiAddressDetailsResponse
            {
                Items = new List<LocationApiAddressDetails>
                {
                    new LocationApiAddressDetails
                    {
                        Id = "test-Id",
                        City = "test-City",
                        Line1 = "test-Line1",
                        Line2 = "test-Line2",
                        Line3 = "test-Line3",
                        Line4 = "test-Line4",
                        Line5 = "test-Line5",
                        PostalCode = "test-postcode",
                        CountryIso2 = "test-CountryIso2",
                        Type = "test-Type",

                        Error = "test-error",
                        Cause= "test-Cause",
                        Description= "test-Description"
                    }
                }
            });

        var result = await _sut.Get(It.IsAny<string>());
        var response = ((result as OkObjectResult).Value as IEnumerable<AddressResponse>).ToList();

        result.Should().BeOfType<OkObjectResult>();
        response[0].AddressId.Should().Be("test-Id");
        response[0].City.Should().Be("test-City");
        response[0].Line1.Should().Be("test-Line1");
        response[0].Line2.Should().Be("test-Line2");
        response[0].Line3.Should().Be("test-Line3");
        response[0].Line4.Should().Be("test-Line4");
        response[0].Line5.Should().Be("test-Line5");
        response[0].PostalCode.Should().Be("test-postcode");
        response[0].CountryIso2.Should().Be("test-CountryIso2");
        response[0].Type.Should().Be("test-Type");
    }

    public async Task GetAddressByIdReturns200StatusCodeAndCorrectData2()
    {
        _loqateApiClientMock
            .Setup(x => x.GetDetails(It.IsAny<string>()))
            .ReturnsAsync(Error.New("test-error2"));

        var result = await _sut.Get(It.IsAny<string>());
        var response = (result as BadRequestObjectResult).Value as ApiError;

        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("test-error2");
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

    public async Task WhenGetAddressesCountriesIsCalled_ThenReturnCountriesData()
    {
        string country = "AFGHANISTAN";
        string countryCode = "AF";
        _tenantRepositoryMock
            .Setup(x => x.GetAddressCountries(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<SdDomainList>
            {
                new SdDomainList(It.IsAny<string>(),It.IsAny<string>(),countryCode,country)
            });

        var result = await _sut.GetCountries();
        var response = ((result as OkObjectResult).Value as IEnumerable<GetCountriesResponse>).ToList();

        result.Should().BeOfType<OkObjectResult>();
        response[0].Code.Should().Be(countryCode);
        response[0].Name.Should().Be(country);
    }
}