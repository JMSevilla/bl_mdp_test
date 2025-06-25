using System;
using FluentAssertions;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Calculations;
using static WTW.Web.MdpConstants;
namespace WTW.MdpService.Test.Domain.Members
{
    public class MemberRetirementApplicationStatusTest
    {
        private RetirementDatesAges RetirementDatesAges(DateTimeOffset earliestRetirementDate)
        {
            return new RetirementDatesAges(new RetirementDatesAgesDto()
            {
                EarliestRetirementDate = earliestRetirementDate,
                NormalMinimumPensionDate = new DateTimeOffset(new DateTime(2025, 7, 30)),
                NormalRetirementDate = new DateTimeOffset(new DateTime(2030, 7, 30)),
                EarliestRetirementAge = 55,
                NormalMinimumPensionAge = 55,
                NormalRetirementAge = 60
            });
        }


        public void GetRetirementApplication_StartedRA()
        {
            var now = new DateTimeOffset(new DateTime(2022, 02, 07));
            var earliestRetirementDate = new DateTimeOffset(new DateTime(2016, 10, 10));
            var member = new MemberBuilder().Build();

            var retirementApplicationStatus =
                member.GetRetirementApplicationStatus(
                    now, 12, 6, true, false, false, null, RetirementDatesAges(earliestRetirementDate));

            retirementApplicationStatus.Should().Be(RetirementApplicationStatus.StartedRA);
        }

        public void GetRetirementApplication_SubmittedRA()
        {
            var now = new DateTimeOffset(new DateTime(2022, 02, 07));
            var earliestRetirementDate = new DateTimeOffset(new DateTime(2016, 10, 10));
            var member = new MemberBuilder().Build();

            var retirementApplicationStatus =
                member.GetRetirementApplicationStatus(now, 12, 6,
                    true, true, true, null, RetirementDatesAges(earliestRetirementDate));

            retirementApplicationStatus.Should().Be(RetirementApplicationStatus.SubmittedRA);
        }

        public void GetRetirementApplication_EligibleToStart()
        {
            var dateOfBirth = new DateTimeOffset(new DateTime(1967, 7, 30));
            var earliestRetirementDate = new DateTimeOffset(new DateTime(2022, 07, 30));
            var now = new DateTimeOffset(new DateTime(2022, 02, 07));
            var member = new MemberBuilder()
                .NormalRetirementAge(55)
                .MinimumPensionAge(50)
                .DateOfBirth(dateOfBirth)
                .Status(MemberStatus.Deferred)
                .BusinessGroup("OTHER")
                .Build();

            var retirementApplicationStatus =
                member.GetRetirementApplicationStatus(now, 6, 1,
                    false, false, false, null, RetirementDatesAges(earliestRetirementDate));

            retirementApplicationStatus.Should().Be(RetirementApplicationStatus.EligibleToStart);
        }

        public void GetRetirementApplication_RetirementDateOutOfRange()
        {
            var dateOfBirth = new DateTimeOffset(new DateTime(1967, 7, 30));
            var earliestRetirementDate = new DateTimeOffset(new DateTime(2022, 09, 30));
            var now = new DateTimeOffset(new DateTime(2022, 02, 07));
            var member = new MemberBuilder()
                .MinimumPensionAge(50)
                .DateOfBirth(dateOfBirth)
                .Status(MemberStatus.Deferred)
                .Build();

            var retirementApplicationStatus =
                member.GetRetirementApplicationStatus(now, 6, 1,
                    false, false, false, null, RetirementDatesAges(earliestRetirementDate));

            retirementApplicationStatus.Should().Be(RetirementApplicationStatus.RetirementDateOutOfRange);
        }

