using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Infrastructure.Aws;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.Aws;

public class AwsClientTest
{
    private readonly Mock<IAmazonS3> _s3ClientMock;
    private readonly Mock<ILogger<AwsClient>> _loggerMock;
    private readonly AwsClient _sut;

    public AwsClientTest()
    {
        _s3ClientMock = new Mock<IAmazonS3>();
        _loggerMock = new Mock<ILogger<AwsClient>>();
        _sut = new AwsClient(_s3ClientMock.Object, _loggerMock.Object);
    }

    public async Task FileReturnsFileStream()
    {
        _s3ClientMock
            .Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectResponse { ResponseStream = new MemoryStream() });

        var result = await _sut.File("s3://dev-s3pycalc/RBS/RBSPOEPAHUBDB_1111122_WW.pdf");

        result.IsRight.Should().BeTrue();
        result.Right().Should().BeOfType<MemoryStream>();
        _s3ClientMock.Verify(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    public async Task FileReturnsError_WhenS3ReturnsError()
    {
        _s3ClientMock
            .Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Exception("test message"));

        var result = await _sut.File("s3://dev-s3pycalc/RBS/RBSPOEPAHUBDB_1111122_WW.pdf");

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("test message");
        _s3ClientMock.Verify(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    public async Task FileReturnsError_WhenInvalidUriGiven()
    {
        var result = await _sut.File("aa/bb/cc");

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Invalid URI: The format of the URI could not be determined.");
    }
}