using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using WTW.MdpService.Infrastructure.Edms;

namespace WTW.MdpService.Test.Infrastructure.Edms;

public class PostIndexErrorTest
{
    public void ReturnsErrorMessage_When401StatusCodeFormatRetrieved()
    {
        var sut = new PostIndexError
        {
            Error = "Invalid token",
            Description = "Signature verification failed",
            StatusCode = 401
        };

        var result = sut.GetErrorMessage();

        result.Should().Be("Error: Invalid token. Error description: Signature verification failed. StatusCode: 401.");
    }

    public void ReturnsErrorMessage_When400StatusCodeValidationErrorFormatRetrieved()
    {
        var sut = new PostIndexError
        {
            Errors = JsonSerializer.Deserialize<JsonElement>("{\"bgroup\": \"'bgroup' is a required property\",\"refno\": \"'refno' is a required property\", \"randomNonStringProp\":123}"),
            Message = "Input payload validation failed",
        };

        var result = sut.GetErrorMessage();

        result.Should().Be("Error message: Input payload validation failed. 'bgroup' is a required property. 'refno' is a required property.");
    }

    public void ReturnsErrorMessage_When400StatusCodeAndDocumentUploadErrorFormatRetrieved()
    {
        var sut = new PostIndexError
        {
            Documents = new List<PostindexDocumentResponse> {
                new PostindexDocumentResponse
                {
                    Message = "No document metadata found for bc6e88c6-e597-11ed-a25b-56e392e28d5e"
                },
                new PostindexDocumentResponse
                {
                    Message = "No document metadata found for 1234"
                }
            }
        };

        var result = sut.GetErrorMessage();

        result.Should().Be("Documents post index Errors: 'Message: No document metadata found for bc6e88c6-e597-11ed-a25b-56e392e28d5e.'" +
            " 'Message: No document metadata found for 1234.'");
    }

    public void ReturnsErrorMessage_WhenAllPropertiesArePopulated()
    {
        var sut = new PostIndexError
        {
            Error = "Invalid token",
            Description = "Signature verification failed",
            StatusCode = 401,
            Errors = JsonSerializer.Deserialize<JsonElement>("{\"bgroup\": \"'bgroup' is a required property\",\"refno\": \"'refno' is a required property\"}"),
            Message = "Input payload validation failed",
            Documents = new List<PostindexDocumentResponse> {
                new PostindexDocumentResponse
                {
                    Message = "No document metadata found for bc6e88c6-e597-11ed-a25b-56e392e28d5e"
                },
                new PostindexDocumentResponse
                {
                    Message = "No document metadata found for 1234"
                }
            }
        };

        var result = sut.GetErrorMessage();

        result.Should().Be("Error: Invalid token. Error description: Signature verification failed." +
            " StatusCode: 401. Error message: Input payload validation failed. 'bgroup' is a required" +
            " property. 'refno' is a required property.Documents post index Errors: 'Message: No" +
            " document metadata found for bc6e88c6-e597-11ed-a25b-56e392e28d5e.' 'Message: No document metadata found for 1234.'");
    }
}