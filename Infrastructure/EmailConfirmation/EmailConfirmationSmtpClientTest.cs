using System.IO;
using System.Threading.Tasks;
using Moq;
using WTW.MdpService.Infrastructure.EmailConfirmation;
using WTW.TestCommon.FixieConfig;
using WTW.Web.Clients;

namespace WTW.MdpService.Test.Infrastructure.EmailConfirmation;

public class EmailConfirmationSmtpClientTest
{
    private readonly Mock<IEmailClient> _mockEmailClient;
    private readonly EmailConfirmationSmtpClient _emailConfirmationSmtpClient;

    public EmailConfirmationSmtpClientTest()
    {
        _mockEmailClient = new Mock<IEmailClient>();
        _emailConfirmationSmtpClient = new EmailConfirmationSmtpClient(_mockEmailClient.Object);
    }

    public async Task Send_ShouldCallEmailClientSend_WithCorrectParameters()
    {
        var to = "recipient@example.com";
        var from = "Assure Support;AssureSupport@mdp.wtwco.com";
        var htmlBody = "<p>Hello, World!</p>";
        var subject = "Test Subject";

        await _emailConfirmationSmtpClient.Send(to, from, htmlBody, subject);

        _mockEmailClient.Verify(client => client.Send(to, "assuresupport@mdp.wtwco.com", "Assure Support", subject, htmlBody, default), Times.Once);
    }

    [Input(null, "Assure Support", "AssureSupport@mdp.wtwco.com")]
    [Input("Assure Support;AssureSupport@mdp.wtwco.com;random", "Assure Support", "AssureSupport@mdp.wtwco.com")]
    [Input("Assure Support;invalid@email@com", "Assure Support", "AssureSupport@mdp.wtwco.com")]
    public async Task SendWithAttachment_ShouldCallEmailClientSendWithAttachment_WithCorrectParameters(string from, string expectedName, string expectedEmai)
    {
        var to = "recipient@example.com";
        var htmlBody = "<p>Hello, World!</p>";
        var subject = "Test Subject";
        var fileName = "test.pdf";
        var stream = new MemoryStream();

        await _emailConfirmationSmtpClient.SendWithAttachment(to, from, htmlBody, subject, stream, fileName);

        _mockEmailClient.Verify(client => client.SendWithAttachement(to, expectedEmai, expectedName, subject, htmlBody, stream, fileName, default), Times.Once);
    }
}