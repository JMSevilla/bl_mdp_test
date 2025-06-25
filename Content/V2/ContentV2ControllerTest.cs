using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Content;
using WTW.MdpService.Content.V2;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.Redis;
using WTW.MdpService.Test.Domain.Members;
using WTW.Web.Caching;

namespace WTW.MdpService.Test.Content.V2;

public class ContentV2ControllerTest
{
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<ICalculationsRedisCache> _calculationsRedisCacheMock;
    private readonly Mock<ICache> _cacheMock;
    private readonly Mock<IContentService> _contentServiceMock;
    private readonly Mock<ILogger<ContentV2Controller>> _logger;
    private readonly Mock<IAccessKeyService> _accessKeyServiceMock;
    private readonly ContentV2Controller _sut;

    public ContentV2ControllerTest()
    {
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _calculationsRedisCacheMock = new Mock<ICalculationsRedisCache>();
        _cacheMock = new Mock<ICache>();
        _contentServiceMock = new Mock<IContentService>();
        _logger = new Mock<ILogger<ContentV2Controller>>();
        _accessKeyServiceMock = new Mock<IAccessKeyService>();

        _sut = new ContentV2Controller(
            _memberRepositoryMock.Object,
            _calculationsRedisCacheMock.Object,
            _cacheMock.Object,
            _contentServiceMock.Object,
            _logger.Object,
            _accessKeyServiceMock.Object);

        _sut.SetupControllerContext();
    }

    public async Task AccessKeyReturns404_WhenTenantUrlIsInvalid()
    {
        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        _contentServiceMock.Setup(x => x.FindTenant(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new ContentResponse());
        _contentServiceMock.Setup(x => x.IsValidTenant(It.IsAny<ContentResponse>(), It.IsAny<string>())).ReturnsAsync(false);

        var result = await _sut.AccessKey(new ContentAccessKeyRequest { TenantUrl = It.IsAny<string>(), PreRetirementAgePeriodInYears = It.IsAny<int>(), NewlyRetiredRangeInMonth = It.IsAny<int>() });

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task AccessKeyReturns200_WhenMemberDoesNotExist()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.None);

        _contentServiceMock.Setup(x => x.FindTenant(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new ContentResponse());
        _contentServiceMock.Setup(x => x.IsValidTenant(It.IsAny<ContentResponse>(), It.IsAny<string>())).ReturnsAsync(true);

        var result = await _sut.AccessKey(new ContentAccessKeyRequest { TenantUrl = It.IsAny<string>(), PreRetirementAgePeriodInYears = It.IsAny<int>(), NewlyRetiredRangeInMonth = It.IsAny<int>() });

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task AccessKeyReturns200AndValidData()
    {
        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        _contentServiceMock.Setup(x => x.FindTenant(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new ContentResponse());
        _contentServiceMock.Setup(x => x.IsValidTenant(It.IsAny<ContentResponse>(), It.IsAny<string>())).ReturnsAsync(true);

        _accessKeyServiceMock
            .Setup(x => x.CalculateKey(It.IsAny<Member>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync("{\"tenantUrl\":\"natwestdev.assure.wtwco.com\",\"isCalculationSuccessful\":true");

        var result = await _sut.AccessKey(new ContentAccessKeyRequest { TenantUrl = It.IsAny<string>(), PreRetirementAgePeriodInYears = It.IsAny<int>(), NewlyRetiredRangeInMonth = It.IsAny<int>() });

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as ContentAccessKeyResponse;
        response.ContentAccessKey.Should().BeEquivalentTo("{\"tenantUrl\":\"natwestdev.assure.wtwco.com\",\"isCalculationSuccessful\":true");
        response.SchemeType.Should().BeEquivalentTo("DB");
        response.SchemeCodeAndCategory.Should().BeEquivalentTo("schemeCode-category");

        _cacheMock.Verify(x => x.Remove(It.IsAny<string>()), Times.Once);
        _calculationsRedisCacheMock.Verify(x => x.Clear(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    public async Task RecalculateAccessKeyReturns404_WhenTenantUrlIsInvalid()
    {
        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        _contentServiceMock.Setup(x => x.FindTenant(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new ContentResponse());
        _contentServiceMock.Setup(x => x.IsValidTenant(It.IsAny<ContentResponse>(), It.IsAny<string>())).ReturnsAsync(false);

        var result = await _sut.RecalculateAccessKey(new ContentAccessKeyRequest { TenantUrl = It.IsAny<string>(), PreRetirementAgePeriodInYears = It.IsAny<int>(), NewlyRetiredRangeInMonth = It.IsAny<int>() });

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task RecalculateAccessKeyReturns200_WhenMemberDoesNotExist()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.None);

        _contentServiceMock.Setup(x => x.FindTenant(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new ContentResponse());
        _contentServiceMock.Setup(x => x.IsValidTenant(It.IsAny<ContentResponse>(), It.IsAny<string>())).ReturnsAsync(true);

        var result = await _sut.RecalculateAccessKey(new ContentAccessKeyRequest { TenantUrl = It.IsAny<string>(), PreRetirementAgePeriodInYears = It.IsAny<int>(), NewlyRetiredRangeInMonth = It.IsAny<int>() });

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task RecalculateAccessKeyReturns200AndValidData()
    {
        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        _contentServiceMock.Setup(x => x.FindTenant(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new ContentResponse());
        _contentServiceMock.Setup(x => x.IsValidTenant(It.IsAny<ContentResponse>(), It.IsAny<string>())).ReturnsAsync(true);

        _accessKeyServiceMock
            .Setup(x => x.RecalculateKey(It.IsAny<Member>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync("{\"tenantUrl\":\"natwestdev.assure.wtwco.com\",\"isCalculationSuccessful\":true");

        var result = await _sut.RecalculateAccessKey(new ContentAccessKeyRequest { TenantUrl = It.IsAny<string>(), PreRetirementAgePeriodInYears = It.IsAny<int>(), NewlyRetiredRangeInMonth = It.IsAny<int>() });

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as ContentAccessKeyResponse;
        response.ContentAccessKey.Should().BeEquivalentTo("{\"tenantUrl\":\"natwestdev.assure.wtwco.com\",\"isCalculationSuccessful\":true");
        response.SchemeType.Should().BeEquivalentTo("DB");
        response.SchemeCodeAndCategory.Should().BeEquivalentTo("schemeCode-category");

        _cacheMock.Verify(x => x.Remove(It.IsAny<string>()), Times.Never);
        _calculationsRedisCacheMock.Verify(x => x.Clear(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}