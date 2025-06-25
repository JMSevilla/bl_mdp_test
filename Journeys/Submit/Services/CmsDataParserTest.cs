using System;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using WTW.MdpService.Infrastructure.MdpApi;
using WTW.MdpService.Journeys.Submit.Services.DocumentsRenderer;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Journeys.Submit.Services;

public class CmsDataParserTest
{
    private readonly CmsDataParser _sut;
    private readonly PublicApiSetting _publicApiSetting = new(new Uri("https://mdp.api.com"));

    public CmsDataParserTest()
    {
        _sut = new CmsDataParser(_publicApiSetting);
    }

    public async Task ReturnsUrisFromSummaryBlock()
    {
        var json = "{\"elements\":{\"dataSourceUrl\": {\"value\": \"http://www.google.com/api/get-data;http://www.yahoo.com/api/get-data;http://www.wtw.com/api/get-data\"}}}";

        var result = _sut.GetDataSummaryBlockSourceUris(JsonSerializer.Deserialize<JsonElement>(json, SerialiationBuilder.Options()));

        result.Should().HaveCount(3);
    }

    public async Task FormatsUrisFromSummaryBlock()
    {
        var json = "{\"elements\":{\"dataSourceUrl\": {\"value\": \"file://mdp-api/api/journeys/dcretirementapplication/data;api/get-all;api/get-quotes\"}}}";

        var result = _sut.GetDataSummaryBlockSourceUris(JsonSerializer.Deserialize<JsonElement>(json, SerialiationBuilder.Options()));

        result.Should().HaveCount(3);
    }

    public async Task ReturnsContentBlockKeys_WhenContentBlockKeysStringIsNotNullOrEmpty()
    {
        var result = _sut.GetContentBlockKeys(new MdpService.Infrastructure.Content.TemplateResponse { ContentBlockKeys="key1;key2;key3"});

        result.Should().HaveCount(3);
        result.Should().Contain("key1");
        result.Should().Contain("key2");
        result.Should().Contain("key3");
    }

    public async Task ReturnsEmptyContentBlockKeys_WhenContentBlockKeysStringIsNull()
    {
        var result = _sut.GetContentBlockKeys(new MdpService.Infrastructure.Content.TemplateResponse { ContentBlockKeys = "" });

        result.Should().BeEmpty();
    }
}