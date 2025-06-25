using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Infrastructure.Db;

namespace WTW.MdpService.Test.Db;

public class DatabaseConnectionParserTest
{
    private Mock<ILogger<DatabaseConnectionParser>> _mockLogger;

    public DatabaseConnectionParserTest()
    {
        _mockLogger = new Mock<ILogger<DatabaseConnectionParser>>();
    }
    
    public void GetDatabaseSid_ExtractsSidFromConnectionString()
    {
        var sut = new DatabaseConnectionParser(CreateTestConnectionString("somesid"), _mockLogger.Object);

        var result = sut.GetSid();
        result.Should().Be("somesid");
    }
    
    public void GetDatabaseSid_ReturnsEmptyWhenNoSid()
    {
        var sut = new DatabaseConnectionParser(CreateTestConnectionString(null), _mockLogger.Object);

        var result = sut.GetSid();
        result.Should().BeEmpty();
    }

    public void GetDatabaseSid_ReturnsNullWhenInvalidConnectionString()
    {
        var sut = new DatabaseConnectionParser("random string", _mockLogger.Object);

        var result = sut.GetSid();
        result.Should().BeNull();
    }

    private static string CreateTestConnectionString(string sid)
    {
        return $"DATA SOURCE=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=some-host)(PORT=123))(CONNECT_DATA=(SID={sid})));USER ID=usr; Password=psw;";
    }
}