        public void GetRetirementApplication_NotEligibleToStart()
        {
            var dateOfBirth = new DateTimeOffset(new DateTime(1967, 7, 30));
            var earliestRetirementDate = new DateTimeOffset(new DateTime(2016, 10, 10));
            var now = new DateTimeOffset(new DateTime(2022, 02, 07));
            var member = new MemberBuilder()
                .MinimumPensionAge(50)
                .DateOfBirth(dateOfBirth)
                .Status(MemberStatus.Active)
                .Build();

            var retirementApplicationStatus =
                member.GetRetirementApplicationStatus(now, 6, 1,
                    false, false, false, null, RetirementDatesAges(earliestRetirementDate));

            retirementApplicationStatus.Should().Be(RetirementApplicationStatus.NotEligibleToStart);
        }

        public void GetRetirementApplication_RetirementCase()
        {
            var paperApplication = new PaperRetirementApplication(null, "RTP9", null, null, null, null, null, new BatchCreateDetails("paper"));
            var earliestRetirementDate = new DateTimeOffset(new DateTime(2016, 10, 10));
            var member = new MemberBuilder()
                .PaperRetirementApplications(paperApplication)
                .Build();

            var retirementApplicationStatus =
                member.GetRetirementApplicationStatus(DateTimeOffset.UtcNow, 6, 1,
                    false, false, false, null, RetirementDatesAges(earliestRetirementDate));

            retirementApplicationStatus.Should().Be(RetirementApplicationStatus.RetirementCase);
        }

        public void GetRetirementApplication_MinimumRetirementDateOutOfRange()
        {
            var dateOfBirth = new DateTimeOffset(new DateTime(1970, 7, 30));
            var earliestRetirementDate = new DateTimeOffset(new DateTime(2020, 07, 30));
            var now = new DateTimeOffset(new DateTime(2022, 02, 07));
            var member = new MemberBuilder()
                .NormalRetirementAge(60)
                .MinimumPensionAge(55)
                .DateOfBirth(dateOfBirth)
                .Status(MemberStatus.Deferred)
                .Build();

            var retirementApplicationStatus =
                member.GetRetirementApplicationStatus(DateTimeOffset.UtcNow, 6, 1,
                    false, false, false, new DateTimeOffset(new DateTime(2025, 01, 29)), RetirementDatesAges(earliestRetirementDate));

            retirementApplicationStatus.Should().Be(RetirementApplicationStatus.MinimumRetirementDateOutOfRange);
        }

        public void GetRetirementApplication_EligibleToStart_RBS()
        {
            var dateOfBirth = new DateTimeOffset(new DateTime(1967, 7, 30));
            var now = new DateTimeOffset(new DateTime(2022, 2, 7));
            var withinNinetyDays = now.AddDays(60);
            var member = new MemberBuilder()
                .NormalRetirementAge(55)
                .MinimumPensionAge(50)
                .DateOfBirth(dateOfBirth)
                .BusinessGroup(NatwestBgroup)
                .Status(MemberStatus.Deferred)
                .Build();

            var retirementApplicationStatus =
                member.GetRetirementApplicationStatus(
                    now,
                    6,
                    1,
                    false,
                    false,
                    false,
                    null,
                    RetirementDatesAges(withinNinetyDays));

            retirementApplicationStatus.Should().Be(RetirementApplicationStatus.EligibleToStart);
        }

        public void GetRetirementApplication_RetirementDateOutOfRange_RBS()
        {
            var dateOfBirth = new DateTimeOffset(new DateTime(1967, 7, 30));
            var now = new DateTimeOffset(new DateTime(2022, 2, 7));
            var beyondNinetyDays = now.AddDays(120);
            var member = new MemberBuilder()
                .MinimumPensionAge(50)
                .DateOfBirth(dateOfBirth)
                .BusinessGroup(NatwestBgroup)
                .Status(MemberStatus.Deferred)
                .Build();

            var retirementApplicationStatus =
                member.GetRetirementApplicationStatus(
                    now,
                    6,
                    1,
                    false,
                    false,
                    false,
                    null,
                    RetirementDatesAges(beyondNinetyDays));

            retirementApplicationStatus.Should().Be(RetirementApplicationStatus.RetirementDateOutOfRange);
        }
    }
}