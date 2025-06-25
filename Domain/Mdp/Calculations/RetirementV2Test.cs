using System;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using LanguageExt.Common;
using Microsoft.Extensions.Options;
using Moq;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.EpaService;
using WTW.TestCommon.FixieConfig;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Domain.Mdp.Calculations
{
    public class RetirementV2Test
    {
        private readonly CalculationsParser _sut;
        protected Mock<IOptionsSnapshot<CalculationServiceOptions>> _optionsMock;
        protected readonly Mock<IEpaServiceClient> _epaServiceClientMock;
        public RetirementV2Test()
        {
            _optionsMock = new Mock<IOptionsSnapshot<CalculationServiceOptions>>();
            _epaServiceClientMock = new Mock<IEpaServiceClient>();
            _sut = new CalculationsParser(_optionsMock.Object, _epaServiceClientMock.Object);
        }

        public void CanCreateRetirementV2FromRetirementResponse()
        {
            var response = JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options());

            var sut = new RetirementV2Mapper().MapToDomain(response, "PV");

            sut.Should().NotBeNull();
            sut.CalculationEventType.Should().Be("PV");
            sut.DateOfBirth.Should().Be(DateTime.Parse("1962-11-07T00:00:00"));
            sut.DatePensionableServiceCommenced.Should().Be(DateTime.Parse("1980-08-05T00:00:00"));
            sut.DateOfLeaving.Should().Be(DateTime.Parse("2010-09-06T00:00:00"));
            sut.StatePensionDate.Should().BeNull();
            sut.StatePensionDeduction.Should().BeNull();
            sut.GMPAge.Should().Be("P60Y0M");
            sut.Post88GMPIncreaseCap.Should().Be(3);
            sut.Pre88GMPAtGMPAge.Should().Be(960.96m);
            sut.Post88GMPAtGMPAge.Should().Be(1604.2m);
            sut.TransferInService.Should().BeNull();
            sut.TotalPensionableService.Should().BeNull();
            sut.FinalPensionableSalary.Should().BeNull();
            sut.InternalAVCFundValue.Should().Be(0);
            sut.ExternalAvcFundValue.Should().Be(0);
            sut.TotalAvcFundValue.Should().Be(0);
            sut.StandardLifetimeAllowance.Should().Be(1073100m);
            sut.TotalLtaUsedPercentage.Should().Be(60.32m);
            sut.MaximumPermittedTotalLumpSum.Should().Be(165346.35m);
            sut.TotalLtaRemainingPercentage.Should().Be(0);
            sut.NormalMinimumPensionAge.Should().Be("P55Y0M");
            sut.FindQuote("fullPension").PensionTranches.First().TrancheTypeCode.Should().Be("post88GMP");
            sut.FindQuote("fullPension").PensionTranches.First().IncreaseTypeCode.Should().Be("GMP");
            sut.FindQuote("fullPension").PensionTranches.First().Value.Should().Be(1604.2m);
            sut.WordingFlags.First().Should().Be("GMP");
            sut.ResidualFundValue.Should().Be(23477.3m);
        }

        public void CanCreateRetirementV2FromRetirementV2Dto()
        {
            var response = JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options());
            var (retirementV2, mdp) = _sut.GetRetirementJsonV2(response, "PV");

            var sut = _sut.GetRetirementV2(retirementV2);

            sut.Should().NotBeNull();
            sut.CalculationEventType.Should().Be("PV");
            sut.DateOfBirth.Should().Be(DateTime.Parse("1962-11-07T00:00:00"));
            sut.DatePensionableServiceCommenced.Should().Be(DateTime.Parse("1980-08-05T00:00:00"));
            sut.DateOfLeaving.Should().Be(DateTime.Parse("2010-09-06T00:00:00"));
            sut.StatePensionDate.Should().BeNull();
            sut.StatePensionDeduction.Should().BeNull();
            sut.GMPAge.Should().Be("P60Y0M");
            sut.Post88GMPIncreaseCap.Should().Be(3);
            sut.Pre88GMPAtGMPAge.Should().Be(960.96m);
            sut.Post88GMPAtGMPAge.Should().Be(1604.2m);
            sut.TransferInService.Should().BeNull();
            sut.TotalPensionableService.Should().BeNull();
            sut.FinalPensionableSalary.Should().BeNull();
            sut.InternalAVCFundValue.Should().Be(0);
            sut.ExternalAvcFundValue.Should().Be(0);
            sut.TotalAvcFundValue.Should().Be(0);
            sut.StandardLifetimeAllowance.Should().Be(1073100m);
            sut.TotalLtaUsedPercentage.Should().Be(60.32m);
            sut.MaximumPermittedTotalLumpSum.Should().Be(165346.35m);
            sut.TotalLtaRemainingPercentage.Should().Be(0);
            sut.NormalMinimumPensionAge.Should().Be("P55Y0M");
            sut.FindQuote("fullPension").PensionTranches.First().TrancheTypeCode.Should().Be("post88GMP");
            sut.FindQuote("fullPension").PensionTranches.First().IncreaseTypeCode.Should().Be("GMP");
            sut.FindQuote("fullPension").PensionTranches.First().Value.Should().Be(1604.2m);
            sut.WordingFlags.First().Should().Be("GMP");
        }

        public void CalculatesV2TotalAVCFund()
        {
            var response = JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options());
            var (retirementV2, mdp) = _sut.GetRetirementJsonV2(response, "PV");
            var sut = _sut.GetRetirementV2(retirementV2);

            var result = sut.TotalAVCFund();

            result.Should().BeNull();
        }

        public void CalculatesV2TotalPension()
        {
            var response = JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options());
            var (retirementV2, mdp) = _sut.GetRetirementJsonV2(response, "PV");
            var sut = _sut.GetRetirementV2(retirementV2);

            var result = sut.TotalPension();

            result.Should().Be(32369.4M);
        }

        public void GetsTotalLtaUsedPerc()
        {
            var response = JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options());
            var (retirementV2, mdp) = _sut.GetRetirementJsonV2(response, "PV");
            var sut = _sut.GetRetirementV2(retirementV2);

            var result = sut.GetTotalLtaUsedPerc("fullPension.DCAsOMOAnnuity", "RBS", "DB");

            result.Should().NotBeNull();
            result.Should().Be(21.17M);
        }

        public void GetsTotalLtaUsedPercForGsk()
        {
            var response = JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options());
            var (retirementV2, mdp) = _sut.GetRetirementJsonV2(response, "PV");
            var sut = _sut.GetRetirementV2(retirementV2);

            var result = sut.GetTotalLtaUsedPerc("fullPension.DCAsOMOAnnuity", "GSK", "DB");

            result.Should().NotBeNull();
            result.Should().Be(14.72M);
        }

        public void CanCreateRetirementJsonV2FromRetirementV2()
        {
            var retirementV2 = _sut.GetRetirementV2(TestData.RetirementJsonV2);

            var sut = _sut.GetRetirementJsonV2FromRetirementV2(retirementV2);

            sut.Should().BeEquivalentTo(TestData.RetirementJsonV2);
        }

        [Input(true, "inner-error")]
        [Input(false, "CalculationFailed")]
        public void SetsCalculationFailedWordingFlags(bool containsInnerError, string expectedWordingFlag)
        {
            var response = JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options());
            var (retirementV2, mdp) = _sut.GetRetirementJsonV2(response, "PV");
            var sut = _sut.GetRetirementV2(retirementV2);

            sut.SetCalculationFailedWordingFlags(containsInnerError ? Error.New("test-error", Error.New("inner-error")) : Error.New("test-error"));

            sut.WordingFlags.Should().Contain(expectedWordingFlag);
        }
    }
}
