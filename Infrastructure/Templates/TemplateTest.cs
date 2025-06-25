using System.Collections.Generic;
using FluentAssertions;
using WTW.Web.Templates;

namespace WTW.MdpService.Test.Infrastructure.Templates;

public class TemplateTest
{
    public void AppliesArgs()
    {
        var result = new Template("[[test]]!").Apply(
            new Dictionary<string, string>()
            {
                ["test"] = "value"
            });

        result.Should().Be("value!");
    }

    public void AppliesButtonsValueReplacement()
    {
        var result = new Template("[[button:test_button]]!").Apply(
            new Dictionary<string, (string, string)>()
            {
                ["button:test_button"] = ("Test buttun text", "https://www.wtwco.com/")
            });

        result.Should().Be("[[button:Test buttun text||https://www.wtwco.com/]]!");
    }

    public void ReplacesDataTokens()
    {
        var result = new Template("[[data-text:test]]!").ReplaceDataTokens(
                       new Dictionary<string, string>()
                       {
                           ["test"] = "value"
                       },
                       new string[] { "text" });

        result.Should().Be("value!");
    }
}