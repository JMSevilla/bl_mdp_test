using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using WTW.MdpService.Domain.Common.Journeys;

namespace WTW.MdpService.Test.Domain.Common;

public class CheckboxesListTest
{
    private readonly List<(string, bool)> _checkBoxes = new List<(string, bool)>
        {
            ("a1",true),
            ("a2",false),
            ("a3",true),
        };

    public void CreatesCheckboxList()
    {
        var sut = new CheckboxesList("testKey", _checkBoxes);

        sut.CheckboxesListKey.Should().Be("testKey");
        sut.Checkboxes.Count.Should().Be(3);
        sut.Checkboxes.Single(x => x.Key == "a2").AnswerValue.Should().BeFalse();
    }

    public void DuplicatesCheckboxList()
    {
        var sut = new CheckboxesList("testKey", _checkBoxes);
        var clonedList = sut.Duplicate();

        clonedList.CheckboxesListKey.Should().Be("testKey");
        clonedList.Checkboxes.Count.Should().Be(3);
        clonedList.Checkboxes.Single(x => x.Key == "a2").AnswerValue.Should().BeFalse();
    }
}