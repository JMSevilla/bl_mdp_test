using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class PaperRetirementApplicationTest
{
    public void CreatesPaperRetirementApplication()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDeatils = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication("123456", "654321", "11111111", "someType", "someStatus", date, date.AddDays(-10), batchCreateDeatils);

        sut.Code.Should().Be("123456");
        sut.CaseCode.Should().Be("654321");
        sut.CaseNumber.Should().Be("11111111");
        sut.EventType.Should().Be("someType");
        sut.Status.Should().Be("someStatus");
        sut.CaseCompletionDate.Should().Be(date);
        sut.CaseReceivedDate.Should().Be(date.AddDays(-10));
        sut.BatchCreateDeatils.Should().NotBeNull();
        sut.BatchCreateDeatils.Should().BeEquivalentTo(batchCreateDeatils);
    }

    public void ReturnsTrue_WhenPaperRetirementApplicationSubmitted_WhenCaseCode_RTP9()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDeatils = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication(null, "RTP9", "11111111", "someType", "someStatus", date, date.AddDays(-10), batchCreateDeatils);

        var result = sut.IsPaperRetirementApplicationSubmitted();

        result.Should().BeTrue();
    }

    public void ReturnsTrue_WhenPaperRetirementApplicationSubmitted_WhenCaseCode_TOP9()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDeatils = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication(null, "TOP9", "11111111", "someType", "someStatus", date, date.AddDays(-10), batchCreateDeatils);

        var result = sut.IsPaperRetirementApplicationSubmitted();

        result.Should().BeTrue();
    }

    public void ReturnsFalse_WhenPaperRetirementApplicationIsNotSubmitted1()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDeatils = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication("123456", "TOP9", "11111111", "someType", "someStatus", date, date.AddDays(-10), batchCreateDeatils);

        var result = sut.IsPaperRetirementApplicationSubmitted();

        result.Should().BeFalse();
    }

    public void ReturnsFalse_WhenPaperRetirementApplicationIsNotSubmitted2()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDeatils = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication(null, "TOP9999", "11111111", "someType", "someStatus", date, date.AddDays(-10), batchCreateDeatils);

        var result = sut.IsPaperRetirementApplicationSubmitted();

        result.Should().BeFalse();
    }

    public void ReturnsFalse_WhenPaperRetirementApplicationIsNotSubmitted3()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var sut = new PaperRetirementApplication(null, "TOP9", "11111111", "someType", "someStatus", date, date.AddDays(-10), null);

        var result = sut.IsPaperRetirementApplicationSubmitted();

        result.Should().BeFalse();
    }

    public void ReturnsFalse_WhenPaperRetirementApplicationIsNotSubmitted4()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDeatils = new BatchCreateDetails("Case created by an online retirement application");
        var sut = new PaperRetirementApplication(null, "TOP9", "11111111", "someType", "someStatus", date, date.AddDays(-10), batchCreateDeatils);

        var result = sut.IsPaperRetirementApplicationSubmitted();

        result.Should().BeFalse();
    }

    public void ReturnsTrue_WhenTransferRetirementApplicationSubmitted()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDeatils = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication(null, "TOP9", "11111111", "someType", "someStatus", date, date.AddDays(-10), batchCreateDeatils);

        var result = sut.IsTransferRetirementApplicationSubmitted();

        result.Should().BeTrue();
    }

    public void ReturnsTrue_WhenTransferRetirementApplicationIsNotSubmitted1()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDeatils = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication("123456", "TOP9", "11111111", "someType", "someStatus", date, date.AddDays(-10), batchCreateDeatils);

        var result = sut.IsTransferRetirementApplicationSubmitted();

        result.Should().BeFalse();

    }

    public void ReturnsTrue_WhenTransferRetirementApplicationIsNotSubmitted2()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDeatils = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication(null, "TOP9999", "11111111", "someType", "someStatus", date, date.AddDays(-10), batchCreateDeatils);

        var result = sut.IsTransferRetirementApplicationSubmitted();

        result.Should().BeFalse();
    }

    public void ReturnsCorrectClosedOrAbandonedCaseStatus1()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDeatils = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication("AC", "RTP9", "11111111", "someType", "someStatus", date, date.AddDays(-10), batchCreateDeatils);

        var result = sut.IsClosedOrAbandoned();

        result.Should().BeTrue();
    }

    public void ReturnsCorrectClosedOrAbandonedCaseStatus2()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDeatils = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication(null, "RTP9", "11111111", "someType", "someStatus", null, date.AddDays(-10), batchCreateDeatils);

        var result = sut.IsClosedOrAbandoned();

        result.Should().BeFalse();
    }

    public void ReturnsCorrectClosedOrAbandonedCaseStatus3()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDeatils = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication(null, "RTP9", "11111111", "someType", "someStatus", date, date.AddDays(-10), batchCreateDeatils);

        var result = sut.IsClosedOrAbandoned();

        result.Should().BeFalse();
    }

    public void ReturnsCorrectClosedOrAbandonedCaseStatus4()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDeatils = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication("AC", "RTP9", "11111111", "someType", "someStatus", null, date.AddDays(-10), batchCreateDeatils);

        var result = sut.IsClosedOrAbandoned();

        result.Should().BeFalse();
    }

    public void IsAbandonedReturnsTrue_WhenCaseIsAbandoned()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDetails = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication("AC", "RTP9", "11111111", "someType", "someStatus", null, date.AddDays(-10), batchCreateDetails);

        var result = sut.IsAbandoned();

        result.Should().BeTrue();
    }

    public void IsAbandonedReturnsFalse_WhenCaseIsNotAbandoned()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDetails = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication("PF", "RTP9", "11111111", "someType", "someStatus", null, date.AddDays(-10), batchCreateDetails);

        var result = sut.IsAbandoned();

        result.Should().BeFalse();
    }

    public void ReturnsCorrectRTP9Status1()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDeatils = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication("AC", "RTP9", "11111111", "someType", "someStatus", date, date.AddDays(-10), batchCreateDeatils);

        var result = sut.IsRTP9();

        result.Should().BeTrue();
    }

    public void ReturnsCorrectRTP9Status2()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-23);
        var batchCreateDeatils = new BatchCreateDetails("some note");
        var sut = new PaperRetirementApplication("AC", "TOP9", "11111111", "someType", "someStatus", date, date.AddDays(-10), batchCreateDeatils);

        var result = sut.IsRTP9();

        result.Should().BeFalse();
    }
}