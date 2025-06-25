using FluentAssertions;
using WTW.MdpService.Domain.Members;
using WTW.TestCommon.FixieConfig;

namespace WTW.MdpService.Test.Domain.Members;

public class BatchCreateDetailsTest
{
    public void CreatesBatchCreateDetails()
    {
        var sut = new BatchCreateDetails("Some Note");

        sut.Notes.Should().Be("Some Note");
    }

    [Input("Some Note")]
    [Input(null)]
    public void ReturnsTrue_WhenPaperRetirementApplicationSubmitted(string notes)
    {
        var sut = new BatchCreateDetails(notes);
        var result = sut.IsPaperRetirementApplicationSubmitted();

        result.Should().BeTrue();
    }

    [Input("Case created by an online retirement application")]
    [Input("Case created by an online transfer application")]
    [Input("Case created by an online bereavement application")]
    public void ReturnsFalse_WhenPaperRetirementApplicationIsNotSubmitted(string notes)
    {
        var sut = new BatchCreateDetails(notes);
        var result = sut.IsPaperRetirementApplicationSubmitted();

        result.Should().BeFalse();
    }
}