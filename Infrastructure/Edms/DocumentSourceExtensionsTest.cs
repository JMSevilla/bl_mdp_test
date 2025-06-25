using System;
using FluentAssertions;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Infrastructure.Edms;
using WTW.TestCommon.FixieConfig;

namespace WTW.MdpService.Test.Infrastructure.Edms;

public class DocumentSourceExtensionsTest
{
    [Input(DocumentSource.Outgoing, "O")]
    [Input(DocumentSource.Incoming, "I")]
    [Input(DocumentSource.None, null)]
    public void ReturnsCorrectDocSrcString(DocumentSource documentSource, string expectedValue)
    {
        documentSource.ToDocSrcString().Should().Be(expectedValue);
    }
}