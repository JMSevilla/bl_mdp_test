using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class IdvDetailTest
{
    public void CanCreateIdvDetail()
    {
        var sut = IdvDetail.Create(123, "result","doctype", 321);

        sut.Should().NotBeNull();
        sut.Id.Should().Be(123);
        sut.ScanResult.Should().Be("");
        sut.DocumentType.Should().Be("doctype");
        sut.EdmsNumber.Should().Be(321);
    }

    public void SetsCorrectScanResult1()
    {
        var sut = IdvDetail.Create(123, "Pass", "doctype", 321);
        
        sut.ScanResult.Should().Be("P");       
    }

    public void SetsCorrectScanResult2()
    {
        var sut = IdvDetail.Create(123, "Failed", "doctype", 321);

        sut.ScanResult.Should().Be("F");
    }

    public void SetsCorrectScanResult3()
    {
        var sut = IdvDetail.Create(123, "Referred", "doctype", 321);

        sut.ScanResult.Should().Be("R");
    }

    public void SetsCorrectScanResult4()
    {
        var sut = IdvDetail.Create(123, "Refer", "doctype", 321);

        sut.ScanResult.Should().Be("R");
    }
}