using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members.Dependants;
using WTW.TestCommon.FixieConfig;

namespace WTW.MdpService.Test.Domain.Members.Dependants;

public class DependantTest
{
    public void CreatesDependant()
    {
        var address = DependantAddress.Empty();
        var dob = DateTimeOffset.UtcNow.AddYears(-35);
        var now = DateTimeOffset.UtcNow;
        var sut = new Dependant("1111124", "RBS", 1, "SPOP", "Liz", "Doe", "M", dob, now, address);

        sut.ReferenceNumber.Should().Be("1111124");
        sut.BusinessGroup.Should().Be("RBS");
        sut.SequenceNumber.Should().Be(1);
        sut.RelationshipCode.Should().Be("SPOP");
        sut.Forenames.Should().Be("Liz");
        sut.Surname.Should().Be("Doe");
        sut.Gender.Should().Be("M");
        sut.DateOfBirth.Should().Be(dob);
        sut.StartDate.Should().Be(now);
        sut.EndDate.Should().BeNull();
        sut.Address.Should().BeEquivalentTo(address);
    }

    [Input("CHLD", "F", "Daughter")]
    [Input("CHLD", "M", "Son")]
    [Input("CHLD", null, "Child")]
    [Input("SPOP", "F", "Spouse")]
    [Input("SPOP", "M", "Spouse")]
    [Input("SPOP", null, "Spouse")]
    [Input("RANDOM", null, "Other Relation")]
    public void ReturnsCorrectDecodedRelationship(string RelationshipCode, string Gender, string DecodedRelationShip)
    {
        var address = DependantAddress.Empty();
        var dob = DateTimeOffset.UtcNow.AddYears(-35);
        var now = DateTimeOffset.UtcNow;

        var sut = new Dependant("1111124", "RBS", 1, RelationshipCode, "Liz", "Doe", Gender, dob, now, address);
        var result = sut.DecodedRelationship();

        result.Should().Be(DecodedRelationShip);
    }
}