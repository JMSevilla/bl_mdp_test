using System;
using System.Security.Cryptography.Xml;
using FluentAssertions;
using LanguageExt.ClassInstances;
using LanguageExt.Common;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Members;
using WTW.TestCommon.FixieConfig;

namespace WTW.MdpService.Test.Domain.Mdp.Calculations;

public class TransferCalculationTest
{
    public void CanCreateTransferCalculation()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new MdpService.Domain.Mdp.Calculations.TransferCalculation("RBS", "1111124", "{}", date);

        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferenceNumber.Should().Be("1111124");
        sut.TransferQuoteJson.Should().Be("{}");
        sut.CreatedAt.Should().Be(date);
        sut.HasLockedInTransferQuote.Should().BeFalse();
    }

    public void LocksInTransferQuote()
    {
        var sut = new MdpService.Domain.Mdp.Calculations.TransferCalculation("RBS", "1111124", "{}", DateTimeOffset.UtcNow);

        sut.LockTransferQoute();

        sut.HasLockedInTransferQuote.Should().BeTrue();
    }

    public void UnlocksInTransferQuote()
    {
        var sut = new MdpService.Domain.Mdp.Calculations.TransferCalculation("RBS", "1111124", "{}", DateTimeOffset.UtcNow);
        sut.LockTransferQoute();

        sut.UnlockTransferQoute();

        sut.HasLockedInTransferQuote.Should().BeFalse();
    }

    [Input("{}", true, null, TransferApplicationStatus.StartedTA)]
    [Input("{}", false, null, TransferApplicationStatus.NotStartedTA)]
    [Input(null, false, null, TransferApplicationStatus.UnavailableTA)]
    [Input("{}", true, TransferApplicationStatus.SubmittedTA, TransferApplicationStatus.SubmittedTA)]
    [Input("{}", true, TransferApplicationStatus.OutsideTA, TransferApplicationStatus.OutsideTA)]
    [Input("{}", true, TransferApplicationStatus.SubmitStarted, TransferApplicationStatus.SubmitStarted)]
    public void ReturnsCorrectsTransferApplicationStatus(string transferQuoteJson, bool isLocked, TransferApplicationStatus? changeStatus, TransferApplicationStatus expectedStatus)
    {
        var sut = new MdpService.Domain.Mdp.Calculations.TransferCalculation("RBS", "1111124", transferQuoteJson, DateTimeOffset.UtcNow);

        if (isLocked)
            sut.LockTransferQoute();

        if(changeStatus.HasValue)
            sut.SetStatus(changeStatus.Value);

        var result = sut.TransferStatus();

        result.Should().Be(expectedStatus);
    }       

    public void CanSetTransferApplicationStatusForLockedQuote()
    {
        var sut = new MdpService.Domain.Mdp.Calculations.TransferCalculation("RBS", "1111124", null, DateTimeOffset.UtcNow);
        sut.LockTransferQoute();

        sut.SetStatus(TransferApplicationStatus.SubmitStarted);

        sut.Status.Should().NotBeNull();
        sut.Status.Should().Be(TransferApplicationStatus.SubmitStarted);
    }

    public void ReturnsError_ForSettingStatusWhenQuoteIsNotLocked()
    {
        var sut = new MdpService.Domain.Mdp.Calculations.TransferCalculation("RBS", "1111124", null, DateTimeOffset.UtcNow);
        var result = sut.SetStatus(TransferApplicationStatus.SubmitStarted);

        sut.Status.Should().BeNull();
        result.Should().NotBeNull();
        result.Value.Message.Should().Be("Transfer journey must be started");
    }
}