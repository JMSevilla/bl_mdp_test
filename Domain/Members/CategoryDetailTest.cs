using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class CategoryDetailTest
{
    public void CanCreateCategoryDetail()
    {
        var sut = new CategoryDetail(65, 60);

        sut.NormalRetirementAge.Should().Be(65);
        sut.MinimumPensionAge.Should().Be(60);
    }
}