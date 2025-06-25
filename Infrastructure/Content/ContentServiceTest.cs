using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using LanguageExt.Pipes;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using WTW.MdpService.Infrastructure.Content;
using WTW.TestCommon.FixieConfig;
using WTW.TestCommon.Helpers;
using WTW.Web.Serialization;
using static System.Net.Mime.MediaTypeNames;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace WTW.MdpService.Test.Infrastructure.Content;

public class ContentServiceTest
{
    private readonly Mock<IContentClient> _contentClientMock;
    private readonly Mock<ILogger<ContentService>> _loggerMock;
    private readonly ContentService _sut;
    protected JsonElement _stubTenantWithoutWordingFlags;
    protected JsonElement _stubTenantWithoutBgroup;
    protected JsonElement _stubTenantWithValidOldWordingFlagFormat;
    protected JsonElement _stubTenantWithIncompleteOldWordingFlagFormat;
    protected JsonElement _stubTenantWithValidWordingFlagFormat;
    protected JsonElement _stubTenantWithValidNewAndOldWordingFlagFormat;
    protected JsonElement _stubTenantWithIncompleteWordingFlagFormat;

    public ContentServiceTest()
    {
        _contentClientMock = new Mock<IContentClient>();
        _loggerMock = new Mock<ILogger<ContentService>>();
        _sut = new ContentService(_contentClientMock.Object, _loggerMock.Object);

        _stubTenantWithoutWordingFlags = JsonSerializer.Deserialize<JsonElement>(@"{""elements"": {""businessGroup"": {""values"": [""RBS""]}}}", SerialiationBuilder.Options());
        _stubTenantWithoutBgroup = JsonSerializer.Deserialize<JsonElement>(@"{""elements"": {}}", SerialiationBuilder.Options());
        _stubTenantWithValidOldWordingFlagFormat = JsonSerializer.Deserialize<JsonElement>(@"{""elements"": {""businessGroup"": {""values"": [""RBS""]},
            ""webRuleWordingFlag"": {""value"": {""elements"": {""classifierItem"": {""values"": [
            {""key"": {""elementType"": ""text"",""value"": ""HasExternalAVCs""},""value"": {""elementType"": ""text"",""value"": ""AVCDET=1""}},
            {""key"": {""elementType"": ""text"",""value"": ""HasDCAssets""},""value"": {""elementType"": ""text"",""value"": ""HASDC=1""}},
            {""key"": {""elementType"": ""text"",""value"": ""HasDBOnly""},""value"": {""elementType"": ""text"",""value"": ""DBONLY='Y'""}},
            {""key"": {""elementType"": ""text"",""value"": ""WithdrawalSummaryMode""},""value"": {""elementType"": ""text"",""value"": ""IDDMOD='S'""}},
            {""key"": {""elementType"": ""text"",""value"": ""WithdrawalStandardMode""},""value"": {""elementType"": ""text"",""value"": ""IDDMOD='M'""}}
            ]},""classifierKey"": {""elementType"": ""text"",""value"": ""web_rules""}},""type"": ""Classifier""}}}}", SerialiationBuilder.Options());
        _stubTenantWithIncompleteOldWordingFlagFormat = JsonSerializer.Deserialize<JsonElement>(@"{""elements"": {""businessGroup"": {""values"": [""RBS""]},
            ""webRuleWordingFlag"": {""value"": {}}}}"
        , SerialiationBuilder.Options());

        _stubTenantWithValidWordingFlagFormat = JsonSerializer.Deserialize<JsonElement>(@"{""elements"": {""businessGroup"": {""values"": [""RBS""]},
            ""cmsConfiguredWordingFlags"":{""values"":[
                {""elements"":{""webRuleConfiguration"":{""values"":[
                    {""key"":{""elementType"":""text"",""value"":""HasExternalAVCs""},""value"":{""elementType"":""text"",""value"":""AVCDET=1""}},
                    {""key"":{""elementType"":""text"",""value"":""HasDCAssets""},""value"":{""elementType"":""text"",""value"":""HASDC=1""}}
                    ]}},""type"":""element1""},
                {""elements"":{""webRuleConfiguration"":{""values"":[
                    {""key"":{""elementType"":""text"",""value"":""WithdrawalSummaryMode""},""value"":{""elementType"":""text"",""value"":""IDDMOD=M""}},
                    {""key"":{""elementType"":""text"",""value"":""WithdrawalStandardMode""},""value"":{""elementType"":""text"",""value"":""IDDMOD=S""}},
                    {""key"":{""elementType"":""text"",""value"":""WithdrawalNewSummaryMode""},""value"":{""elementType"":""text"",""value"":""IDDMOD=N""}}
                    ]}},""type"":""element2""},
                {""elements"":{""webRuleConfiguration"":{""values"":[
                    {""key"":{""elementType"":""text"",""value"":""HasDBOnly""},""value"":{""elementType"":""text"",""value"":""DBONLY=Y""}}
                    ]}},""type"":""element3""}
                ]}}}", SerialiationBuilder.Options());

        _stubTenantWithValidNewAndOldWordingFlagFormat = JsonSerializer.Deserialize<JsonElement>(@"{""elements"": {""businessGroup"": {""values"": [""RBS""]},
            ""webRuleWordingFlag"": {""value"": {""elements"": {""classifierItem"": {""values"": [
                {""key"": {""elementType"": ""text"",""value"": ""HasExternalAVCs""},""value"": {""elementType"": ""text"",""value"": ""AVCDET=1""}},
                {""key"": {""elementType"": ""text"",""value"": ""HasDCAssets""},""value"": {""elementType"": ""text"",""value"": ""HASDC=1""}},
                {""key"": {""elementType"": ""text"",""value"": ""HasDBOnly""},""value"": {""elementType"": ""text"",""value"": ""DBONLY='Y'""}},
                {""key"": {""elementType"": ""text"",""value"": ""WithdrawalSummaryMode""},""value"": {""elementType"": ""text"",""value"": ""IDDMOD='S'""}},
                {""key"": {""elementType"": ""text"",""value"": ""WithdrawalStandardMode""},""value"": {""elementType"": ""text"",""value"": ""IDDMOD='M'""}}
                ]},""classifierKey"": {""elementType"": ""text"",""value"": ""web_rules""}},""type"": ""Classifier""}},
            ""cmsConfiguredWordingFlags"":{""values"":[
                {""elements"":{""webRuleConfiguration"":{""values"":[
                    {""key"":{""elementType"":""text"",""value"":""HasExternalAVCs""},""value"":{""elementType"":""text"",""value"":""AVCDET=1""}},
                    {""key"":{""elementType"":""text"",""value"":""HasDCAssets""},""value"":{""elementType"":""text"",""value"":""HASDC=1""}}
                    ]}},""type"":""element1""},
                {""elements"":{""webRuleConfiguration"":{""values"":[
                   {""key"":{""elementType"":""text"",""value"":""WithdrawalSummaryMode""},""value"":{""elementType"":""text"",""value"":""IDDMOD=M""}},
                    {""key"":{""elementType"":""text"",""value"":""WithdrawalStandardMode""},""value"":{""elementType"":""text"",""value"":""IDDMOD=S""}},
                    {""key"":{""elementType"":""text"",""value"":""WithdrawalNewSummaryMode""},""value"":{""elementType"":""text"",""value"":""IDDMOD=N""}}
                    ]}},""type"":""element2""},
                {""elements"":{""webRuleConfiguration"":{""values"":[
                    {""key"":{""elementType"":""text"",""value"":""HasDBOnly""},""value"":{""elementType"":""text"",""value"":""DBONLY=Y""}}
                    ]}},""type"":""element3""}
                ]}}}", SerialiationBuilder.Options());

        _stubTenantWithIncompleteWordingFlagFormat = JsonSerializer.Deserialize<JsonElement>(@"{""elements"": {""businessGroup"": {""values"": [""RBS""]},
            ""cmsConfiguredWordingFlags"": {}}}", SerialiationBuilder.Options());
    }
    public async Task WhenFindTenantCalledWithValidInput_ExpectedContentResponseIsReturnedAndLoggingOccurred()
    {
        _contentClientMock.Setup(x => x.FindTenant(It.IsAny<string>()))
        .ReturnsAsync(_stubTenantWithoutWordingFlags);
        var actualResult = await _sut.FindTenant(It.IsAny<string>(), It.IsAny<string>());
        JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };
        var expectedResult = JsonSerializer.Deserialize<ContentResponse>(_stubTenantWithoutWordingFlags.GetRawText(), SerialiationBuilder.Options());
        actualResult.Should().NotBeNull();
        actualResult.Should().BeEquivalentTo(expectedResult);
        _loggerMock.VerifyLogging("FindTenant is called - tenantUrl: (null), businessGroup: (null)", LogLevel.Information, Times.Once());
    }
    public async Task WhenFindTenantCalledAndFindTenantThrowsException_ExpectedNullContentResponseIsReturnedAndLoggingOccurred()
    {
        _contentClientMock.Setup(x => x.FindTenant(It.IsAny<string>()))
        .ThrowsAsync(new HttpRequestException("message"));
        var actualResult = await _sut.FindTenant(It.IsAny<string>(), It.IsAny<string>());
        actualResult.Should().BeNull();
        _loggerMock.VerifyLogging("FindTenant - Unable to retrieve tenant content for this tenant url (null). Business group: (null)", LogLevel.Error, Times.Once());
    }
    [Input("RBS", true)]
    [Input("BCL", false)]
    public async Task WhenIsValidTenantCalledWithValidInput_ThenExpectedOutputIsRetruendAndLoggingOccurred(string businessGroup, bool expectedResult)
    {
        var actualResult = await _sut.IsValidTenant(JsonConvert.DeserializeObject<ContentResponse>(_stubTenantWithoutWordingFlags.GetRawText()), businessGroup);
        actualResult.Should().Be(expectedResult);
        _loggerMock.VerifyLogging($"IsValidTenant is called - businessGroup: {businessGroup}", LogLevel.Information, Times.Once());
    }
    [Input("RBS", false)]
    [Input("BCL", false)]
    public async Task WhenIsValidTenantCalledWithNullInput_ThenExpectedOutputIsRetruendAndLoggingOccurred(string businessGroup, bool expectedResult)
    {
        var actualResult = await _sut.IsValidTenant(null, businessGroup);
        actualResult.Should().Be(expectedResult);
        _loggerMock.VerifyLogging($"IsValidTenant - Tenant content not available, Business group: {businessGroup}", LogLevel.Warning, Times.Once());
    }
    public async Task WhenIsValidTenantGetsException_ThenExpectedOutputIsRetruendAndLoggingOccurred()
    {
        var businessGroup = "RBS";
        var actualResult = await _sut.IsValidTenant(JsonConvert.DeserializeObject<ContentResponse>(_stubTenantWithoutBgroup.GetRawText()), businessGroup);
        actualResult.Should().Be(false);
        _loggerMock.VerifyLogging($"IsValidTenant - Error while checking tenant content for business group: {businessGroup}", LogLevel.Error, Times.Once());
    }
    public async Task WhenGetWebRuleWordingFlagsCalledWithValidOldWordingFlagFormat_ThenExpectedOutputIsRetruendAndLoggingOccurred()
    {
        var expectedResult = JsonConvert.DeserializeObject<ContentResponse>(_stubTenantWithValidOldWordingFlagFormat.GetRawText()).Elements.WebRuleWordingFlag.Value.Elements.ClassifierItem.Values;
        var actualResult = await _sut.GetWebRuleWordingFlags(JsonConvert.DeserializeObject<ContentResponse>(_stubTenantWithValidOldWordingFlagFormat.GetRawText()));
        actualResult.Should().NotBeNull();
        actualResult.Should().BeEquivalentTo(expectedResult);
        _loggerMock.VerifyLogging("GetWebRuleWordingFlags is called", LogLevel.Information, Times.Once());
    }
    public async Task WhenGetWebRuleWordingFlagsCalledWithNullInput_ThenExpectedOutputIsRetruendAndLoggingOccurred()
    {
        var actualResult = await _sut.GetWebRuleWordingFlags(null);
        actualResult.Should().BeNull();
        _loggerMock.VerifyLogging("GetWebRuleWordingFlags - Tenant content not available", LogLevel.Warning, Times.Once());
    }
    public async Task WhenGetWebRuleWordingFlagsCalledWithIncompleteOldWordingFlagFormat_ThenExpectedNullOutputIsRetruendAndLoggingOccurred()
    {
        var actualResult = await _sut.GetWebRuleWordingFlags(JsonConvert.DeserializeObject<ContentResponse>(_stubTenantWithIncompleteOldWordingFlagFormat.GetRawText()));
        actualResult.Should().BeNull();
        _loggerMock.VerifyLogging("GetWebRuleWordingFlags - Tenant content elements and/or classifieritems are not available", LogLevel.Warning, Times.Once());
    }
    public async Task WhenGetWebRuleWordingFlagsCalledWithValidWordingFlagFormat_ThenExpectedOutputIsRetruendAndLoggingOccurred()
    {
        var expectedResult = JsonConvert.DeserializeObject<ContentResponse>(_stubTenantWithValidWordingFlagFormat.GetRawText()).Elements.CmsConfiguredWordingFlags.Values
            .Where(cmsConfiguredWordingFlag => cmsConfiguredWordingFlag?.Elements?.WebRuleConfiguration?.Values != null)
            .SelectMany(cmsConfiguredWordingFlag => cmsConfiguredWordingFlag.Elements.WebRuleConfiguration.Values)
            .ToList();

        var actualResult = await _sut.GetWebRuleWordingFlags(JsonConvert.DeserializeObject<ContentResponse>(_stubTenantWithValidWordingFlagFormat.GetRawText()));
        
        actualResult.Should().NotBeNull();
        actualResult.Should().BeEquivalentTo(expectedResult);

        _loggerMock.VerifyLogging("GetWebRuleWordingFlags is called", LogLevel.Information, Times.Once());
    }

    public async Task WhenGetWebRuleWordingFlagsCalledWithIncompleteWordingFlagFormat_ThenExpectedOutputIsRetruendAndLoggingOccurred()
    {
        var actualResult = await _sut.GetWebRuleWordingFlags(JsonConvert.DeserializeObject<ContentResponse>(_stubTenantWithIncompleteWordingFlagFormat.GetRawText()));

        actualResult.Should().BeNull();

        _loggerMock.VerifyLogging("GetWebRuleWordingFlags is called", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging("GetWebRuleWordingFlags - Tenant content elements and/or classifieritems are not available", LogLevel.Warning, Times.Once());
    }
    public async Task WhenGetWebRuleWordingFlagsCalledWithNoWordingFlagContent_ThenExpectedOutputIsRetruendAndLoggingOccurred()
    {
        var actualResult = await _sut.GetWebRuleWordingFlags(JsonConvert.DeserializeObject<ContentResponse>(_stubTenantWithoutWordingFlags.GetRawText()));

        actualResult.Should().BeNull();

        _loggerMock.VerifyLogging("GetWebRuleWordingFlags is called", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging("GetWebRuleWordingFlags - Tenant content elements and/or classifieritems are not available", LogLevel.Warning, Times.Once());
    }

    public async Task WhenGetWebRuleWordingFlagsCalledWithOldAndNewWordingFlagFormat_ThenExpectedOutputIsRetruendAndLoggingOccurred()
    {
        var expectedResult = JsonConvert.DeserializeObject<ContentResponse>(_stubTenantWithValidWordingFlagFormat.GetRawText()).Elements.CmsConfiguredWordingFlags.Values
            .Where(cmsConfiguredWordingFlag => cmsConfiguredWordingFlag?.Elements?.WebRuleConfiguration?.Values != null)
            .SelectMany(cmsConfiguredWordingFlag => cmsConfiguredWordingFlag.Elements.WebRuleConfiguration.Values)
            .ToList();

        var actualResult = await _sut.GetWebRuleWordingFlags(JsonConvert.DeserializeObject<ContentResponse>(_stubTenantWithValidNewAndOldWordingFlagFormat.GetRawText()));

        actualResult.Should().NotBeNull();
        actualResult.Should().BeEquivalentTo(expectedResult);

        _loggerMock.VerifyLogging("GetWebRuleWordingFlags is called", LogLevel.Information, Times.Once());
    }
}